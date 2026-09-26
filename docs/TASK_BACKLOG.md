# MobileDeneme Task Backlog

Bu dosya, ürün kararlarını uygulanabilir dikey dilimlere çeviren ortak sıradır. Her task uygulanmadan önce kapsamı, kabul kriterleri ve mağaza/uyumluluk riskleri netleştirilir.

## Sıradaki task — Quiz/Test Engine: 4 seçenekli sınav akışı

**Durum:** Queued  
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

## Sıralı task — Monetization: Free + Premium paketleri

**Durum:** Queued  
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

## Diğer sıralı işler

1. Review event API + server-authoritative spaced repetition.
2. Quiz/Test Engine: 4 seçenekli sınav akışı (bu task).
3. Monetization: Free + Premium paketleri.
4. Offline review queue ve haftalık istatistikler.
5. Audio/listening ve erişilebilirlik geçişi.
6. Admin web uygulaması, içerik yayınlama ve audit.
