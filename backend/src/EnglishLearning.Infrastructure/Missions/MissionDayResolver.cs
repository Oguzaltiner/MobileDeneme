using System.Globalization;

namespace EnglishLearning.Infrastructure.Missions;

/// <summary>A resolved client clock: an IANA zone, a fixed UTC offset, or UTC.</summary>
public sealed record MissionZone(string Key, TimeZoneInfo? Zone, int OffsetMinutes)
{
    public static readonly MissionZone Utc = new("UTC", null, 0);

    public DateOnly LocalDate(DateTime utcNow) => DateOnly.FromDateTime(Zone is not null
        ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), Zone)
        : utcNow.AddMinutes(OffsetMinutes));

    /// <summary>UTC instant at which <paramref name="date"/> ends (local midnight of the next day).</summary>
    public DateTime EndOfDayUtc(DateOnly date)
    {
        var midnight = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        if (Zone is null) return DateTime.SpecifyKind(midnight.AddMinutes(-OffsetMinutes), DateTimeKind.Utc);
        // Zones whose DST switch happens at midnight skip it; the day then ends an hour later.
        if (Zone.IsInvalidTime(midnight)) midnight = midnight.AddHours(1);
        return TimeZoneInfo.ConvertTimeToUtc(midnight, Zone);
    }
}

/// <summary>Outcome of reading the zone a client sent.</summary>
public enum RequestedZoneKind { NotProvided, Valid, Invalid }

/// <summary>
/// Mission day rule (docs/DAILY_MISSION.md, "Gün"): the user's pinned zone decides the day.
/// A requested zone is a valid IANA id, otherwise a valid <c>utcOffsetMinutes</c>. The day
/// never goes back before the user's last mission day and stays within ±1 day of the UTC date.
/// </summary>
public static class MissionDayResolver
{
    public const int MinOffsetMinutes = -12 * 60;
    public const int MaxOffsetMinutes = 14 * 60;
    public const int MaxTimeZoneLength = 64;
    /// <summary>A different zone replaces the pinned one at most once per this period.</summary>
    public static readonly TimeSpan ZoneChangeCooldown = TimeSpan.FromDays(7);
    /// <summary>A mission stays completable until this long after its local day ends.</summary>
    public static readonly TimeSpan CompletionGrace = TimeSpan.FromHours(6);

    /// <summary>Valid IANA id first; when it is missing or unknown, a valid offset (normalized to "UTC±hh:mm").</summary>
    public static RequestedZoneKind ReadRequested(string? timeZone, int? utcOffsetMinutes, out MissionZone zone)
    {
        zone = MissionZone.Utc;
        if (TryIana(timeZone, out var iana)) { zone = iana; return RequestedZoneKind.Valid; }
        if (utcOffsetMinutes is { } offset && offset is >= MinOffsetMinutes and <= MaxOffsetMinutes)
        {
            zone = FromOffset(offset);
            return RequestedZoneKind.Valid;
        }
        return string.IsNullOrWhiteSpace(timeZone) && utcOffsetMinutes is null ? RequestedZoneKind.NotProvided : RequestedZoneKind.Invalid;
    }

    /// <summary>
    /// Pinning rule: the first usable zone is stored; a different one replaces it only when the
    /// stored zone is at least <see cref="ZoneChangeCooldown"/> old. Returns null when nothing is usable (→ 400).
    /// <paramref name="persist"/> is true when the caller should store the returned zone.
    /// </summary>
    public static MissionZone? Effective(string? storedKey, DateTime? storedAtUtc, string? timeZone, int? utcOffsetMinutes, DateTime utcNow, out bool persist)
    {
        persist = false;
        var stored = string.IsNullOrWhiteSpace(storedKey) ? null : FromStoredKey(storedKey);
        var kind = ReadRequested(timeZone, utcOffsetMinutes, out var requested);
        switch (kind)
        {
            case RequestedZoneKind.Invalid:
                return stored;
            case RequestedZoneKind.NotProvided when stored is not null:
                return stored;
        }
        // Valid request, or nothing sent and nothing stored (UTC per the contract).
        if (stored is null) { persist = true; return requested; }
        if (stored.Key == requested.Key) return stored;
        if (storedAtUtc is null || utcNow - storedAtUtc.Value >= ZoneChangeCooldown) { persist = true; return requested; }
        return stored;
    }

    /// <summary>Parses a key stored on user settings or a mission; falls back to UTC.</summary>
    public static MissionZone FromStoredKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key == "UTC") return MissionZone.Utc;
        if (key.StartsWith("UTC", StringComparison.Ordinal) && key.Length == 9 && key[3] is '+' or '-' &&
            TimeSpan.TryParseExact(key[4..], @"hh\:mm", CultureInfo.InvariantCulture, out var span))
            return FromOffset((key[3] == '-' ? -1 : 1) * (int)span.TotalMinutes);
        return TryIana(key, out var zone) ? zone : MissionZone.Utc;
    }

    public static DateOnly Resolve(MissionZone zone, DateTime utcNow, DateOnly? lastMissionDate)
    {
        var utcDate = DateOnly.FromDateTime(utcNow);
        var date = zone.LocalDate(utcNow);
        if (lastMissionDate is { } last && date < last) date = last;
        if (date < utcDate.AddDays(-1)) date = utcDate.AddDays(-1);
        if (date > utcDate.AddDays(1)) date = utcDate.AddDays(1);
        return date;
    }

    /// <summary>Mission completion deadline: end of the mission's local day plus <see cref="CompletionGrace"/>.</summary>
    public static DateTime CompletionDeadlineUtc(string? missionZoneKey, DateOnly missionDate) =>
        FromStoredKey(missionZoneKey).EndOfDayUtc(missionDate).Add(CompletionGrace);

    private static bool TryIana(string? timeZone, out MissionZone zone)
    {
        zone = MissionZone.Utc;
        if (string.IsNullOrWhiteSpace(timeZone)) return false;
        var id = timeZone.Trim();
        if (id.Length > MaxTimeZoneLength) return false;
        if (id is "UTC" or "Etc/UTC") return true;
        // Only IANA ids are accepted so the stored key is portable between Windows and Linux hosts.
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(id, out var info) || !info.HasIanaId) return false;
        zone = new MissionZone(id, info, 0);
        return true;
    }

    private static MissionZone FromOffset(int offset)
    {
        if (offset == 0) return MissionZone.Utc;
        var abs = Math.Abs(offset);
        return new MissionZone($"UTC{(offset < 0 ? '-' : '+')}{abs / 60:00}:{abs % 60:00}", null, offset);
    }
}
