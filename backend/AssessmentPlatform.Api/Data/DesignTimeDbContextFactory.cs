using AssessmentPlatform.Api.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AssessmentPlatform.Api.Data;

/// <summary>
/// Used by `dotnet ef` at design time (migrations add / database update). Reads the Neon
/// credentials from .env.local and prefers the direct, non-pooled endpoint for DDL work.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DotEnv.Load(".env.local", ".env");

        var url = Environment.GetEnvironmentVariable("DATABASE_URL_UNPOOLED")
                  ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        var connection = string.IsNullOrWhiteSpace(url)
            ? "Host=localhost;Port=5432;Database=assessment;Username=postgres;Password=postgres"
            : url.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
                ? NpgsqlUrl.ToConnectionString(url)
                : url;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new AppDbContext(options);
    }
}
