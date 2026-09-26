# MobileDeneme Task Backlog

Bu dosya, ürün kararlarını uygulanabilir dikey dilimlere çeviren ortak sıradır. Her task uygulanmadan önce kapsamı, kabul kriterleri ve mağaza/uyumluluk riskleri netleştirilir.

## Sıradaki task — Monetization: Free + Premium paketleri

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
2. Monetization: Free + Premium paketleri (bu task).
3. Offline review queue ve haftalık istatistikler.
4. Audio/listening ve erişilebilirlik geçişi.
5. Admin web uygulaması, içerik yayınlama ve audit.
