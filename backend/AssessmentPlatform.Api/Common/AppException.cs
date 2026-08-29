namespace AssessmentPlatform.Api.Common;

/// <summary>
/// An expected, user-facing error. The middleware turns this into a clean JSON response
/// with the given status code instead of a 500.
/// </summary>
public class AppException(string message, int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static AppException NotFound(string what) => new($"{what} was not found.", StatusCodes.Status404NotFound);
    public static AppException Forbidden(string message = "You do not have access to this resource.") => new(message, StatusCodes.Status403Forbidden);
    public static AppException Conflict(string message) => new(message, StatusCodes.Status409Conflict);
    public static AppException Unavailable(string message) => new(message, StatusCodes.Status503ServiceUnavailable);
}
