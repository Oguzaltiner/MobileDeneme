# MobileDeneme Task Backlog

Bu dosya, ürün kararlarını uygulanabilir dikey dilimlere çeviren ortak sıradır. Her task uygulanmadan önce kapsamı, kabul kriterleri ve mağaza/uyumluluk riskleri netleştirilir.

## Tamamlandı — Quiz/Test Engine: 4 seçenekli sınav akışı

**Durum:** Done — `4e91d42`  
**Öncelik:** P1 — vocabulary catalog sonrası, review event kalıcılığından önce MVP öğrenme deneyimi  
**Amaç:** Kullanıcıya yalnızca kelime göstermeyen; soruyu, dört seçeneği, cevabı ve sonucu olan ölçülebilir bir test deneyimi sunmak.

### Ürün şekli

- Quiz başlangıcında seviye/kategori ve soru sayısı seçilir: ilk MVP için 5 veya 10 soru.
- Soru tipleri ilk sürümde iki türle sınırlı tutulur: İngilizce kelime → Türkçe anlam ve İngilizce tanım → kelime.
- Her soruda tam 4 seçenek bulunur; tek doğru cevap vardır.
- Çeldiriciler aynı seviye ve mümkünse aynı kategori/kelime türünden seçilir; tekrar eden, doğru cevaba çok benzeyen veya boş seçenek üretilmez.
- Kullanıcı bir cevabı işaretledikten sonra doğru/yanlış sonucu ve kısa açıklama görür; soru başına tek cevap hakkı vardır.
- Quiz sonunda skor, doğru/yanlış sayısı, başarı yüzdesi ve tekrar önerisi gösterilir.
- İlk MVP cevapları öğrenme istatistiğine kaydedilebilir; FSRS/sınav ağırlığı ve ayrıntılı review scheduling ayrı task olarak kalır.

### Backend tasarımı

- Auth korumalı quiz session modeli: kullanıcı, filtre, soru sırası, durum, başlangıç/bitiş zamanı ve skor.
- Soru cevabı mobil istemciye `correctAnswer` veya cevap anahtarı sızdırmadan gönderilir; doğrulama sunucuda yapılır.
- Önerilen endpoint sınırı:
  - `POST /api/v1/quizzes/sessions`
  - `GET /api/v1/quizzes/sessions/{id}`
  - `POST /api/v1/quizzes/sessions/{id}/answers`
  - `POST /api/v1/quizzes/sessions/{id}/complete`
- Session için süre/tekrar oynama/idempotency kuralları tanımlanır. Başka kullanıcının session id'si erişilemez.
- Soru üretimi deterministik ve test edilebilir bir servis olur; içerik yoksa quiz başlatma anlamlı hata döndürür.

### Mobil tasarımı

- `QuizStartScreen`: seviye, kategori ve soru sayısı seçimi.
- `QuizQuestionScreen`: ilerleme göstergesi, soru, 4 şık, seçim sonrası açıklama ve sonraki soru.
- `QuizResultScreen`: skor, başarı yüzdesi, yanlışlar ve tekrar başlat/ana sayfaya dön aksiyonları.
- Loading, network error, boş katalog, session expired ve tekrar gönderim durumları açıkça gösterilir.

### Kabul kriterleri

- Giriş yapmış kullanıcı 5/10 soruluk quiz başlatabilir.
- Her soruda tam 4 benzersiz seçenek ve tek doğru cevap vardır.
- Doğru cevap istemciye önceden açık edilmez; cevap sunucuda doğrulanır.
- Kullanıcı aynı soruya ikinci kez cevap gönderemez.
- Quiz yarıda kapanırsa session durumu güvenli biçimde devam ettirilebilir veya sonlandırılabilir.
- Sonuç ekranı doğru sayısı, toplam soru ve yüzdeyi sunucudan gelen veriye göre gösterir.
- Aynı kullanıcı dışındaki istekler 401/403 alır; süresi dolan session tekrar kullanılamaz.
- Quiz akışı review/SRS altyapısına ileride bağlanabilecek event sınırını korur.

