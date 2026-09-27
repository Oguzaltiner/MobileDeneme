using EnglishLearning.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Persistence;

/// <summary>Development catalog used to keep the local product loop useful before the admin studio is populated.</summary>
public static class VocabularyCatalogSeeder
{
    private static readonly (string Term, string Pronunciation, string PartOfSpeech, string Definition, string Translation, string Level, string Category, string Example)[] Catalog =
    [
        ("adapt", "/əˈdæpt/", "verb", "to change to fit a new situation", "uyum sağlamak", "B1", "Work", "You must adapt to a new environment."),
        ("advice", "/ədˈvaɪs/", "noun", "an opinion about what someone should do", "tavsiye", "A2", "Daily Life", "Can I ask for your advice?"),
        ("afford", "/əˈfɔːd/", "verb", "to have enough money or time for something", "karşılayabilmek", "B1", "Daily Life", "I cannot afford a long holiday."),
        ("arrange", "/əˈreɪndʒ/", "verb", "to organize or plan something", "düzenlemek", "B1", "Work", "We arranged a meeting for Friday."),
        ("aware", "/əˈweə/", "adjective", "knowing about something", "farkında", "B1", "Daily Life", "Are you aware of the change?"),
        ("benefit", "/ˈbenɪfɪt/", "noun", "an advantage or good result", "fayda", "B1", "Academic", "Exercise has many health benefits."),
        ("challenge", "/ˈtʃælɪndʒ/", "noun", "a difficult task or situation", "zorluk", "B1", "Work", "Learning a language is a rewarding challenge."),
        ("communicate", "/kəˈmjuːnɪkeɪt/", "verb", "to share information or ideas", "iletişim kurmak", "B1", "Work", "Try to communicate clearly."),
        ("compare", "/kəmˈpeə/", "verb", "to examine how things are similar or different", "karşılaştırmak", "B1", "Academic", "Compare the two answers carefully."),
        ("complete", "/kəmˈpliːt/", "verb", "to finish something", "tamamlamak", "A2", "Daily Life", "Please complete the exercise."),
        ("confident", "/ˈkɒnfɪdənt/", "adjective", "feeling sure about your ability", "kendine güvenen", "B1", "Personality", "She feels confident speaking English."),
        ("consider", "/kənˈsɪdə/", "verb", "to think carefully about something", "düşünmek", "B1", "Academic", "Please consider all the options."),
        ("contact", "/ˈkɒntækt/", "noun", "communication with a person or group", "iletişim", "A2", "Work", "I will contact the hotel today."),
        ("convenient", "/kənˈviːniənt/", "adjective", "easy or suitable for a situation", "uygun", "B1", "Travel", "The station is convenient for visitors."),
        ("decision", "/dɪˈsɪʒən/", "noun", "a choice made after thinking", "karar", "B1", "Daily Life", "It was a difficult decision."),
        ("deliver", "/dɪˈlɪvə/", "verb", "to take something to a person or place", "teslim etmek", "B1", "Work", "We deliver orders within two days."),
        ("describe", "/dɪˈskraɪb/", "verb", "to say what someone or something is like", "tanımlamak", "A2", "Daily Life", "Can you describe the problem?"),
        ("develop", "/dɪˈveləp/", "verb", "to grow or improve something", "geliştirmek", "B1", "Work", "We develop useful learning tools."),
        ("discover", "/dɪˈskʌvə/", "verb", "to find something for the first time", "keşfetmek", "A2", "Travel", "Discover a new place every weekend."),
        ("encourage", "/ɪnˈkʌrɪdʒ/", "verb", "to give someone confidence or support", "teşvik etmek", "B1", "Personality", "Good feedback encourages learners."),
        ("environment", "/ɪnˈvaɪərənmənt/", "noun", "the natural or social conditions around us", "çevre", "B1", "Academic", "We should protect the environment."),
        ("essential", "/ɪˈsenʃəl/", "adjective", "completely necessary", "gerekli", "B1", "Academic", "Sleep is essential for learning."),
        ("experience", "/ɪkˈspɪəriəns/", "noun", "knowledge gained by doing something", "deneyim", "A2", "Work", "This job gave me valuable experience."),
        ("explore", "/ɪkˈsplɔː/", "verb", "to travel around or examine something", "keşfetmek", "A2", "Travel", "We explored the old city."),
        ("flexible", "/ˈfleksəbəl/", "adjective", "able to change easily", "esnek", "B1", "Work", "Our schedule is flexible."),
        ("focus", "/ˈfəʊkəs/", "verb", "to give attention to something", "odaklanmak", "A2", "Academic", "Focus on the main idea."),
        ("improve", "/ɪmˈpruːv/", "verb", "to make something better", "geliştirmek", "A2", "Daily Life", "Practice helps you improve."),
        ("include", "/ɪnˈkluːd/", "verb", "to contain or involve something", "içermek", "A2", "Daily Life", "The price includes breakfast."),
        ("independent", "/ˌɪndɪˈpendənt/", "adjective", "able to do things without help", "bağımsız", "B1", "Personality", "She is an independent learner."),
        ("influence", "/ˈɪnfluəns/", "noun", "the power to affect someone or something", "etki", "B2", "Academic", "Media can influence our choices."),
        ("maintain", "/meɪnˈteɪn/", "verb", "to keep something in good condition", "sürdürmek", "B2", "Work", "Maintain a regular study routine."),
        ("opportunity", "/ˌɒpəˈtjuːnəti/", "noun", "a chance to do something", "fırsat", "B1", "Work", "This course is a great opportunity."),
        ("organize", "/ˈɔːɡənaɪz/", "verb", "to plan or arrange an activity", "organize etmek", "A2", "Work", "I organize my tasks every morning."),
        ("prefer", "/prɪˈfɜː/", "verb", "to like one thing more than another", "tercih etmek", "A2", "Daily Life", "I prefer tea to coffee."),
        ("progress", "/ˈprəʊɡres/", "noun", "improvement or movement forward", "ilerleme", "B1", "Academic", "You are making good progress."),
        ("recommend", "/ˌrekəˈmend/", "verb", "to suggest something as good", "önermek", "B1", "Travel", "Can you recommend a restaurant?"),
        ("reduce", "/rɪˈdjuːs/", "verb", "to make something smaller or less", "azaltmak", "B1", "Academic", "Reduce distractions while studying."),
        ("reliable", "/rɪˈlaɪəbəl/", "adjective", "able to be trusted", "güvenilir", "B1", "Work", "We need a reliable connection."),
        ("schedule", "/ˈʃedjuːl/", "noun", "a plan that shows when things happen", "program", "A2", "Work", "Check the meeting schedule."),
        ("solution", "/səˈluːʃən/", "noun", "an answer to a problem", "çözüm", "B1", "Work", "We found a simple solution."),
        ("strategy", "/ˈstrætədʒi/", "noun", "a plan for achieving a goal", "strateji", "B2", "Exam", "Create a strategy for the exam."),
        ("suggest", "/səˈdʒest/", "verb", "to offer an idea for consideration", "önermek", "A2", "Daily Life", "I suggest taking a short break."),
        ("support", "/səˈpɔːt/", "verb", "to help someone or something", "desteklemek", "A2", "Personality", "Friends support each other."),
        ("understand", "/ˌʌndəˈstænd/", "verb", "to know the meaning of something", "anlamak", "A1", "Daily Life", "I understand the question."),
        ("valuable", "/ˈvæljuəbəl/", "adjective", "useful or important", "değerli", "B1", "Academic", "Your feedback is valuable.")
    ];

    public static async Task SeedAsync(EnglishLearningDbContext db, CancellationToken ct = default)
    {
        var existing = await db.VocabularyWords.AsNoTracking().Select(x => x.Term).ToListAsync(ct);
        var missing = Catalog.Where(x => !existing.Contains(x.Term, StringComparer.OrdinalIgnoreCase))
            .Select(x => new VocabularyWord
            {
                Term = x.Term, Pronunciation = x.Pronunciation, PartOfSpeech = x.PartOfSpeech,
                Definition = x.Definition, Translation = x.Translation, Level = x.Level,
                Category = x.Category, ExampleSentence = x.Example, PublicationStatus = VocabularyPublicationStatus.Published,
                PublishedAtUtc = DateTime.UtcNow
            }).ToList();
        if (missing.Count == 0) return;
        db.VocabularyWords.AddRange(missing);
        await db.SaveChangesAsync(ct);
    }
}
