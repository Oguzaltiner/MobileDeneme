using System.Text.Json;
using EnglishLearning.Application.Entitlements;
using EnglishLearning.Application.Missions;
using EnglishLearning.Application.Practice;
using EnglishLearning.Application.Vocabulary;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishLearning.Infrastructure.Missions;

/// <summary>
/// Daily mission (docs/DAILY_MISSION.md). Content, answer keys, XP and streak are decided
/// here; the mobile client only renders. Review/new-word steps are recorded by
/// <c>POST /reviews</c> with the mission's practice session; recall/listening answers are
/// <see cref="PracticeEvent"/> rows keyed <c>mission:{missionId:N}:{questionId:N}</c>.
/// </summary>
public sealed class DailyMissionService(
    EnglishLearningDbContext db,
    IEntitlementService entitlements,
    IPracticeContentService content,
    ILogger<DailyMissionService> logger) : IDailyMissionService
{
    public const string ReviewStep = "review";
    public const string NewWordsStep = "new-words";
    public const string RecallStep = "recall";
    public const string ListeningStep = "listening";
    public const int MaxReviewWords = 8;
    public const int MaxNewWords = 5;
    public const int RecallQuestionCount = 4;
    private const int DistractorCandidates = 15;

    private static readonly string[] Levels = ["A1", "A2", "B1", "B2", "C1", "C2"];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string AnswerEventId(Guid missionId, Guid questionId) => $"mission:{missionId:N}:{questionId:N}";

    public async Task<MissionTodayDto> GetTodayAsync(Guid userId, string? timeZone, int? utcOffsetMinutes, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        // Read-only: the requested zone is evaluated with the pinning rule but stored only by POST /today.
        var zone = await EffectiveZoneAsync(userId, timeZone, utcOffsetMinutes, now, persist: false, ct);
        var date = MissionDayResolver.Resolve(zone, now, await LastMissionDateAsync(userId, ct));
        var entitlement = await entitlements.GetAsync(userId, ct);
        var completedDates = await CompletedDatesAsync(userId, ct);
        var completedToday = completedDates.Contains(date);
        var streak = new MissionStreakDto(RunEndingAt(completedDates, completedToday ? date : date.AddDays(-1)), Longest(completedDates), completedToday);
        var limits = new MissionLimitsDto(entitlement.IsPremium, entitlement.IsPremium ? -1 : RemainingWords(entitlement));

        var pendingCandidates = await db.DailyMissions.AsNoTracking()
            .Where(x => x.UserId == userId && x.MissionDate < date && x.Status == DailyMissionStatus.InProgress)
            .OrderByDescending(x => x.MissionDate).Take(3).ToListAsync(ct);
        var pendingMission = pendingCandidates.FirstOrDefault(x => now <= MissionDayResolver.CompletionDeadlineUtc(x.TimeZone, x.MissionDate));
        var pending = pendingMission is null ? null : await BuildDtoAsync(pendingMission, entitlement, ct);

        var mission = await db.DailyMissions.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.MissionDate == date, ct);
        if (mission is not null)
        {
            var dto = await BuildDtoAsync(mission, entitlement, ct);
            var reviewRequired = dto.Steps.FirstOrDefault(x => x.Key == ReviewStep)?.Required ?? 0;
            var newRequired = dto.Steps.FirstOrDefault(x => x.Key == NewWordsStep)?.Required ?? 0;
            return new(date, dto.Status, dto, new(dto.EstimatedMinutes, reviewRequired, newRequired, dto.Listening is not null), streak, limits, pending);
        }

        var level = await EffectiveLevelAsync(userId, entitlement, ct);
        var remaining = entitlement.IsPremium ? int.MaxValue : RemainingWords(entitlement);
        var reviewCount = Math.Min(remaining, await DueWords(userId, entitlement, now).Take(MaxReviewWords).CountAsync(ct));
        var newCount = Math.Min(remaining - reviewCount, await NewWords(userId, level).Take(MaxNewWords).CountAsync(ct));
        var hasListening = PickListening(level, entitlement.MaxLevel, date) is not null;
        var preview = new MissionPreviewDto(EstimateMinutes(reviewCount, newCount, RecallQuestionCount, hasListening), reviewCount, newCount, hasListening);
        return new(date, "notStarted", null, preview, streak, limits, pending);
    }

    public async Task<DailyMissionDto> StartTodayAsync(Guid userId, StartMissionRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var zone = await EffectiveZoneAsync(userId, request.TimeZone, request.UtcOffsetMinutes, now, persist: true, ct);
        var date = MissionDayResolver.Resolve(zone, now, await LastMissionDateAsync(userId, ct));
        var entitlement = await entitlements.GetAsync(userId, ct);
        var existing = await db.DailyMissions.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.MissionDate == date, ct);
        if (existing is not null) return await BuildDtoAsync(existing, entitlement, ct);

        var level = await EffectiveLevelAsync(userId, entitlement, ct);
        // Free users: the mission is sized to the remaining daily word quota (reviews and new
        // words consume it through POST /reviews). The quiz quota is never touched.
        var remaining = entitlement.IsPremium ? int.MaxValue : RemainingWords(entitlement);
        var reviewWords = remaining == 0 ? new List<VocabularyWord>() : await DueWords(userId, entitlement, now)
            .Take(Math.Min(MaxReviewWords, remaining)).Select(x => x.VocabularyWord).ToListAsync(ct);
        var newTake = Math.Min(MaxNewWords, remaining - reviewWords.Count);
        var newWords = newTake <= 0 ? new List<VocabularyWord>() : await NewWords(userId, level)
            .OrderBy(x => x.Level == level ? 0 : 1).ThenBy(_ => EF.Functions.Random())
            .Take(newTake).ToListAsync(ct);

        var recallWords = await PickRecallWordsAsync(userId, entitlement, level, reviewWords.Concat(newWords).ToList(), ct);
        var recall = new List<RecallItem>();
        for (var i = 0; i < recallWords.Count; i++)
            if (await BuildRecallAsync(recallWords[i], i, level, ct) is { } item) recall.Add(item);
        var listening = PickListening(level, entitlement.MaxLevel, date) is { } exercise ? BuildListening(exercise) : null;

        var payload = new MissionPayload(reviewWords.Select(x => x.Id).ToList(), newWords.Select(x => x.Id).ToList(), recall, listening);
        var session = new PracticeSession { UserId = userId, PathKey = DailyMission.PracticePathKey, StartedAtUtc = now };
        var steps = new List<(string Key, string Title, int Minutes)>
        {
            (ReviewStep, "Tekrar", EstimateMinutes(reviewWords.Count, 0, 0, false)),
            (NewWordsStep, "Yeni kelimeler", EstimateMinutes(0, newWords.Count, 0, false)),
            (RecallStep, "Hatırlama", EstimateMinutes(0, 0, recall.Count, false))
        };
        if (listening is not null) steps.Add((ListeningStep, "Dinleme", 1));
        session.Steps = steps.Select((x, i) => new PracticeSessionStep { SessionId = session.Id, Order = i + 1, Key = x.Key, Title = x.Title, EstimatedMinutes = x.Minutes }).ToList();
        var mission = new DailyMission
        {
            UserId = userId,
            PracticeSessionId = session.Id,
            PracticeSession = session,
            MissionDate = date,
            TimeZone = zone.Key,
            PayloadJson = JsonSerializer.Serialize(payload, Json),
            CreatedAtUtc = now
        };
        db.DailyMissions.Add(mission);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // A parallel request created today's mission first: return that one (idempotent create).
            db.ChangeTracker.Clear();
            var winner = await db.DailyMissions.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.MissionDate == date, ct);
            if (winner is null) throw;
            logger.LogWarning("Daily mission create for user {UserId} on {MissionDate} lost a race; returning mission {MissionId}.", userId, date, winner.Id);
            return await BuildDtoAsync(winner, entitlement, ct);
        }
        db.ChangeTracker.Clear();
        return await BuildDtoAsync(mission, entitlement, ct);
    }

    public async Task<MissionAnswerResultDto?> AnswerAsync(Guid userId, Guid missionId, MissionAnswerRequest request, CancellationToken ct)
    {
        var mission = await db.DailyMissions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == missionId && x.UserId == userId, ct);
        if (mission is null) return null;
        var payload = ReadPayload(mission);
        var stepKey = request.StepKey?.Trim().ToLowerInvariant() ?? "";
        List<Guid> questionIds;
        AnswerKey? question = null;
        if (stepKey == RecallStep)
        {
            questionIds = payload.Recall.Select(x => x.Id).ToList();
            if (payload.Recall.FirstOrDefault(x => x.Id == request.QuestionId) is { } r)
                question = new AnswerKey(r.WordId, r.Term, r.Translation, r.Options, r.CorrectKey, r.Explanation);
        }
        else if (stepKey == ListeningStep)
        {
            questionIds = payload.Listening is null ? [] : [payload.Listening.Id];
            if (payload.Listening is { } l && l.Id == request.QuestionId)
                question = new AnswerKey(null, null, null, l.Options, l.CorrectKey, l.Explanation);
        }
        else throw new InvalidMissionRequestException("Bilinmeyen görev adımı.");
        if (question is null) throw new InvalidMissionRequestException("Bu adımda böyle bir soru yok.");
        if (mission.Status == DailyMissionStatus.Completed)
            throw new MissionConflictException("Bu görev tamamlandı; cevap kabul edilmiyor.");

        var clientEventId = AnswerEventId(mission.Id, request.QuestionId);
        // First answer wins: a replay (or a parallel duplicate) returns the stored result.
        var stored = await StoredAnswerAsync(userId, mission.PracticeSessionId, clientEventId, ct);
        if (stored is null)
        {
            var optionKey = request.OptionKey?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(optionKey) || question.Options.All(x => x.Key != optionKey))
                throw new InvalidMissionRequestException("Geçersiz şık.");
            db.PracticeEvents.Add(new PracticeEvent
            {
                UserId = userId,
                SessionId = mission.PracticeSessionId,
                VocabularyWordId = question.WordId,
                StepKey = stepKey,
                IsCorrect = optionKey == question.CorrectKey,
                ClientEventId = clientEventId
            });
            try
            {
                await db.SaveChangesAsync(ct);
                stored = optionKey == question.CorrectKey;
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                db.ChangeTracker.Clear();
                logger.LogWarning("Mission {MissionId} question {QuestionId} was answered concurrently; returning the stored answer.", mission.Id, request.QuestionId);
                stored = await StoredAnswerAsync(userId, mission.PracticeSessionId, clientEventId, ct)
                         ?? throw new InvalidOperationException("Mission answer conflict could not be resolved.", ex);
            }
        }

        var answeredIds = questionIds.Select(x => AnswerEventId(mission.Id, x)).ToList();
        var done = await db.PracticeEvents.AsNoTracking().CountAsync(x => x.UserId == userId && x.SessionId == mission.PracticeSessionId && x.ClientEventId != null && answeredIds.Contains(x.ClientEventId), ct);
        var completed = done >= questionIds.Count;
        if (completed)
            await db.PracticeSessionSteps.Where(x => x.SessionId == mission.PracticeSessionId && x.Key == stepKey && !x.Completed)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Completed, true).SetProperty(x => x.CompletedAtUtc, DateTime.UtcNow), ct);
        return new(request.QuestionId, stored.Value, question.CorrectKey, question.Explanation, question.WordId, question.Term, question.Translation,
            new(stepKey, done, questionIds.Count, completed));
    }

    public async Task<MissionResultDto?> CompleteAsync(Guid userId, Guid missionId, CancellationToken ct)
    {
        var mission = await db.DailyMissions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == missionId && x.UserId == userId, ct);
        if (mission is null) return null;
        var entitlement = await entitlements.GetAsync(userId, ct);
        if (mission.Status == DailyMissionStatus.Completed) return await StoredResultAsync(mission, entitlement, ct);

        var now = DateTime.UtcNow;
        if (now > MissionDayResolver.CompletionDeadlineUtc(mission.TimeZone, mission.MissionDate))
        {
            logger.LogWarning("Mission {MissionId} completion rejected: window closed for {MissionDate} ({TimeZone}).", mission.Id, mission.MissionDate, mission.TimeZone);
            throw new MissionIncompleteException("Bu görevin tamamlama süresi doldu.", []);
        }
        var payload = ReadPayload(mission);
        var progress = await ProgressAsync(mission, payload, entitlement, ct);
        var incomplete = progress.Steps.Where(x => !x.Completed).Select(x => x.Key).ToList();
        if (incomplete.Count > 0)
        {
            logger.LogWarning("Mission {MissionId} completion rejected: incomplete steps {Steps}.", mission.Id, string.Join(",", incomplete));
            throw new MissionIncompleteException("Görevin tamamlanmamış adımları var.", incomplete);
        }

        var completedDates = await CompletedDatesAsync(userId, ct);
        completedDates.Add(mission.MissionDate);
        var streak = RunEndingAt(completedDates, mission.MissionDate);
        var breakdown = MissionXpRules.Calculate(progress.ReviewDone, progress.NewDone, progress.CorrectAnswers, streak);
        var xp = MissionXpRules.Total(breakdown);

        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            // Conditional update: only the request that flips InProgress -> Completed awards XP.
            var affected = await db.DailyMissions
                .Where(x => x.Id == mission.Id && x.Status == DailyMissionStatus.InProgress)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, DailyMissionStatus.Completed)
                    .SetProperty(x => x.CompletedAtUtc, now)
                    .SetProperty(x => x.XpAwarded, xp)
                    .SetProperty(x => x.CorrectAnswers, progress.CorrectAnswers)
                    .SetProperty(x => x.TotalAnswers, progress.TotalAnswers)
                    .SetProperty(x => x.ReviewedWords, progress.ReviewDone)
                    .SetProperty(x => x.NewWordsLearned, progress.NewDone), ct);
            if (affected == 0)
            {
                await transaction.RollbackAsync(ct);
                logger.LogWarning("Mission {MissionId} was completed by a concurrent request; returning the stored result.", mission.Id);
                var winner = await db.DailyMissions.AsNoTracking().SingleAsync(x => x.Id == mission.Id, ct);
                return await StoredResultAsync(winner, entitlement, ct);
            }
            await db.PracticeSessionSteps.Where(x => x.SessionId == mission.PracticeSessionId && !x.Completed)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Completed, true).SetProperty(x => x.CompletedAtUtc, now), ct);
            await db.PracticeSessions.Where(x => x.Id == mission.PracticeSessionId && x.Status == PracticeSessionStatus.InProgress)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, PracticeSessionStatus.Completed).SetProperty(x => x.CompletedAtUtc, now), ct);
            await transaction.CommitAsync(ct);
        }
        logger.LogInformation("Mission {MissionId} completed by user {UserId}: {Xp} XP (streak {Streak}).", mission.Id, userId, xp, streak);
        return new(mission.Id, xp, breakdown, progress.CorrectAnswers, progress.TotalAnswers, progress.ReviewDone, progress.NewDone,
            new(streak, Longest(completedDates), streak > 1), false, await TomorrowAsync(mission, entitlement, ct));
    }

    private async Task<DailyMissionDto> BuildDtoAsync(DailyMission mission, EntitlementDto entitlement, CancellationToken ct)
    {
        var payload = ReadPayload(mission);
        var progress = await ProgressAsync(mission, payload, entitlement, ct);
        var storedSteps = await db.PracticeSessionSteps.AsNoTracking().Where(x => x.SessionId == mission.PracticeSessionId)
            .OrderBy(x => x.Order).Select(x => new { x.Key, x.Order, x.Title }).ToListAsync(ct);
        var steps = storedSteps.Select(s => progress.Steps.FirstOrDefault(p => p.Key == s.Key) is { } p
                ? new MissionStepDto(s.Key, s.Order, s.Title, p.Required, p.Done, p.Completed)
                : new MissionStepDto(s.Key, s.Order, s.Title, 0, 0, true))
            .ToList();
        var recall = payload.Recall.Select(q => progress.Answers.TryGetValue(q.Id, out var correct)
            ? new MissionRecallQuestionDto(q.Id, q.Prompt, q.SpeakText, Options(q.Options), true, correct)
            : new MissionRecallQuestionDto(q.Id, q.Prompt, q.SpeakText, Options(q.Options), false, null)).ToList();
        MissionListeningDto? listening = null;
        if (payload.Listening is { } l)
        {
            var answered = progress.Answers.TryGetValue(l.Id, out var correct);
            listening = new(l.Id, l.Title, l.Level, l.Prompt, l.Transcript, Options(l.Options), answered, answered ? correct : null);
        }
        var result = mission.Status == DailyMissionStatus.Completed ? await StoredResultAsync(mission, entitlement, ct) : null;
        return new(mission.Id, mission.PracticeSessionId, mission.MissionDate, StatusText(mission.Status),
            EstimateMinutes(payload.ReviewWordIds.Count, payload.NewWordIds.Count, payload.Recall.Count, payload.Listening is not null),
            steps, progress.ReviewWords, progress.NewWords, recall, listening, result);
    }

    private async Task<MissionProgress> ProgressAsync(DailyMission mission, MissionPayload payload, EntitlementDto entitlement, CancellationToken ct)
    {
        var wordIds = payload.ReviewWordIds.Concat(payload.NewWordIds).Distinct().ToList();
        var words = await db.VocabularyWords.AsNoTracking()
            .Where(x => wordIds.Contains(x.Id) && x.PublicationStatus == VocabularyPublicationStatus.Published)
            .Select(x => new VocabularyWordDto(x.Id, x.Term, x.Pronunciation, x.PartOfSpeech, x.Definition, x.Translation, x.Level, x.Category, x.ExampleSentence))
            .ToDictionaryAsync(x => x.Id, ct);
        var events = await db.PracticeEvents.AsNoTracking()
            .Where(x => x.UserId == mission.UserId && x.SessionId == mission.PracticeSessionId)
            .Select(x => new { x.StepKey, x.VocabularyWordId, x.IsCorrect, x.ClientEventId })
            .ToListAsync(ct);
        // Review/new-word progress = distinct mission words reviewed through POST /reviews with this session.
        var reviewed = events.Where(x => x.VocabularyWordId is not null && (x.StepKey == ReviewStep || x.StepKey == NewWordsStep))
            .Select(x => x.VocabularyWordId!.Value).ToHashSet();
        var answerEvents = events.Where(x => x.ClientEventId is not null && (x.StepKey == RecallStep || x.StepKey == ListeningStep))
            .GroupBy(x => x.ClientEventId!).ToDictionary(x => x.Key, x => x.First().IsCorrect == true);
        var answers = new Dictionary<Guid, bool>();
        var questionIds = payload.Recall.Select(x => x.Id).ToList();
        if (payload.Listening is not null) questionIds.Add(payload.Listening.Id);
        foreach (var id in questionIds)
            if (answerEvents.TryGetValue(AnswerEventId(mission.Id, id), out var correct)) answers[id] = correct;

        // Words unpublished after creation cannot be reviewed any more, so they no longer count as required.
        var reviewWords = payload.ReviewWordIds.Where(words.ContainsKey).Select(x => words[x]).ToList();
        var newWords = payload.NewWordIds.Where(words.ContainsKey).Select(x => words[x]).ToList();
        var reviewDone = reviewWords.Count(x => reviewed.Contains(x.Id));
        var newDone = newWords.Count(x => reviewed.Contains(x.Id));
        var recallDone = payload.Recall.Count(x => answers.ContainsKey(x.Id));
        var listeningDone = payload.Listening is { } li && answers.ContainsKey(li.Id) ? 1 : 0;
        var isCompleted = mission.Status == DailyMissionStatus.Completed;

        // Free quota exhausted mid-mission: word steps are "capped" and count as completed. The flag
        // is persisted so the steps stay completed after the UTC quota reset.
        var capped = mission.WordStepsCapped;
        if (!capped && !isCompleted && !entitlement.IsPremium && RemainingWords(entitlement) == 0)
        {
            capped = true;
            await db.DailyMissions.Where(x => x.Id == mission.Id && !x.WordStepsCapped && x.Status == DailyMissionStatus.InProgress)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.WordStepsCapped, true), ct);
        }
        var steps = new List<MissionStepProgressDto>
        {
            new(ReviewStep, reviewDone, reviewWords.Count, isCompleted || capped || reviewDone >= reviewWords.Count),
            new(NewWordsStep, newDone, newWords.Count, isCompleted || capped || newDone >= newWords.Count),
            new(RecallStep, recallDone, payload.Recall.Count, isCompleted || recallDone >= payload.Recall.Count)
        };
        if (payload.Listening is not null) steps.Add(new(ListeningStep, listeningDone, 1, isCompleted || listeningDone == 1));
        return new(steps, reviewWords, newWords, answers, reviewDone, newDone, answers.Count(x => x.Value), answers.Count);
    }

    private async Task<MissionResultDto> StoredResultAsync(DailyMission mission, EntitlementDto entitlement, CancellationToken ct)
    {
        var items = mission.ReviewedWords * MissionXpRules.PerReview + mission.NewWordsLearned * MissionXpRules.PerNewWord;
        var accuracy = mission.CorrectAnswers * MissionXpRules.PerCorrectAnswer;
        var breakdown = new MissionXpBreakdownDto(MissionXpRules.Base, items, accuracy, Math.Max(0, mission.XpAwarded - MissionXpRules.Base - items - accuracy));
        // Mission days never move backwards, so the run ending at this mission's day is stable.
        var completedDates = (await CompletedDatesAsync(mission.UserId, ct)).Where(x => x <= mission.MissionDate).ToHashSet();
        var current = RunEndingAt(completedDates, mission.MissionDate);
        var streak = new MissionResultStreakDto(current, Longest(completedDates), current > 1);
        return new(mission.Id, mission.XpAwarded, breakdown, mission.CorrectAnswers, mission.TotalAnswers, mission.ReviewedWords, mission.NewWordsLearned,
            streak, true, await TomorrowAsync(mission, entitlement, ct));
    }

    private async Task<MissionTomorrowDto> TomorrowAsync(DailyMission mission, EntitlementDto entitlement, CancellationToken ct)
    {
        var date = mission.MissionDate.AddDays(1);
        var level = await EffectiveLevelAsync(mission.UserId, entitlement, ct);
        // Tomorrow the free quota starts fresh.
        var quota = entitlement.IsPremium ? int.MaxValue : Math.Max(0, entitlement.DailyWordLimit);
        var due = Math.Min(quota, await DueWords(mission.UserId, entitlement, DateTime.UtcNow.AddDays(1)).Take(MaxReviewWords).CountAsync(ct));
        var fresh = Math.Min(quota - due, await NewWords(mission.UserId, level).Take(MaxNewWords).CountAsync(ct));
        var hasListening = PickListening(level, entitlement.MaxLevel, date) is not null;
        return new(date, due, fresh, EstimateMinutes(due, fresh, RecallQuestionCount, hasListening));
    }

    private async Task<List<VocabularyWord>> PickRecallWordsAsync(Guid userId, EntitlementDto entitlement, string level, List<VocabularyWord> missionWords, CancellationToken ct)
    {
        var picked = missionWords.OrderBy(_ => Random.Shared.Next()).Take(RecallQuestionCount).ToList();
        if (picked.Count >= RecallQuestionCount) return picked;
        // Quota-capped or tiny missions: top up with words the user already studied, then with level-appropriate words.
        var exclude = picked.Select(x => x.Id).ToList();
        var allowed = LevelsUpTo(entitlement.MaxLevel);
        picked.AddRange(await db.UserWordProgress.AsNoTracking()
            .Where(x => x.UserId == userId && !exclude.Contains(x.VocabularyWordId) && x.VocabularyWord.PublicationStatus == VocabularyPublicationStatus.Published && allowed.Contains(x.VocabularyWord.Level))
            .OrderByDescending(x => x.LastReviewedAtUtc).Take(RecallQuestionCount - picked.Count).Select(x => x.VocabularyWord).ToListAsync(ct));
        if (picked.Count >= RecallQuestionCount) return picked;
        exclude = picked.Select(x => x.Id).ToList();
        var levels = LevelsUpTo(level);
        picked.AddRange(await db.VocabularyWords.AsNoTracking()
            .Where(x => !exclude.Contains(x.Id) && x.PublicationStatus == VocabularyPublicationStatus.Published && levels.Contains(x.Level))
            .OrderBy(_ => EF.Functions.Random()).Take(RecallQuestionCount - picked.Count).ToListAsync(ct));
        return picked;
    }

    /// <summary>
    /// Same distractor rule as the quiz engine (unique, never another spelling of the answer, target
    /// level preferred), drawn in the database from levels up to min(user level, entitlement MaxLevel).
    /// </summary>
    private async Task<RecallItem?> BuildRecallAsync(VocabularyWord word, int index, string level, CancellationToken ct)
    {
        var askTranslation = index % 2 == 0;
        var prompt = askTranslation ? word.Term : word.Translation;
        var correct = askTranslation ? word.Translation : word.Term;
        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(correct)) return null;
        var levels = LevelsUpTo(level);
        var wordRank = Rank(word.Level);
        var nearby = Levels.Where(x => Math.Abs(Rank(x) - wordRank) == 1).ToList();
        var correctLower = correct.ToLower();
        var query = db.VocabularyWords.AsNoTracking()
            .Where(x => x.Id != word.Id && x.PublicationStatus == VocabularyPublicationStatus.Published && levels.Contains(x.Level));
        query = askTranslation ? query.Where(x => x.Translation.ToLower() != correctLower) : query.Where(x => x.Term.ToLower() != correctLower);
        var candidates = await query
            .OrderBy(x => x.Level == word.Level ? 0 : nearby.Contains(x.Level) ? 1 : 2).ThenBy(_ => EF.Functions.Random())
            .Take(DistractorCandidates)
            .Select(x => askTranslation ? x.Translation : x.Term)
            .ToListAsync(ct);
        var distractors = candidates
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals(correct, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
        if (distractors.Count < 3) return null;
        var options = distractors.Append(correct).OrderBy(_ => Random.Shared.Next())
            .Select((text, i) => new OptionItem(((char)('A' + i)).ToString(), text)).ToList();
        return new RecallItem(Guid.NewGuid(), word.Id, prompt, askTranslation ? word.Term : null, options,
            options.Single(x => x.Text == correct).Key, $"{word.Term} = {word.Translation}. {word.Definition}", word.Term, word.Translation);
    }

    private ListeningExerciseDto? PickListening(string level, string maxLevel, DateOnly date)
    {
        var maxRank = Rank(maxLevel);
        var candidates = content.GetListeningExercises().Where(x => Rank(x.Level) >= 0 && Rank(x.Level) <= maxRank && x.Options.Contains(x.CorrectOption)).ToList();
        if (candidates.Count == 0) return null;
        // Closest level at or below the learner's level; otherwise the easiest available item.
        var rank = Rank(level);
        var atOrBelow = candidates.Where(x => Rank(x.Level) <= rank).ToList();
        var targetRank = atOrBelow.Count > 0 ? atOrBelow.Max(x => Rank(x.Level)) : candidates.Min(x => Rank(x.Level));
        var pool = candidates.Where(x => Rank(x.Level) == targetRank).OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
        return pool[date.DayNumber % pool.Count];
    }

    private static ListeningItem BuildListening(ListeningExerciseDto exercise)
    {
        var options = exercise.Options.Distinct(StringComparer.Ordinal).OrderBy(_ => Random.Shared.Next())
            .Select((text, i) => new OptionItem(((char)('A' + i)).ToString(), text)).ToList();
        return new ListeningItem(Guid.NewGuid(), exercise.Key, exercise.Title, exercise.Level, exercise.Prompt, exercise.Transcript,
            options, options.First(x => x.Text == exercise.CorrectOption).Key, $"{exercise.Translation} {exercise.Tip}".Trim());
    }

    private IQueryable<UserWordProgress> DueWords(Guid userId, EntitlementDto entitlement, DateTime dueBefore)
    {
        var allowed = LevelsUpTo(entitlement.MaxLevel);
        return db.UserWordProgress.AsNoTracking()
            .Where(x => x.UserId == userId && x.DueAtUtc <= dueBefore && x.VocabularyWord.PublicationStatus == VocabularyPublicationStatus.Published && allowed.Contains(x.VocabularyWord.Level))
            .OrderBy(x => x.DueAtUtc);
    }

    private IQueryable<VocabularyWord> NewWords(Guid userId, string level)
    {
        var levels = LevelsUpTo(level);
        return db.VocabularyWords.AsNoTracking()
            .Where(x => x.PublicationStatus == VocabularyPublicationStatus.Published && levels.Contains(x.Level)
                        && !db.UserWordProgress.Any(p => p.UserId == userId && p.VocabularyWordId == x.Id));
    }

    /// <summary>
    /// Applies the pinned-zone rule (docs/DAILY_MISSION.md, "Gün"). Throws (→ 400) only when the
    /// request is unusable and nothing is stored. With <paramref name="persist"/> a new pin is saved.
    /// </summary>
    private async Task<MissionZone> EffectiveZoneAsync(Guid userId, string? timeZone, int? utcOffsetMinutes, DateTime now, bool persist, CancellationToken ct)
    {
        var settings = await db.UserSettings.AsNoTracking().Where(x => x.UserId == userId)
            .Select(x => new { x.TimeZone, x.TimeZoneUpdatedAtUtc }).SingleOrDefaultAsync(ct);
        var zone = MissionDayResolver.Effective(settings?.TimeZone, settings?.TimeZoneUpdatedAtUtc, timeZone, utcOffsetMinutes, now, out var store)
                   ?? throw new InvalidMissionRequestException("Geçersiz saat dilimi.");
        if (persist && store && settings is not null)
        {
            // Conditional on the value read above, so two parallel first requests cannot flip-flop the pin.
            await db.UserSettings
                .Where(x => x.UserId == userId && x.TimeZone == settings.TimeZone && x.TimeZoneUpdatedAtUtc == settings.TimeZoneUpdatedAtUtc)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TimeZone, zone.Key).SetProperty(x => x.TimeZoneUpdatedAtUtc, now), ct);
        }
        return zone;
    }

    /// <summary>min(user level, entitlement MaxLevel); A1 when the user has no level yet.</summary>
    private async Task<string> EffectiveLevelAsync(Guid userId, EntitlementDto entitlement, CancellationToken ct)
    {
        var current = await db.UserSettings.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.CurrentLevel).SingleOrDefaultAsync(ct);
        var rank = Rank(current?.Trim().ToUpperInvariant());
        var max = Math.Max(0, Rank(entitlement.MaxLevel));
        return Levels[Math.Min(rank < 0 ? 0 : rank, max)];
    }

    private async Task<DateOnly?> LastMissionDateAsync(Guid userId, CancellationToken ct) =>
        await db.DailyMissions.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.MissionDate).Select(x => (DateOnly?)x.MissionDate).FirstOrDefaultAsync(ct);

    private async Task<HashSet<DateOnly>> CompletedDatesAsync(Guid userId, CancellationToken ct) =>
        (await db.DailyMissions.AsNoTracking().Where(x => x.UserId == userId && x.Status == DailyMissionStatus.Completed).Select(x => x.MissionDate).ToListAsync(ct)).ToHashSet();

    private async Task<bool?> StoredAnswerAsync(Guid userId, Guid sessionId, string clientEventId, CancellationToken ct) =>
        await db.PracticeEvents.AsNoTracking().Where(x => x.UserId == userId && x.SessionId == sessionId && x.ClientEventId == clientEventId)
            .Select(x => (bool?)(x.IsCorrect == true)).FirstOrDefaultAsync(ct);

    private static int RunEndingAt(IReadOnlySet<DateOnly> dates, DateOnly end)
    {
        var run = 0;
        while (dates.Contains(end.AddDays(-run))) run++;
        return run;
    }

    private static int Longest(IEnumerable<DateOnly> dates)
    {
        int longest = 0, current = 0; DateOnly? previous = null;
        foreach (var day in dates.OrderBy(x => x))
        {
            current = previous is { } p && day == p.AddDays(1) ? current + 1 : 1;
            longest = Math.Max(longest, current); previous = day;
        }
        return longest;
    }

    private static int EstimateMinutes(int reviews, int newWords, int recall, bool listening) =>
        Math.Max(1, (reviews * 15 + newWords * 30 + recall * 15 + (listening ? 60 : 0)) / 60);

    private static int RemainingWords(EntitlementDto entitlement) => Math.Max(0, entitlement.DailyWordLimit - entitlement.DailyWordsUsed);
    private static int Rank(string? level) => level is null ? -1 : Array.IndexOf(Levels, level);
    private static List<string> LevelsUpTo(string level) => Levels.Take(Math.Max(0, Rank(level)) + 1).ToList();
    private static string StatusText(DailyMissionStatus status) => status == DailyMissionStatus.Completed ? "completed" : "inProgress";
    private static IReadOnlyList<MissionOptionDto> Options(IEnumerable<OptionItem> options) => options.Select(x => new MissionOptionDto(x.Key, x.Text)).ToList();
    private static MissionPayload ReadPayload(DailyMission mission) =>
        JsonSerializer.Deserialize<MissionPayload>(mission.PayloadJson, Json) ?? throw new InvalidOperationException($"Daily mission {mission.Id} has no payload.");

    // Server-only payload (stored as jsonb). Correct keys never leave the server before an answer.
    private sealed record MissionPayload(List<Guid> ReviewWordIds, List<Guid> NewWordIds, List<RecallItem> Recall, ListeningItem? Listening);
    private sealed record RecallItem(Guid Id, Guid WordId, string Prompt, string? SpeakText, List<OptionItem> Options, string CorrectKey, string Explanation, string Term, string Translation);
    private sealed record ListeningItem(Guid Id, string ExerciseKey, string Title, string Level, string Prompt, string Transcript, List<OptionItem> Options, string CorrectKey, string Explanation);
    private sealed record OptionItem(string Key, string Text);
    private sealed record AnswerKey(Guid? WordId, string? Term, string? Translation, List<OptionItem> Options, string CorrectKey, string Explanation);
    private sealed record MissionProgress(List<MissionStepProgressDto> Steps, List<VocabularyWordDto> ReviewWords, List<VocabularyWordDto> NewWords, Dictionary<Guid, bool> Answers, int ReviewDone, int NewDone, int CorrectAnswers, int TotalAnswers);
}
