using EnglishLearning.Application.Missions;

namespace EnglishLearning.Infrastructure.Missions;

/// <summary>XP for a completed daily mission (docs/DAILY_MISSION.md, "XP").</summary>
public static class MissionXpRules
{
    public const int Base = 30;
    public const int PerReview = 2;
    public const int PerNewWord = 3;
    public const int PerCorrectAnswer = 5;
    public const int StreakBonus = 5;
    public const int StreakBonusThreshold = 3;

    /// <param name="streak">Mission streak including the mission being completed.</param>
    public static MissionXpBreakdownDto Calculate(int reviewedWords, int newWords, int correctAnswers, int streak) => new(
        Base,
        Math.Max(0, reviewedWords) * PerReview + Math.Max(0, newWords) * PerNewWord,
        Math.Max(0, correctAnswers) * PerCorrectAnswer,
        streak >= StreakBonusThreshold ? StreakBonus : 0);

    public static int Total(MissionXpBreakdownDto breakdown) => breakdown.Base + breakdown.Items + breakdown.Accuracy + breakdown.StreakBonus;
}