## Sıradaki task — Monetization: Free + Premium paketleri

**Durum:** In progress — entitlement, limitler ve Premium satın alma ekranı hazır; Play doğrulaması bekliyor
**Öncelik:** P1 — ilk öğrenme döngüsü ve review API'sinden sonra  
**Amaç:** İngilizce kelime öğrenme uygulamasını ücretsiz kullanıcı edinimi ile Premium abonelik/ürün gelirini birlikte destekleyecek şekilde paketlemek.

### Analiz kapsamı

- Free paket sınırları: A1–A2 içerik, günlük kelime/quiz limiti, reklam gösterim noktaları ve tekrar limiti.
- Premium paket: A1–C2 erişimi, sınırsız öğrenme, listening/pronunciation, AI konuşma pratiği, kişisel listeler, spaced repetition, istatistikler ve reklamsız deneyim.
- Aylık/yıllık fiyat denemeleri; 99 TL aylık ve 699 TL yıllık değerleri yalnızca başlangıç varsayımı olarak ele alınacak.
- 1.000 / 10.000 / 50.000 / 100.000 / 500.000 aktif kullanıcı için dönüşüm, reklam ve Premium gelir senaryoları.
- Google Play ve App Store mağaza kesintileri, iade/iptal etkisi ve net gelir hesaplama formülü.
- Türkiye'deki vergi, faturalama, tüketici mevzuatı ve muhasebe yükümlülükleri için uzman doğrulaması gerektiren noktalar.
- Kullanıcı başına gelir, reklam doluluk/eCPM ve Premium dönüşüm varsayımlarının ayrı ayrı değiştirilebildiği bir model.

### Uygulama çıktıları

- Paket/entitlement sözleşmesi ve backend'de server-authoritative premium erişim kontrolü.
- Mobil paywall, restore purchase ve abonelik durum ekranı için API sınırı.
- Reklam consent/kişiselleştirme tercihlerinin veri modeli.
- Ürün analitiği olayları: paywall görüntüleme, deneme başlatma, satın alma, yenileme, iptal, geri yükleme.
- Mağaza ürün kimlikleri, sandbox test planı ve fiyatlandırma karar kaydı.

### Kabul kriterleri

- Free ve Premium özellikleri tek bir özellik matrisiyle açıkça tanımlı.
- Brüt gelir, mağaza kesintisi ve tahmini net gelir ayrı gösteriliyor.
- Varsayımlar değiştirildiğinde beş kullanıcı senaryosu yeniden hesaplanabiliyor.
- Uygulama içi erişim backend tarafından doğrulanıyor; mobil istemci tek başına Premium kararı vermiyor.
- Reklam ve abonelik deneyimi gizlilik/consent gereksinimleriyle birlikte tasarlanmış.
- Fiyatlar ve mağaza komisyonları yayına almadan önce güncel resmi kaynaklarla doğrulanmış.

## Kalite altyapısı — Backend test projesi

**Durum:** Done — `backend/tests/EnglishLearning.Tests` (xUnit v3): içerik kalite kuralları, import, yayın limiti ve roller, quiz bütünlüğü ve IDOR, entitlement limitleri, Günlük Görev. Entegrasyon testleri yerel PostgreSQL üzerinde geçici veritabanı açar (`TEST_POSTGRES`, bkz. `backend/tests/README.md`).

## Diğer sıralı işler

## Yeni ürün dilimi — Gramer laboratuvarı ve oyun merkezi

**Durum:** In progress — ilk API sözleşmesi ve mobil giriş ekranları hazır

