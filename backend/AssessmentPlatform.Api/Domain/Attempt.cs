using System.ComponentModel.DataAnnotations.Schema;

namespace AssessmentPlatform.Api.Domain;

/// <summary>One student's run through a quiz. Kept forever so students can revise past attempts.</summary>
public class Attempt
{
    public Guid Id { get; set; }

    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public double ScorePercent { get; set; }
    public int DurationSeconds { get; set; }

    [NotMapped]
    public bool IsSubmitted => SubmittedAt is not null;

    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();

    /// <summary>AI study feedback generated after submission (null until requested).</summary>
    public AttemptReview? Review { get; set; }
}
