using System.Security.Claims;
using AssessmentPlatform.Api.Domain;

namespace AssessmentPlatform.Api.Common;

/// <summary>The signed-in user for the current request, read from the validated JWT.</summary>
public interface ICurrentUser
{
    Guid Id { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsInstructor { get; }
    bool IsAuthenticated { get; }
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid Id => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw AppException.Forbidden("You must be signed in.");

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public UserRole Role => Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role)
        ? role
        : UserRole.Student;

    public bool IsInstructor => Role == UserRole.Instructor;
}
