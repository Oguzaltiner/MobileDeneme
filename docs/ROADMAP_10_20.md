# MobileDeneme — Üst Seviye Ürün Yol Haritası (Task 10–20)

Bu belge mevcut MVP'nin üzerine kurulacak üretim kalitesindeki öğrenme sistemini tanımlar. Amaç yalnızca yeni ekranlar eklemek değil; her özelliği öğrenme sonucuna, ölçülebilir davranışa ve güvenli veri akışına bağlamaktır.

## Ürün boşlukları

- Review algoritması mevcut olsa da gerçek mastery, unutma tahmini ve beceri bazlı planlama sınırlı.
- Practice planındaki konuşma senaryoları statik; speech-to-text ve telaffuz skoru yok.
- Leaderboard kapanışı ve ödül dağıtımı kalıcı bir sezon modeli değil.
- Admin paneli kelime yayınlama odaklı; ders, soru, ses, video ve rota içerik stüdyosu eksik.
- Offline kuyruk var; tam lesson paket senkronizasyonu ve çoklu cihaz conflict çözümü eksik.
- Ürün analitiği, deney/feature flag, retention ve funnel ölçümleri eksik.
- Erişilebilirlik, crash telemetry ve store doğrulaması yayın öncesi tamamlanmalı.

## Güncel ilerleme

- **10 tamamlandı (ilk sürüm):** mastery, tekrar sayısı, doğru oranı, lapse ve server-authoritative due planı.
- **11 tamamlandı (ilk sürüm):** son quiz performansına göre zorluk bandı 1–4 arasında ayarlanıyor.
- **12 tamamlandı (ilk sürüm):** placement test, CEFR tahmini ve mobil seviye testi akışı.
- **13 ilk dilim tamamlandı:** soru becerisi, açıklama ve hata etiketi metadata'sı.
- **14 ilk dilim tamamlandı:** Coach önerileri artık aksiyon anahtarı, rota, beceri ve CTA döndürüyor.
- **18 ilk dilim tamamlandı:** leaderboard sezon anahtarı, yükselme/düşme eşikleri ve ödül katmanı döndürülüyor; kalıcı sezon kapanışı sonraki alt iştir.
- **15–17 ilk production slice tamamlandı:** API kontrollü konuşma senaryoları, dinleme laboratuvarı, cihaz TTS ve transcript tabanlı telaffuz değerlendirmesi eklendi.
- **20–22 ilk production slice tamamlandı:** admin öğrenme analitiği, review metadata kalite skoru ve idempotent offline PracticeEvent batch sync endpoint'i eklendi.
- **23 ilk slice tamamlandı:** bildirim açık/kapalı, hatırlatma saati ve sessiz saatler kullanıcı ayarlarında saklanıyor.

## Task sırası

| No | Task | Öncelik | Bağımlılık | Bitti kabulü |
|---|---|---:|---|---|
| 10 | Mastery tabanlı spaced repetition | P0 | PracticeEvent | `masteryScore`, `nextReviewAt`, streak ve due planı server-authoritative olur |
| 11 | Adaptive difficulty | P0 | 10 | Başarı, tepki süresi ve hata etiketine göre soru zorluğu değişir |
| 12 | CEFR curriculum ve placement test | P0 | İçerik modeli | A1–C2 rota, hedef ve checkpoint sınavı oluşur |
| 13 | Soru bankası 2.0 | P0 | 12 | En az 7 soru tipi, açıklama, ses ve hata etiketi desteklenir |
| 14 | Coach 2.0 aksiyon kartları | P0 | 10–13 | Her öneri tek dokunuşla ölçülebilir session başlatır |
| 15 | AI konuşma partneri | P1 | ConversationPractice | Seviyeye göre dinamik diyalog ve oturum raporu verir |
| 16 | Speech-to-text ve telaffuz skoru | P1 | 15 | Ses kaydı, kelime/cümle skoru ve tekrar önerisi oluşur |
| 17 | Dinleme laboratuvarı | P1 | 13, 16 | Native audio/video, shadowing, dictation ve hız kontrolü vardır |
| 18 | Lig sezonu, ödül ve arkadaş görevleri | P1 | Leaderboard | Sezon kapanışı, ödül geçmişi ve güvenli sosyal görevler çalışır |
| 19 | Topluluk feedback ve moderasyon | P1 | 18, Admin | Yazma/konuşma cevapları raporlanabilir ve denetlenebilir |
| 20 | İçerik stüdyosu 2.0 | P1 | 12–19 | Ders, soru, medya, rota ve senaryo taslak/review/publish akışında yönetilir |
| 21 | Ürün analitiği ve deney platformu | P1 | Tüm akışlar | Funnel, D1/D7 retention, completion ve feature flag ölçülür |
| 22 | Offline-first tam senkronizasyon | P1 | 10, PracticeSession | Ders paketleri ve event'ler offline çalışır, conflict idempotent çözülür |
| 23 | Bildirim ve alışkanlık motoru | P1 | 10, 18, 21 | Hedef, streak, due review ve lig bildirimleri kişiselleşir |
| 24 | Premium paket ve entitlement v2 | P1 | 14–17, 21 | Free/Premium/Premium Plus limitleri API tarafından uygulanır |
| 25 | Erişilebilirlik ve tasarım sistemi | P0 | Mobil ekranlar | Dark mode, font scaling, screen reader ve safe-area testleri geçer |
| 26 | Güvenlik, gözlemlenebilirlik ve E2E kalite kapısı | P0 | Tüm API | Rate limit, audit, crash telemetry, E2E ve güvenlik taraması vardır |
| 27 | Google Play/App Store billing | P2 / son | 24, 26 | Sandbox satın alma, restore, iptal, yenileme ve webhook doğrulanır |

## Uygulama ilkeleri

1. Skor, entitlement, süre ve doğru cevap gibi kritik kararlar mobilde değil backend'de verilir.
2. Her öğrenme aksiyonu `PracticeEvent` veya eşdeğer idempotent event olarak kaydedilir.
3. Yeni içerik taslak olarak başlar; reviewer onayı olmadan mobil katalogda görünmez.
4. AI ve ses verisi varsayılan olarak saklanmaz; açık rıza ve silme akışı olmadan kalıcı depolama yapılmaz.
5. Her task için backend contract, mobile akış, admin görünümü, test ve telemetry birlikte teslim edilir.

## Rakiplerden alınan ürün kalıpları

- Babbel: 5–10 dakikalık hedef odaklı ders, konuşma tanıma, kişisel review.
- Memrise: native-speaker video, gerçek hayat ifadeleri, AI konuşma partneri.
- Busuu: CEFR rota, checkpoint, topluluk düzeltmesi, offline ders ve AI conversation feedback.
- Duolingo: streak, quest, lig ve arkadaş döngüsü; ancak puan sisteminin öğrenme kalitesini gölgelememesi gerekir.

Bu kalıplar görsel veya içerik olarak kopyalanmaz; MobileDeneme'nin koç + bağlam + tekrar çekirdeğine uyarlanır.
