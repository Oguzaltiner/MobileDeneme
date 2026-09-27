# Accessibility ve güvenlik kalite kapısı

## Mobil accessibility

- Tüm etkileşimli kontroller `accessibilityRole` ve anlamlı `accessibilityLabel` taşımalı.
- Metinler `allowFontScaling` kullanmalı; kritik ekranlar 1.4x büyük font ile taşma testi geçmeli.
- Renk tek başına anlam taşımamalı; doğru/yanlış durumlarında metin ve ikon da gösterilmeli.
- Geri butonu ve hata ekranı screen reader ile erişilebilir olmalı.
- iOS VoiceOver ve Android TalkBack kritik akışlarda test edilmeli.
- Safe area, landscape ve küçük ekran testleri release öncesi yapılmalı.

## API security

- Auth endpoint rate limit: 30/dk.
- JWT signing key production secret manager’dan gelmeli.
- Problem details response’ları stack trace sızdırmamalı.
- Correlation ID ile log takibi yapılmalı.
- Security headers ve body limit aktif olmalı.
- Tüm kullanıcı kaynakları JWT user id ile sahiplik kontrolü yapmalı.
- Quiz, placement, sync ve community event’leri idempotent olmalı.

## Release gate

```powershell
dotnet build backend/src/EnglishLearning.Api/EnglishLearning.Api.csproj -c Release --no-restore
Push-Location mobile; npm run typecheck; Pop-Location
./scripts/smoke.ps1
```
