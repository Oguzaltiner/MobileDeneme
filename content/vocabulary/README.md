# Kelime içerik paketleri

`a1.json`, `a2.json`, `b1.json`, `b2.json` uygulamanın İngilizce–Türkçe kelime kataloğudur (toplam 2.595 kelime). Her kelimede IPA telaffuz, kelime türü, sade İngilizce tanım, Türkçe karşılık (1–3 seçenek), CEFR seviyesi, kategori ve kelimenin tam hâlini içeren bir örnek cümle bulunur.

## Nasıl hazırlandı

- İçerik, yapay zekâ ile yazıldı ve ardından bağımsız ikinci bir yapay zekâ incelemesinden geçti (74 düzeltme uygulandı). Tanımlar ve örnek cümleler özgündür; yayımlanmış sözlüklerden kopyalanmamıştır.
- Türkçesi İngilizcesiyle aynı yazılan kelimeler (park, robot, program vb.) quiz cevabını ele verdiği için katalogda yoktur.
- İnsan editör kontrolü önerilir: admin panelindeki inceleme kuyruğu ve katalog ekranı bunun için kullanılabilir.

## Kurallar

`node scripts/validate-vocabulary.mjs content/vocabulary/*.json` paketi doğrular. Kurallar, API'nin import doğrulamasıyla (`VocabularyQuality.FindImportProblems`) uyumludur ve bunlara ek olarak kategori listesi, Türkçe karakter seti, tanımın terimi içermemesi ve örnek cümle uzunluğu gibi editoryal kontroller yapar.

## Yayınlama

Yayın kapasitesi 2.500 kelimedir (`VocabularyLimits.MaxPublishedWords`). `publish-reserve.json` içindeki 146 kelime içe aktarılır ama İncelemede kalır; admin, yayındaki bir kelimeyi çıkarıp yerine bunlardan birini yayınlayabilir.

```bash
node scripts/import-vocabulary.mjs --publish
```

Betik, paketleri `POST /api/v1/admin/content/vocabulary/import` ile yükler (yeni kelimeler İncelemede durumuna geçer, yayındakiler değişmez), ardından yedek listesi dışındakileri 100'erli gruplar hâlinde yayınlar. Yerel geliştirme dışında `API_BASE_URL`, `ADMIN_EMAIL` ve `ADMIN_PASSWORD` ortam değişkenleri gerekir. Aynı paket admin panelindeki "Toplu içe aktar" ekranından da yüklenebilir.
