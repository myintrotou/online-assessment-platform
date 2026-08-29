using AssessmentPlatform.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssessmentPlatform.Api.Data;

/// <summary>Seeds a demo instructor, a demo student, and one published quiz on first run.</summary>
public static class DbSeeder
{
    public const string DemoPassword = "Password123!";

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        var instructor = new User
        {
            Email = "instructor@demo.com",
            FullName = "Demo Instructor",
            Role = UserRole.Instructor,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword)
        };

        var student = new User
        {
            Email = "student@demo.com",
            FullName = "Demo Student",
            Role = UserRole.Student,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword)
        };

        db.Users.AddRange(instructor, student);

        var quiz = new Quiz
        {
            Title = "C# Fundamentals",
            Subject = "C# Programming",
            Description = "A short starter quiz on C# basics.",
            TimeLimitMinutes = 10,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow,
            CreatedBy = instructor
        };

        quiz.Questions.Add(new Question
        {
            Text = "Which keyword marks a field that can only be set in its declaration or a constructor?",
            Options = new List<string> { "const", "readonly", "static", "sealed" },
            CorrectOptionIndex = 1,
            Explanation = "A 'readonly' field can be assigned at declaration or in a constructor, then never again.",
            Topic = "Fields and constants",
            Source = QuestionSource.Manual,
            OrderIndex = 0
        });

        quiz.Questions.Add(new Question
        {
            Text = "What does the 'async' modifier by itself do to how a method runs?",
            Options = new List<string>
            {
                "Runs the method on a new thread",
                "Nothing on its own - it only enables 'await'",
                "Blocks the calling thread until completion",
                "Makes the method thread-safe"
            },
            CorrectOptionIndex = 1,
            Explanation = "'async' only lets you use 'await'. Whether work moves off the current thread depends on what you await.",
            Topic = "Async and await",
            Source = QuestionSource.Manual,
            OrderIndex = 1
        });

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();
    }
}