- Türkçe–İngilizce karşılaştırmalı gramer dersleri için `GET /api/v1/grammar/lessons` ve detay endpointi eklendi.
- İlk içerik seti: Present Simple/Continuous, artikeller ve soru cümlesi sırası.
- Mobilde Gramer Lab; kural kartları, Türkçe notlar ve cevap sonrası açıklamalı mini pratik içeriyor.
- Oyun Merkezi; quiz, cümle tamamlama, eşleştirme, yazma ve dinleme modlarını tek girişte topluyor.
- Sonraki iş: gramer içeriklerini admin CMS'e taşımak, cevapları PracticeEvent/XP ile bağlamak ve gerçek Sentence Builder oyun motorunu eklemek.

1. Review event API + server-authoritative spaced repetition. **In progress — ilk dikey dilim tamamlandı**
2. Quiz/Test Engine: 4 seçenekli sınav akışı (bu task).
3. Monetization: Free + Premium paketleri.
4. Offline review queue ve haftalık istatistikler. **Done — haftalık istatistik API’si ve AsyncStorage tabanlı idempotent review kuyruğu tamamlandı**
5. Audio/listening ve erişilebilirlik geçişi.
6. Admin web uygulaması, içerik yayınlama ve audit.

## Ürünleşme notu — Personal Coach başlangıcı

**Durum:** Done — Günlük Görev (Daily Mission) sunucu kontrollü akış olarak eklendi: tekrar → yeni kelime → hatırlama → dinleme → sonuç; XP tek sefer, görev serisi, sabitlenmiş saat dilimi, dünkü görevi bitirme ve ana sayfa kartı. Sözleşme: `docs/DAILY_MISSION.md`.

İlk öğrenme döngüsünü satılabilir bir “kişisel koç” deneyimine taşımak için dashboard artık gerçek review verilerinden günlük ilerleme, öğrenilen kelime, bekleyen tekrar ve önerilen seans boyutu üretiyor. Mobil ana sayfa bu veriyi kişisel koç kartında gösteriyor. Sonraki adım: bu öneriyi dinleme + recall + review adımlarından oluşan tek bir Daily Mission akışına bağlamak.

## Kalite görevleri — Listening, pronunciation ve catalog

### Listening Lab ses doğruluğu

**Durum:** Done — transcript ve doğru seçenek ayrı ayrı seslendiriliyor; TTS hatası kullanıcıya gösteriliyor.

- Dinleme ekranındaki metin API’den gelen `transcript` ile aynı olmalı.
- Cevap açıldığında doğru seçenek tekrar dinlenebilmeli.
- TTS hatası sessizce yutulmamalı; tekrar deneme mesajı gösterilmeli.

### Telaffuz değerlendirme akışı

**Durum:** Done — hedef cümle güvenli seçiliyor, hata/tekrar akışı ve odak kelimeleri gösteriliyor.

- Seçili senaryonun son hedef cümlesi kullanılmalı.
- Boş veya hatalı değerlendirme anlaşılır geri bildirim vermeli.
- Metin tabanlı assessment Free kullanıcıda da çalışmalı; gerçek STT ayrı premium sağlayıcı entegrasyonudur.

### Vocabulary catalog genişletme

**Durum:** Done — 2.500 yayınlanmış kelime (A1 574, A2 684, B1 774, B2 468); 146 kelime `content/vocabulary/publish-reserve.json` ile İncelemede yedek.

- İçerik `content/vocabulary/{a1,a2,b1,b2}.json` paketlerinde; `node scripts/validate-vocabulary.mjs` ile doğrulanır, `node scripts/import-vocabulary.mjs --publish` veya admin "Toplu içe aktar" ekranıyla yüklenir (`POST /api/v1/admin/content/vocabulary/import`, upsert, her şey-ya-hiç doğrulama).
- İçerik yapay zekâ ile yazıldı ve ikinci bir yapay zekâ incelemesinden geçti; insan editör örneklem kontrolü önerilir.
- Eski indeks eşleşmeli listeden kalan 346 taslak (fonksiyon kelimeleri vb.) yayına alınmayacak; admin katalogdan temizlenebilir.
- Sonraki adım: C1 paketi ve kapasitenin yeniden değerlendirilmesi; insan editör onayı için admin'de örneklem inceleme akışı.
