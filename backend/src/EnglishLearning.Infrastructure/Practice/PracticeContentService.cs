using EnglishLearning.Application.Practice;

namespace EnglishLearning.Infrastructure.Practice;

public sealed class PracticeContentService(IConversationAiProvider conversationAi, ITranscriptionProvider transcription) : IPracticeContentService
{
    private static readonly IReadOnlyList<ConversationScenarioDto> Scenarios =
    [
        new("airport", "Havaalanında", "Check-in ve yön sorma ifadeleri.", "A2", "Seyahat ederken kendini rahat ifade et", [
            new("Agent", "Good morning. May I see your passport?", "Günaydın. Pasaportunuzu görebilir miyim?", "polite requests"),
            new("You", "Here you are. Where is gate twelve?", "Buyurun. On iki numaralı kapı nerede?", "where questions")
        ]),
        new("restaurant", "Restoranda", "Sipariş verirken doğal cümleler kur.", "A2", "Siparişini net ve kibar biçimde ver", [
            new("Waiter", "Are you ready to order?", "Sipariş vermeye hazır mısınız?", "present continuous"),
            new("You", "Could I see the menu? I would like some water.", "Menüyü görebilir miyim? Biraz su istiyorum.", "could / would like")
        ]),
        new("meeting", "İş görüşmesinde", "Kendini tanıt ve fikrini net ifade et.", "B1", "Toplantıda güvenle fikir belirt", [
            new("Colleague", "What is your view on this proposal?", "Bu teklif hakkındaki görüşünüz nedir?", "opinion phrases"),
            new("You", "Let me explain my experience. I agree with your proposal.", "Deneyimimi açıklayayım. Teklifinize katılıyorum.", "agreeing and explaining")
        ])
    ];

    private static readonly IReadOnlyList<ListeningExerciseDto> Listening =
    [
        new("travel-01", "At the airport", "A2", "Where should you go next?", "Your gate is next to the coffee shop.", "Kapınız kahve dükkânının yanında.", ["To the hotel", "To the gate", "To the restaurant", "To the office"], "To the gate", "Listen for the place after 'your gate'."),
        new("work-01", "A team update", "B1", "What will the speaker do first?", "I will review the report before the meeting.", "Toplantıdan önce raporu inceleyeceğim.", ["Call a client", "Review the report", "Join the meeting", "Write an email"], "Review the report", "'Before' tells you the order."),
        new("daily-01", "Making plans", "A2", "When will they meet?", "Let's meet at half past six this evening.", "Bu akşam altı buçukta buluşalım.", ["At five", "At six", "At six thirty", "Tomorrow"], "At six thirty", "Half past six means 6:30.")
    ];

    public IReadOnlyList<ConversationScenarioDto> GetConversationScenarios(string? level = null) => Filter(Scenarios, level);
    public IReadOnlyList<ListeningExerciseDto> GetListeningExercises(string? level = null) => Filter(Listening, level);

    public PronunciationAssessmentDto AssessPronunciation(PronunciationAssessmentRequest request)
    {
        var expected = Normalize(request.Text);
        var actual = Normalize(request.Transcript);
        if (string.IsNullOrWhiteSpace(actual)) return new(0, "Başlangıç", "Cümleni yüksek sesle tekrar edip tekrar dene.", expected.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var expectedWords = expected.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
        var actualWords = actual.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var matched = expectedWords.Count(actualWords.Contains);
        var score = expectedWords.Length == 0 ? 0 : (int)Math.Round(100d * matched / expectedWords.Length);
        var missing = expectedWords.Where(x => !actualWords.Contains(x)).Take(3).ToArray();
        var grade = score >= 90 ? "Harika" : score >= 75 ? "İyi" : score >= 50 ? "Gelişiyor" : "Başlangıç";
        var feedback = score >= 90 ? "Cümlenin tamamı net anlaşılıyor." : missing.Length > 0 ? $"Şu kelimeleri daha belirgin söyle: {string.Join(", ", missing)}." : "Ritmi koru ve cümleyi bir kez daha dene.";
        return new(score, grade, feedback, missing);
    }

    public Task<ConversationReplyDto> GenerateReplyAsync(ConversationReplyRequest request, CancellationToken ct = default) => conversationAi.ReplyAsync(request, ct);
    public Task<TranscriptionDto> TranscribeAsync(TranscriptionRequest request, CancellationToken ct = default) => transcription.TranscribeAsync(request, ct);

    private static string Normalize(string value) => new(value.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
    private static IReadOnlyList<T> Filter<T>(IReadOnlyList<T> items, string? level) where T : class => string.IsNullOrWhiteSpace(level) ? items : items.Where(x => x switch { ConversationScenarioDto s => s.Level.Equals(level, StringComparison.OrdinalIgnoreCase), ListeningExerciseDto l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase), _ => true }).ToList();
}
