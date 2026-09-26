using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Persistence;

public sealed class EnglishLearningDbContext(DbContextOptions<EnglishLearningDbContext> options)
    : DbContext(options);
