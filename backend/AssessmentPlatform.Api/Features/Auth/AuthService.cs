using AssessmentPlatform.Api.Common;
using AssessmentPlatform.Api.Data;
using AssessmentPlatform.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssessmentPlatform.Api.Features.Auth;

public class AuthService(AppDbContext db, JwtTokenService tokens)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = Normalize(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw AppException.Conflict("An account with that email already exists.");

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = Normalize(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new AppException("Email or password is incorrect.", StatusCodes.Status401Unauthorized);

        return BuildResponse(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expiresAt) = tokens.Create(user);
        return new AuthResponse(token, expiresAt,
            new UserSummary(user.Id, user.Email, user.FullName, user.Role));
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
