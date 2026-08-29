using AssessmentPlatform.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssessmentPlatform.Api.Features.Attempts;

[ApiController]
[Route("api/attempts")]
[Authorize]
public class AttemptsController(AttemptService attempts) : ControllerBase
{
    /// <summary>Start (or resume) an attempt for a published quiz.</summary>
    [HttpPost("start/{quizId:guid}")]
    public Task<ActiveAttemptView> Start(Guid quizId, CancellationToken ct)
        => attempts.StartAsync(quizId, ct);

    /// <summary>Submit answers and get the graded, reviewable result.</summary>
    [HttpPost("{id:guid}/submit")]
    public Task<AttemptResultView> Submit(Guid id, SubmitAttemptRequest request, CancellationToken ct)
        => attempts.SubmitAsync(id, request, ct);

    /// <summary>Re-open a past attempt to revise it, answer by answer.</summary>
    [HttpGet("{id:guid}")]
    public Task<AttemptResultView> Get(Guid id, CancellationToken ct)
        => attempts.GetResultAsync(id, ct);

    [HttpGet("history")]
    public Task<List<AttemptHistoryItem>> History(CancellationToken ct)
        => attempts.GetHistoryAsync(ct);

    /// <summary>Ask the AI for a step-by-step solution, the theory, or a study tip on one question.</summary>
    [HttpPost("{id:guid}/questions/{questionId:guid}/help")]
    public Task<QuestionHelpView> QuestionHelp(Guid id, Guid questionId, [FromQuery] HelpKind kind, CancellationToken ct)
        => attempts.GetQuestionHelpAsync(id, questionId, kind, ct);

    /// <summary>Get an AI study plan for this attempt, based on which topics were weak.</summary>
    [HttpPost("{id:guid}/guidance")]
    public async Task<IActionResult> Guidance(Guid id, CancellationToken ct)
        => Ok(new { guidance = await attempts.GetStudyGuidanceAsync(id, ct) });
}
