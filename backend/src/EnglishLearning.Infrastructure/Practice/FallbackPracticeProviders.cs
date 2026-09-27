using EnglishLearning.Application.Practice;

namespace EnglishLearning.Infrastructure.Practice;

/// Deterministic providers keep local development and offline demos useful. A hosted provider can replace these registrations later.
public sealed class FallbackConversationAiProvider : IConversationAiProvider
{
    public Task<ConversationReplyDto> ReplyAsync(ConversationReplyRequest request, CancellationToken ct)
    {
        var text = request.Message.Trim();
        var reply = request.ScenarioKey.ToLowerInvariant() switch
        {
            "airport" => ("Certainly. Gate twelve is on your left, after the coffee shop.", "Elbette. On iki numaralı kapı kahve dükkânından sonra solunuzda.", "asking for directions"),
            "restaurant" => ("Of course. I recommend the soup of the day.", "Elbette. Günün çorbasını öneririm.", "polite requests"),
            _ => ("That is a good point. Could you tell me more about it?", "Bu iyi bir nokta. Biraz daha anlatabilir misiniz?", "sharing an opinion")
        };
        return Task.FromResult(new ConversationReplyDto(reply.Item1, reply.Item2, reply.Item3, true,
            ["Could you repeat that, please?", "I would like to know more."]));
    }
}

public sealed class FallbackTranscriptionProvider : ITranscriptionProvider
{
    public Task<TranscriptionDto> TranscribeAsync(TranscriptionRequest request, CancellationToken ct)
    {
        // Audio decoding/provider credentials are intentionally isolated behind this contract.
        var transcript = request.FallbackTranscript?.Trim() ?? string.Empty;
        return Task.FromResult(new TranscriptionDto(transcript, string.IsNullOrEmpty(transcript) ? 0 : 0.5, "fallback", true));
    }
}
