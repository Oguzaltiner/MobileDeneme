namespace EnglishLearning.Application.Practice;

public sealed record ConversationTurnDto(string Speaker, string English, string Turkish, string Focus);
public sealed record ConversationScenarioDto(string Key, string Title, string Description, string Level, string Goal, IReadOnlyList<ConversationTurnDto> Turns);
public sealed record PronunciationAssessmentRequest(string Text, string Transcript, string? AudioBase64 = null);
public sealed record PronunciationAssessmentDto(int Score, string Grade, string Feedback, IReadOnlyList<string> FocusWords);
public sealed record ListeningExerciseDto(string Key, string Title, string Level, string Prompt, string Transcript, string Translation, IReadOnlyList<string> Options, string CorrectOption, string Tip);
public sealed record ConversationReplyRequest(string ScenarioKey, string Message, string? Level = null);
public sealed record ConversationReplyDto(string Reply, string TurkishHint, string Focus, bool IsFallback, IReadOnlyList<string> SuggestedReplies);
public sealed record TranscriptionRequest(string? AudioBase64, string? FallbackTranscript = null, string? Language = "en-US");
public sealed record TranscriptionDto(string Transcript, double Confidence, string Provider, bool IsFallback);

public interface IPracticeContentService
{
    IReadOnlyList<ConversationScenarioDto> GetConversationScenarios(string? level = null);
    IReadOnlyList<ListeningExerciseDto> GetListeningExercises(string? level = null);
    PronunciationAssessmentDto AssessPronunciation(PronunciationAssessmentRequest request);
    Task<ConversationReplyDto> GenerateReplyAsync(ConversationReplyRequest request, CancellationToken ct = default);
    Task<TranscriptionDto> TranscribeAsync(TranscriptionRequest request, CancellationToken ct = default);
}
