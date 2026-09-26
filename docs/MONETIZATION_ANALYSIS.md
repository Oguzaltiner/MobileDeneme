# Free + Premium Monetization — MVP analizi

Bu belge satın alma sağlayıcısı bağlanmadan önce ürün sınırlarını ve server-authoritative entitlement sözleşmesini sabitler. Fiyatlar ve mağaza komisyonları yayın öncesinde Google Play ve App Store’un güncel resmi koşullarıyla yeniden doğrulanmalıdır.

## Paket matrisi

| Alan | Free | Premium |
|---|---|---|
| İçerik seviyesi | A1–A2 | A1–C2 |
| Günlük kelime | 20 | Sınırsız |
| Günlük quiz | 1 | Sınırsız |
| Reklam | Var | Yok |
| Listening/pronunciation | Temel/sonraki faz | Premium |
| Kişisel liste ve gelişmiş istatistik | Sınırlı | Premium |
| Spaced repetition | MVP sınırları | Tam akış |

## İlk gelir varsayımı

- Aylık Premium: 99 TL
- Yıllık Premium: 699 TL
- Başlangıç dönüşüm varsayımı: aktif kullanıcıların %3’ü
- Net gelir hesabı: `aktif kullanıcı × dönüşüm × fiyat × (1 - mağaza komisyonu)`
- Reklam geliri bu ilk backend diliminde hesaplanmıyor; consent, eCPM ve doluluk değerleri netleşince ayrı model olarak eklenecek.

## Backend sınırı

`GET /api/v1/me/entitlement` endpoint’i mobilin kullanacağı tekil yetki kaynağıdır. Premium kararı istemcide sabitlenmez. Şimdilik kayıtlı entitlement yoksa kullanıcı Free kabul edilir; mağaza doğrulama task’ında Google Play/App Store server notification doğrulaması bu tabloyu güncelleyecek. Günlük quiz sayacı UTC tarihine göre backend’de tutulur ve Free kullanıcı ikinci quiz başlatırsa `429 Too Many Requests` döner.

## Sonraki monetization task’ları

1. Mağaza ürün kimlikleri ve sandbox satın alma/restore akışı.
2. Google/Apple server notification doğrulaması ve idempotent webhook işleme.
3. Günlük kelime/quiz kullanım sayaçları ve Free limitlerinin tüm endpointlerde uygulanması.
4. Mobil paywall, consent ve abonelik durum ekranı.
5. Paywall görüntüleme, satın alma, yenileme ve iptal analitik olayları.

## Google Play satın alma notu

Mobildeki Premium ekranı Google Play Billing ürün kimlikleri `premium_monthly` ve `premium_yearly` ile hazırlandı. Gerçek satın alma için bu ürünlerin Play Console’da oluşturulması, uygulamanın `com.oguzaltiner.englishlearning` paket adıyla imzalı bir development/release build olarak kurulması ve backend’e Google Play Developer API servis hesabı eklenmesi gerekir. Expo Go native IAP modülünü çalıştırmaz; satın alma testi gerçek Android cihazda development build ile yapılmalıdır. Backend doğrulaması yapılandırılmadan entitlement verilmez.
