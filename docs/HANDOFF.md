# Devir notu — kaldığımız yer

Bu proje farklı Claude hesaplarından ve cihazlardan devam ettirilebilsin diye, oturum hafızasında tutulan bilgiler burada. Yeni bir oturum önce bu dosyayı, `AGENTS.md`'yi ve `docs/TASK_BACKLOG.md`'yi okumalı.

## Geçmiş

- Proje 26 Eylül – 5 Ekim 2026 arasında Codex ile geliştirildi, sonra Claude Code'a taşındı. Ajan ve beceri tanımları her iki araç için de repoda (`.claude/` ve `.codex/`, eşleme `CLAUDE.md`'de).
- Çalışma branch'i **`dev`**; yeni işler `dev`'e gider. `main` kararlı branch'tir ve kullanıcı istediğinde `dev`'den ileri sarılarak güncellenir (7 Ekim 2026'da eşitlendi).
- Kullanıcı Türkçe yazar ve her adımda onay beklemeden task listesinin bitirilmesini ister. İnceleme ve güvenlik adımları atlanmaz.

## Son durum (7 Ekim 2026)

- **Kelime kataloğu:** 2.500 yayınlanmış kelime (A1–B2), içerik `content/vocabulary/*.json`. 146 kelime İncelemede yedek (`publish-reserve.json`). Eski hatalı listeden kalan 346 taslak silinebilir.
- **Günlük Görev:** sunucu kontrollü günlük seans, sözleşme `docs/DAILY_MISSION.md`.
- **Backend testleri:** `backend/tests/EnglishLearning.Tests`, 99 test (bkz. `backend/tests/README.md`).
- **Telefonda denenmemiş:** Günlük Görev ekranları (özellikle iOS'ta dinleme sesi).
- Sıradaki aday işler: erişilebilirlik + karanlık mod (roadmap 25, P0), lig sezonu kapanışı ve ödüller (18), C1 kelime paketi. Mağaza faturalandırması bilerek en sona bırakıldı.

## Yerel geliştirme

PostgreSQL yerel Windows servisi olarak `localhost:5432`'de çalışır, veritabanı `english_learning`.

`appsettings.Development.json` içindeki `english_learning` kullanıcısı bu makinede çalışmaz; API, `postgres` kullanıcısı ve bir JWT anahtarı ortam değişkeni olarak verilerek başlatılır. Telefondan (Expo Go) erişim için `0.0.0.0` üzerinde dinletilir:

```powershell
$env:ConnectionStrings__Postgres = 'Host=localhost;Port=5432;Database=english_learning;Username=postgres;Password=<yerel-postgres-şifren>'
$env:Jwt__SigningKey = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
dotnet run --project backend/src/EnglishLearning.Api --launch-profile http --urls http://0.0.0.0:5057
```

- Development ortamında migration'lar ve seed açılışta otomatik uygulanır. Geliştirme hesabı `test@example.com` (şifresi `scripts/smoke.ps1` içinde), Admin + Premium.
- JWT anahtarı her başlatmada yeni üretildiği için telefonda yeniden giriş gerekir.
- Mobil: `mobile/` içinde `npx expo start`; telefon ve bilgisayar aynı ağda olmalı.
- Admin paneli: `admin-web/` içinde `npm run dev` (`http://localhost:5173`).
- Testler: `TEST_POSTGRES` ortam değişkeniyle `dotnet test backend/EnglishLearning.slnx` (CREATE DATABASE yetkili bir rol gerekir).
- EF migration aracı repo kökündeki `dotnet-tools.json` ile gelir: `dotnet tool restore`, sonra `dotnet dotnet-ef ...`.
