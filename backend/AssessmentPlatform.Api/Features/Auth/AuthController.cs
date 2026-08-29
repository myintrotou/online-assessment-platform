using Microsoft.AspNetCore.Mvc;

namespace AssessmentPlatform.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Create an account as a Student or Instructor and receive a JWT.</summary>
    [HttpPost("register")]
    public Task<AuthResponse> Register(RegisterRequest request, CancellationToken ct)
        => auth.RegisterAsync(request, ct);

    /// <summary>Exchange email + password for a JWT.</summary>
    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken ct)
        => auth.LoginAsync(request, ct);
}
