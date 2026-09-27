namespace EnglishLearning.Application.Practice;

public sealed record ConversationTurnDto(string Speaker, string English, string Turkish, string Focus);
public sealed record ConversationScenarioDto(string Key, string Title, string Description, string Level, string Goal, IReadOnlyList<ConversationTurnDto> Turns);
public sealed record PronunciationAssessmentRequest(string Text, string Transcript);
public sealed record PronunciationAssessmentDto(int Score, string Grade, string Feedback, IReadOnlyList<string> FocusWords);
public sealed record ListeningExerciseDto(string Key, string Title, string Level, string Prompt, string Transcript, string Translation, IReadOnlyList<string> Options, string CorrectOption, string Tip);

public interface IPracticeContentService
{
    IReadOnlyList<ConversationScenarioDto> GetConversationScenarios(string? level = null);
    IReadOnlyList<ListeningExerciseDto> GetListeningExercises(string? level = null);
    PronunciationAssessmentDto AssessPronunciation(PronunciationAssessmentRequest request);
}
