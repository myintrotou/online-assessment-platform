namespace AssessmentPlatform.Api.Domain;

public class AttemptAnswer
{
    public Guid Id { get; set; }

    public Guid AttemptId { get; set; }
    public Attempt Attempt { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    /// <summary>The option the student picked. Null means they left it blank.</summary>
    public int? SelectedOptionIndex { get; set; }

    public bool IsCorrect { get; set; }
}
