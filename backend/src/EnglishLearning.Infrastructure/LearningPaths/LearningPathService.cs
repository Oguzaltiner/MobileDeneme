using EnglishLearning.Application.LearningPaths;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.LearningPaths;

public sealed class LearningPathService(EnglishLearningDbContext db) : ILearningPathService
{
    private static readonly (string Key, string Title, string Description, string Purpose)[] Paths =
    [
        ("full-course", "Tam İngilizce Kursu", "Her seviyede sağlam bir temel oluştur.", "general"),
        ("pronunciation", "İngilizce Telaffuz", "Yeni kelimeleri daha özgüvenli söyle.", "general"),
        ("travel", "Seyahat için İngilizce", "Seyahat sırasında kullanışlı ifadeleri öğren.", "travel"),
        ("business", "Temel İş Becerileri", "İş yerinde rahatlıkla iletişim kur.", "business"),
        ("academic", "Akademik Çalışmalar", "Anahtar ifadelerle akademik öğrenmeni kolaylaştır.", "academic"),
        ("exam", "İngilizce Sınavında Başarı", "Sınav başarısı için stratejiler ve kelimeler keşfet.", "exam"),
        ("culture", "Dünyaya İngilizce Bakış", "Gerçek içeriklerle kültür ve kelime bilgini geliştir.", "general")
    ];

    public async Task<IReadOnlyList<LearningPath>> GetAsync(Guid userId, CancellationToken ct)
    {
        var purpose = await db.UserSettings.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.LearningPurpose).SingleOrDefaultAsync(ct);
        return Paths.Select(x => new LearningPath(x.Key, x.Title, x.Description, x.Purpose, x.Purpose == purpose || (purpose == "general" && x.Key == "full-course"))).ToList();
    }
}
