namespace AssessmentPlatform.Api.Domain;

/// <summary>
/// AI-written study feedback for a finished attempt: which topics were weak and what to revise next.
/// One per attempt; cached so re-opening a past result does not re-run the model.
/// </summary>
public class AttemptReview
{
    public Guid Id { get; set; }

    public Guid AttemptId { get; set; }
    public Attempt Attempt { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
