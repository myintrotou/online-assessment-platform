using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssessmentPlatform.Api.Features.Quizzes;

[ApiController]
[Route("api/quizzes")]
[Authorize]
public class QuizzesController(QuizService quizzes) : ControllerBase
{
    // ----- Instructor -----

    [HttpPost]
    public Task<QuizManageDetail> Create(CreateQuizRequest request, CancellationToken ct)
        => quizzes.CreateAsync(request, ct);

    [HttpGet("mine")]
    public Task<List<QuizListItem>> Mine(CancellationToken ct)
        => quizzes.ListMineAsync(ct);

    [HttpGet("{id:guid}/manage")]
    public Task<QuizManageDetail> Manage(Guid id, CancellationToken ct)
        => quizzes.GetForManageAsync(id, ct);

    /// <summary>Draft questions with the AI helper. Returns them for review - nothing is saved yet.</summary>
    [HttpPost("{id:guid}/questions/generate")]
    public Task<IReadOnlyList<GeneratedQuestionDto>> Generate(Guid id, GenerateQuestionsRequest request, CancellationToken ct)
        => quizzes.GenerateAsync(id, request, ct);

    /// <summary>Save a reviewed batch of questions (manual or AI-drafted) onto the quiz.</summary>
    [HttpPost("{id:guid}/questions")]
    public Task<QuizManageDetail> AddQuestions(Guid id, SaveQuestionsRequest request, CancellationToken ct)
        => quizzes.AddQuestionsAsync(id, request, ct);

    [HttpDelete("{id:guid}/questions/{questionId:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid id, Guid questionId, CancellationToken ct)
    {
        await quizzes.DeleteQuestionAsync(id, questionId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    public Task<QuizManageDetail> Publish(Guid id, CancellationToken ct)
        => quizzes.SetPublishedAsync(id, publish: true, ct);

    [HttpPost("{id:guid}/unpublish")]
    public Task<QuizManageDetail> Unpublish(Guid id, CancellationToken ct)
        => quizzes.SetPublishedAsync(id, publish: false, ct);

    [HttpGet("{id:guid}/results")]
    public Task<QuizResultsSummary> Results(Guid id, CancellationToken ct)
        => quizzes.GetResultsAsync(id, ct);

    // ----- Student -----

    [HttpGet("published")]
    public Task<List<PublishedQuizItem>> Published(CancellationToken ct)
        => quizzes.ListPublishedAsync(ct);
}
