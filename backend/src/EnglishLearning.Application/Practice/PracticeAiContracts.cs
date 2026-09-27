namespace EnglishLearning.Application.Practice;

public interface IConversationAiProvider
{
    Task<ConversationReplyDto> ReplyAsync(ConversationReplyRequest request, CancellationToken ct);
}

public interface ITranscriptionProvider
{
    Task<TranscriptionDto> TranscribeAsync(TranscriptionRequest request, CancellationToken ct);
}
