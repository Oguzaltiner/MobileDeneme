# MobileDeneme — Ürünleşme ve yayın hazırlığı özeti

## Bu çalışma turunda tamamlananlar

- Personal Coach dashboard: gerçek review verisiyle bugünkü ilerleme, bekleyen tekrar, önerilen seans ve öğrenme mesajı.
- Daily Mission: dinle → anlamı hatırla → 4 seçenekten seç → zorlukla değerlendir akışı.
- Telaffuz: `expo-speech` ile native TTS ve durdurma/fallback davranışı.
- Streak, haftalık tekrar hedefi ve achievement rozetleri.
- Home, auth, onboarding, vocabulary, quiz ve premium ekranlarında ortak business görsel dili.
- Mobil alt taşma sorunları için ScrollView, liste flex ve alt güvenli boşlukları.
- Backend güvenlik tabanı: auth rate limit (IP başına 30/dk) ve temel güvenlik header’ları.
- Backend Dockerfile, Docker Compose API + PostgreSQL servisi ve non-root runtime.
- GitHub Actions CI: .NET restore/build/vulnerability audit, mobile npm ci/typecheck/Expo web export.
- Android/iOS yayın temeli: bundle/package kimlikleri, versionCode ve EAS development/preview/production profilleri.

## Doğrulamalar

- `dotnet build backend/src/EnglishLearning.Api/EnglishLearning.Api.csproj --no-restore -p:BuildInParallel=false -m:1` başarılı.
- `npm run typecheck` başarılı.
- `npx expo export --platform web` başarılı.
- Dashboard smoke: haftalık hedef ve 4 rozet JSON sözleşmesi doğrulandı.
- `docker compose convert` başarılı.
- Docker daemon çalışmadığı için `docker compose build api` çalıştırılamadı; Docker Desktop açıldığında tekrar edilmelidir.

## Bilinen riskler / yayın öncesi işler

- `System.Security.Cryptography.Xml 9.0.0` için 8 adet NU1903 güvenlik uyarısı var; bağımlılık güncellemesi planlanmalı.
- Google Play/App Store satın alma doğrulaması henüz yapılandırılmadı; mevcut billing endpoint’i bilinçli olarak 501 döner.
- Production ortamında `Jwt__SigningKey` güçlü secret olarak secret manager’dan verilmelidir; Compose varsayılanı yalnızca local development içindir.
- Play Console ürünleri, Apple StoreKit ürünleri, privacy/consent, restore purchase, RTDN ve release signing yayın öncesi tamamlanmalıdır.
- Gerçek cihaz test matrisi: Android development build, iOS development build, düşük bağlantı, büyük font, screen reader ve mağaza sandbox.

## Sonraki önerilen sıra

1. Docker Desktop ile Compose API + PostgreSQL smoke testi.
2. NU1903 bağımlılık güncellemesi ve `dotnet list package --vulnerable` çıktısının temizlenmesi.
3. Android/iOS EAS development build ve gerçek cihaz accessibility testi.
4. Google Play/App Store server-side billing ve restore akışı.
5. Admin web paneli ve içerik yayınlama/audit.
