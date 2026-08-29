namespace AssessmentPlatform.Api.Domain;

/// <summary>
/// A cached piece of AI-generated help for one question. We store it so that asking for the
/// same "solution" / "theory" / "guidance" twice does not cost another model call.
/// </summary>
public class QuestionHelp
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public HelpKind Kind { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
