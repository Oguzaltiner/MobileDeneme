# Ürün Yol Haritası

Bu sıra; mevcut altyapıyı bozmadan, örnek uygulamalardaki yüksek bağlılık döngülerini ürüne uyarlamak için hazırlanmıştır. Görsellerden ilham alınmıştır; marka, içerik ve tasarım birebir kopyalanmayacaktır.

## Ürün yönü

Ana değer önerimiz: **hedefine göre kısa ders + bağlam içinde pratik + kişisel tekrar + konuşmaya geçiş**.

Babbel’in hedefe göre rota, kısa pratik ders, konuşma ve kişiselleştirilmiş tekrar yaklaşımı; Memrise’in telaffuz/gerçek içerik yaklaşımı; Duolingo’nun streak, lig, görev ve oyunlaştırma döngüsü referans alınmıştır. Babbel kendi açıklamasında hedefe göre rota, 5–10 dakikalık dersler, konuşma pratiği ve spaced repetition akışını vurguluyor ([How Babbel Works](https://www.babbel.com/how-babbel-works)).

## Öncelik sırası

### 1. Öğrenme motoru ve içerik kalitesi — P0

- Learning path seçimini tüm dersleri filtreleyecek şekilde bağla.
- Cümle tamamlama, dinleme, yazma, eşleştirme ve telaffuz modlarını ortak `PracticeSession` sözleşmesine taşı. İlk ortak plan giriş noktası tamamlandı; kalıcı seans/cevap olayları sonraki alt iş.
- Her soruya zorluk, beceri ve hata etiketi ekle.
- Cümle/örnek içeriklerini seed yerine admin içerik tablosundan yönet.

**Kabul:** Kullanıcı bir rota seçtiğinde en az üç farklı pratik tipiyle 5–10 dakikalık tamamlanabilir seans oluşur; cevaplar tekrar planını günceller.

**Durum:** Kalıcı PracticeSession/Step/Event sözleşmesi, seans başlatma-tamamlama API'leri ve mobil seans giriş akışı hazır. Plan artık konuşma senaryosunu da içeriyor. Rota anahtarları server-authoritative doğrulanıyor ve adım tamamlama idempotent event kimliğiyle offline tekrarlarında çift kayıt üretmiyor.

### 2. Kişiselleştirilmiş koç — P0

- Hatalı beceriyi tespit et: ör. telaffuz, cümle kurma, kelime anlamı.
- Ana sayfada “bugünün önerisi”ni tek bir metin yerine aksiyon kartına dönüştür.
- Günlük hedefi kullanıcının geçmişine göre otomatik ayarla; kullanıcı isterse sabitleyebilsin.

**Durum:** Dashboard son 7 günlük tekrar başarısını okuyup koç sebebi üretiyor; mobil sözleşmeye taşındı. Aksiyon kartı görselinin genişletilmesi sonraki UI polish işidir.

### 3. Oyunlaştırma ve sosyal döngü — P1

- ~~Mevcut Mavi Lig’e haftalık XP kazanım kaynaklarını göster.~~ Tamamlandı: tekrar ve quiz XP kırılımı mobilde gösteriliyor.
- Lig yükselme/düşme, haftalık ödül ve kişisel rekorlar ekle.
- Streak koruma, günlük görev serisi ve arkadaş daveti ekle.
- Bildirim tercihleri ve sessiz saatler ekle.

**Durum:** Haftalık özet kişisel rekor ve kapanışa kalan süre alanlarını döndürüyor; gerçek haftalık kapanış/ödül dağıtımı sonraki backend cron işidir.

### 4. Konuşma ve gerçek hayat pratiği — P1

- Önce cihaz TTS + kayıt + konuşma değerlendirme arayüzü.
- Sonra sağlayıcı seçimi yapılarak speech-to-text ve telaffuz skoru.
- Senaryo tabanlı diyaloglar: havaalanı, iş görüşmesi, restoran, toplantı.
- AI konuşma özelliği ancak gizlilik, maliyet, veri saklama ve kötüye kullanım politikası netleşince.

**Durum:** Havaalanı, restoran ve toplantı senaryoları için mobil pratik ekranı eklendi; cihaz TTS ile phrase kartları çalışıyor. STT/telaffuz puanı sağlayıcısı sonraki fazda.

### 5. İçerik ve admin paneli — P1

- Rol/izin modeli: admin, editor, reviewer.
- Kelime, cümle, ses, kategori ve rota CRUD.
- ~~Taslak → inceleme → yayın akışı.~~ Reviewer rolü, inceleme kuyruğu ve Draft → InReview → Published/Rejected geçişleri tamamlandı.
- İçerik değişiklikleri için audit log.
- Yayınlanmış kelime havuzu MVP’de 2.500 kelime ile kalite sınırlandırması; ileride paket bazlı genişleme.
- ~~Admin dashboard: aktif kullanıcı, tamamlanan ders, hata oranı, premium dönüşüm.~~ İlk admin dashboard sürümü aktif kullanıcı, kelime, quiz ve premium özetlerini içeriyor; hata oranı analitiği sonraki alt iş.

**Durum:** Admin web paneli ve reviewer akışı yanında temel quiz/review başarı analitiği eklendi; yayınlanmış havuz 2.500 kelime ile sınırlandırılıyor.

### 6. Offline ve cihaz senkronizasyonu — P1

- Practice session paketlerini önceden indir.
- Kuyrukta schema version, retry count ve son hata bilgisini tut.
- Aynı hesabın birden fazla cihazındaki ilerlemeyi idempotent senkronize et.
- Çakışma kuralı: sunucu zamanı + event idempotency.

**Durum:** Offline review kuyruğunda schema version, retry count ve son hata tutuluyor; flush tekilleştirme temeli hazır. PracticeSession paket senkronizasyonu sonraki alt iştir.

### 7. Yayın ve mağaza — P2 / en son

- Google Play Billing ve App Store StoreKit server-side doğrulama.
- Restore purchase, abonelik yenileme, iptal ve RTDN/webhook.
- Privacy policy, consent, data deletion, crash reporting.
- EAS production signing, TestFlight/Internal Testing, staged rollout.

**Kabul:** Sandbox satın alma, restore, iptal ve abonelik yenileme senaryoları Android ve iOS’ta test edilir.

## Bilinçli olarak sonraya bırakılanlar

- Lisanslı haber/TV/video içerikleri: telif ve içerik anlaşması gerekir.
- AI konuşma partneri: ses verisi, maliyet, sağlayıcı ve güvenlik kararı gerekir.
- Gerçek sosyal arkadaş sistemi: moderasyon ve gizlilik altyapısı gerekir.

## İlk uygulanacak üç teknik iş

1. `PracticeSession` ortak sözleşmesi ve cümle/dinleme/yazma modları.
2. Admin rol + içerik taslak/yayın modeli.
3. XP kaynakları, lig haftası kapanışı ve ödül özeti.

## Kalite düzeltme görevleri — aktif

- **Listening audio correctness:** transcript ve cevap ifadeleri aynı ekran bağlamında seslendirilmeli; cevap açıldığında doğru ifade tekrar dinlenebilmeli.
- **Pronunciation assessment reliability:** metin tabanlı değerlendirme tüm planlarda çalışmalı; boş/uygunsuz transcript için anlaşılır hata ve skor geri bildirimi vermeli.
- **Vocabulary catalog expansion:** development kataloğu artık 1.000+ İngilizce-Türkçe başlangıç çiftini seed ediyor; sonraki içerikler admin content studio üzerinden 2.000 kelime kalite sınırına kadar reviewer onayıyla büyütülmeli.
