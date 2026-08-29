using System.ComponentModel.DataAnnotations;
using AssessmentPlatform.Api.Domain;

namespace AssessmentPlatform.Api.Features.Auth;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(2), MaxLength(120)] string FullName,
    [Required, MinLength(8), MaxLength(128)] string Password,
    UserRole Role);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(string Token, DateTime ExpiresAt, UserSummary User);

public record UserSummary(Guid Id, string Email, string FullName, UserRole Role);
