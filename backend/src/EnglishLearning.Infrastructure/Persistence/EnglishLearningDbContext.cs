using Microsoft.EntityFrameworkCore;
using EnglishLearning.Domain;

namespace EnglishLearning.Infrastructure.Persistence;

public sealed class EnglishLearningDbContext(DbContextOptions<EnglishLearningDbContext> options)
    : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<VocabularyWord> VocabularyWords => Set<VocabularyWord>();
    public DbSet<QuizSession> QuizSessions => Set<QuizSession>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<UserEntitlement> UserEntitlements => Set<UserEntitlement>();
    public DbSet<DailyUsage> DailyUsages => Set<DailyUsage>();
    public DbSet<UserWordProgress> UserWordProgress => Set<UserWordProgress>();
    public DbSet<ReviewEvent> ReviewEvents => Set<ReviewEvent>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).IsRequired(); entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.Role).HasMaxLength(20).HasDefaultValue("Learner").IsRequired();
            entity.HasOne(x => x.Settings).WithOne(x => x.User).HasForeignKey<UserSettings>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.ToTable("user_settings"); entity.HasKey(x => x.UserId);
            entity.Property(x => x.CurrentLevel).HasMaxLength(10); entity.Property(x => x.LearningPurpose).HasMaxLength(80);
            entity.Property(x => x.PreferredLanguage).HasMaxLength(10).IsRequired(); entity.Property(x => x.DailyGoal).HasDefaultValue(10);
        });
        modelBuilder.Entity<UserEntitlement>(entity =>
        {
            entity.ToTable("user_entitlements"); entity.HasKey(x => x.UserId);
            entity.Property(x => x.Plan).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(30); entity.Property(x => x.ProductId).HasMaxLength(160);
            entity.HasOne(x => x.User).WithOne().HasForeignKey<UserEntitlement>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DailyUsage>(entity =>
        {
            entity.ToTable("daily_usage"); entity.HasKey(x => new { x.UserId, x.DateUtc });
            entity.Property(x => x.DateUtc).HasColumnType("date");
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserWordProgress>(entity =>
        {
            entity.ToTable("user_word_progress"); entity.HasKey(x => new { x.UserId, x.VocabularyWordId });
            entity.Property(x => x.EaseFactor).HasPrecision(4, 2);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.VocabularyWord).WithMany().HasForeignKey(x => x.VocabularyWordId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ReviewEvent>(entity =>
        {
            entity.ToTable("review_events"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Rating).HasConversion<string>().HasMaxLength(10).IsRequired();
            entity.Property(x => x.ClientEventId).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.UserId, x.ClientEventId }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.VocabularyWord).WithMany().HasForeignKey(x => x.VocabularyWordId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.ToTable("admin_audit_logs"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(40).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens"); entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired(); entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<VocabularyWord>(entity =>
        {
            entity.ToTable("vocabulary_words"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Term).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Pronunciation).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PartOfSpeech).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Definition).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Translation).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Level).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ExampleSentence).HasMaxLength(500);
            entity.Property(x => x.PublicationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.PublicationStatus);
            entity.HasIndex(x => new { x.Level, x.Category });
            entity.HasIndex(x => x.Term).IsUnique();
            var seed = VocabularySeed.Words;
            entity.HasData(seed);
        });
        modelBuilder.Entity<QuizSession>(entity =>
        {
            entity.ToTable("quiz_sessions"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Level).HasMaxLength(10); entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.ToTable("quiz_questions"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.SessionId, x.Order }).IsUnique();
            entity.HasOne(x => x.Session).WithMany(x => x.Questions).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.VocabularyWord).WithMany().HasForeignKey(x => x.VocabularyWordId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<QuizOption>(entity =>
        {
            entity.ToTable("quiz_options"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(1).IsRequired(); entity.Property(x => x.Text).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.QuestionId, x.Key }).IsUnique();
            entity.HasOne(x => x.Question).WithMany(x => x.Options).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

internal static class VocabularySeed
{
    public static readonly VocabularyWord[] Words =
    [
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Term="achieve", Pronunciation="/əˈtʃiːv/", PartOfSpeech="verb", Definition="to succeed in doing something", Translation="başarmak", Level="A2", Category="Daily Life", ExampleSentence="You can achieve your goals." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Term="curious", Pronunciation="/ˈkjʊəriəs/", PartOfSpeech="adjective", Definition="wanting to know or learn something", Translation="meraklı", Level="A2", Category="Personality", ExampleSentence="Children are naturally curious." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Term="efficient", Pronunciation="/ɪˈfɪʃənt/", PartOfSpeech="adjective", Definition="working well without wasting time or energy", Translation="verimli", Level="B1", Category="Work", ExampleSentence="This is an efficient way to study." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Term="journey", Pronunciation="/ˈdʒɜːni/", PartOfSpeech="noun", Definition="an act of travelling from one place to another", Translation="yolculuk", Level="B1", Category="Travel", ExampleSentence="The journey took three hours." },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Term="significant", Pronunciation="/sɪɡˈnɪfɪkənt/", PartOfSpeech="adjective", Definition="important or noticeable", Translation="önemli", Level="B2", Category="Academic", ExampleSentence="The study found a significant difference." }
        ,new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Term="improve", Pronunciation="/ɪmˈpruːv/", PartOfSpeech="verb", Definition="to make something better", Translation="geliştirmek", Level="A2", Category="Daily Life", ExampleSentence="Practice helps you improve." }
        ,new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Term="prepare", Pronunciation="/prɪˈpeə/", PartOfSpeech="verb", Definition="to get ready for something", Translation="hazırlanmak", Level="A2", Category="Daily Life", ExampleSentence="I prepare for the quiz every morning." }
        ,new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), Term="careful", Pronunciation="/ˈkeəfəl/", PartOfSpeech="adjective", Definition="giving attention to avoid mistakes", Translation="dikkatli", Level="A2", Category="Personality", ExampleSentence="Be careful with the answer." }
    ];
}
