using AssessmentPlatform.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssessmentPlatform.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public DbSet<QuestionHelp> QuestionHelpEntries => Set<QuestionHelp>();
    public DbSet<AttemptReview> AttemptReviews => Set<AttemptReview>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Quiz>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.IsPublished);
            e.HasOne(x => x.CreatedBy)
                .WithMany(u => u.QuizzesCreated)
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Question>(e =>
        {
            e.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Explanation).HasMaxLength(4000);
            e.Property(x => x.Topic).HasMaxLength(120);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<QuestionHelp>(e =>
        {
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Content).IsRequired();
            e.HasIndex(x => new { x.QuestionId, x.Kind }).IsUnique();
            e.HasOne(x => x.Question)
                .WithMany(q => q.HelpEntries)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Attempt>(e =>
        {
            e.HasIndex(x => new { x.StudentId, x.QuizId });
            e.HasOne(x => x.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Student)
                .WithMany(u => u.Attempts)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AttemptAnswer>(e =>
        {
            e.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
            e.HasOne(x => x.Attempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AttemptReview>(e =>
        {
            e.Property(x => x.Content).IsRequired();
            e.HasOne(x => x.Attempt)
                .WithOne(a => a.Review)
                .HasForeignKey<AttemptReview>(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
