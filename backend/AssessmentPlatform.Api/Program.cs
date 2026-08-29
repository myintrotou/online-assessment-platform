using System.Text;
using System.Text.Json.Serialization;
using AssessmentPlatform.Api.Ai;
using AssessmentPlatform.Api.Common;
using AssessmentPlatform.Api.Data;
using AssessmentPlatform.Api.Features.Attempts;
using AssessmentPlatform.Api.Features.Auth;
using AssessmentPlatform.Api.Features.Quizzes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// Load .env.local / .env before the builder reads configuration (local dev + Neon branch-first flow).
// In production the files are absent and this is a no-op.
DotEnv.Load(".env.local", ".env");

var builder = WebApplication.CreateBuilder(args);

// Cloud hosts (Render, Railway, Fly) inject the port to listen on via PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ---------------- Options ----------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwtOptions.Key.Length < 32)
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException("Jwt:Key must be set to a 32+ character secret in production.");
    jwtOptions.Key = "dev-only-insecure-signing-key-please-change-me";
}

// ---------------- Database ----------------
// App traffic uses the pooled endpoint (DATABASE_URL); migrations use the direct one
// (DATABASE_URL_UNPOOLED) because PgBouncer's transaction mode breaks migration/DDL sessions.
var appConnection = BuildConnectionString(
    Blank(builder.Configuration.GetConnectionString("Default"))
    ?? Blank(Environment.GetEnvironmentVariable("DATABASE_URL")));

var migrationConnection = BuildConnectionString(
    Blank(Environment.GetEnvironmentVariable("DATABASE_URL_UNPOOLED"))
    ?? Blank(builder.Configuration.GetConnectionString("Default"))
    ?? Blank(Environment.GetEnvironmentVariable("DATABASE_URL")));

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(appConnection));

// ---------------- MVC / JSON ----------------
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHttpContextAccessor();

// ---------------- Auth ----------------
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// ---------------- Feature services ----------------
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<AttemptService>();

// ---------------- AI provider ----------------
var aiOptions = builder.Configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
if (aiOptions.Provider.Equals("Stub", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IAiService, StubAiService>();
}
else
{
    builder.Services.AddHttpClient<IAiService, GeminiAiService>(client =>
    {
        client.BaseAddress = new Uri(aiOptions.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(aiOptions.TimeoutSeconds + 5);
    });
}

// ---------------- CORS ----------------
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(policy =>
{
    if (corsOrigins.Contains("*"))
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else if (corsOrigins.Length > 0)
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
}));

// ---------------- Swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Online Assessment Platform API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT from /api/auth/login (no 'Bearer ' prefix).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// ---------------- Migrate + seed ----------------
// Run over the direct (non-pooled) connection.
{
    var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(migrationConnection).Options;
    await using var db = new AppDbContext(options);
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

// ---------------- Pipeline ----------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Assessment Platform API v1"));
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { service = "assessment-platform-api", status = "ok" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// ---------------- helpers ----------------
static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

static string BuildConnectionString(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return "Host=localhost;Port=5432;Database=assessment;Username=postgres;Password=postgres";

    return value.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
        ? NpgsqlUrl.ToConnectionString(value)
        : value;
}

/// <summary>Exposed so WebApplicationFactory-based integration tests can boot the API.</summary>
public partial class Program;
