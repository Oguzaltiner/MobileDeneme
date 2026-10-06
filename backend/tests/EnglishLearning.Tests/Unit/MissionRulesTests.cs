using EnglishLearning.Infrastructure.Leaderboard;
using EnglishLearning.Infrastructure.Missions;

namespace EnglishLearning.Tests.Unit;

/// <summary>Pure daily mission rules: day/zone resolution, completion window and XP.</summary>
public sealed class MissionRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Europe/Istanbul", null, "Europe/Istanbul")]
    [InlineData("Mars/Olympus", 180, "UTC+03:00")]
    [InlineData(null, -330, "UTC-05:30")]
    [InlineData("Etc/UTC", null, "UTC")]
    [InlineData(null, null, "UTC")]
    public void Effective_FirstRequest_StoresTheUsableZone(string? timeZone, int? offset, string expected)
    {
        var zone = MissionDayResolver.Effective(null, null, timeZone, offset, Now, out var persist);
        Assert.Equal(expected, zone!.Key);
        Assert.True(persist);
    }

    [Theory]
    [InlineData("Mars/Olympus", null)]
    [InlineData("Turkey Standard Time", null)] // Windows ids are not IANA ids
    [InlineData(null, 5000)]
    public void Effective_NothingUsableAndNothingStored_ReturnsNull(string? timeZone, int? offset) =>
        Assert.Null(MissionDayResolver.Effective(null, null, timeZone, offset, Now, out _));

    [Fact]
    public void Effective_DifferentZoneWithinSevenDays_KeepsStoredZone()
    {
        var zone = MissionDayResolver.Effective("Europe/Istanbul", Now.AddDays(-6), "America/New_York", null, Now, out var persist);
        Assert.Equal("Europe/Istanbul", zone!.Key);
        Assert.False(persist);
    }

    [Fact]
    public void Effective_DifferentZoneAfterSevenDays_ReplacesStoredZone()
    {
        var zone = MissionDayResolver.Effective("Europe/Istanbul", Now.AddDays(-7), "America/New_York", null, Now, out var persist);
        Assert.Equal("America/New_York", zone!.Key);
        Assert.True(persist);
    }

    [Theory]
    [InlineData("Mars/Olympus", null)]
    [InlineData(null, null)]
    public void Effective_UnusableOrMissingRequest_UsesStoredZone(string? timeZone, int? offset)
    {
        var zone = MissionDayResolver.Effective("UTC+03:00", Now.AddDays(-30), timeZone, offset, Now, out var persist);
        Assert.Equal("UTC+03:00", zone!.Key);
        Assert.Equal(180, zone.OffsetMinutes);
        Assert.False(persist);
    }

    [Fact]
    public void Resolve_NeverGoesBeforeLastMissionDay_AndStaysWithinOneDayOfUtc()
    {
        var utcMinus11 = MissionDayResolver.FromStoredKey("UTC-11:00");
        var lateUtc = new DateTime(2026, 10, 6, 5, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 10, 5), MissionDayResolver.Resolve(utcMinus11, lateUtc, null));
        Assert.Equal(new DateOnly(2026, 10, 6), MissionDayResolver.Resolve(utcMinus11, lateUtc, new DateOnly(2026, 10, 6)));
        Assert.Equal(new DateOnly(2026, 10, 7), MissionDayResolver.Resolve(MissionZone.Utc, lateUtc, new DateOnly(2026, 10, 9)));
    }

    [Theory]
    [InlineData("UTC", "2026-10-07T06:00:00Z")]
    [InlineData("UTC+03:00", "2026-10-07T03:00:00Z")]
    [InlineData("Europe/Istanbul", "2026-10-07T03:00:00Z")]
    [InlineData("America/New_York", "2026-10-07T10:00:00Z")] // EDT (UTC-4) in October
    public void CompletionDeadline_IsEndOfLocalMissionDayPlusSixHours(string zone, string expectedUtc) =>
        Assert.Equal(DateTime.Parse(expectedUtc).ToUniversalTime(), MissionDayResolver.CompletionDeadlineUtc(zone, new DateOnly(2026, 10, 6)));

    [Fact]
    public void XpRules_MatchTheContract()
    {
        var breakdown = MissionXpRules.Calculate(reviewedWords: 8, newWords: 5, correctAnswers: 4, streak: 4);
        Assert.Equal(new(30, 31, 20, 5), breakdown);
        Assert.Equal(86, MissionXpRules.Total(breakdown));
        Assert.Equal(0, MissionXpRules.Calculate(0, 0, 0, streak: 2).StreakBonus);
    }

    [Fact]
    public void PublicName_MasksMissingDisplayName()
    {
        var id = Guid.Parse("abcd1234-0000-0000-0000-000000000000");
        Assert.Equal("Öğrenci #abcd", LeaderboardService.PublicName(id, null));
        Assert.Equal("Öğrenci #abcd", LeaderboardService.PublicName(id, "  "));
        Assert.Equal("Ayşe", LeaderboardService.PublicName(id, " Ayşe "));
    }
}
