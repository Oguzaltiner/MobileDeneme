# Günlük Görev (Daily Mission) — API sözleşmesi

Ana sayfadaki tek dokunuşla başlayan, ~5–8 dakikalık kişisel günlük seans. İçerik, cevap kontrolü, XP ve seri sunucuda belirlenir; mobil yalnızca gösterir. Bu belge backend ve mobil için bağlayıcı sözleşmedir.

## Akış

1. `review` — bugün tekrarı gelen kelimeler (spaced repetition, `GET /reviews/due` mantığı).
2. `new-words` — kullanıcının seviyesine ve entitlement `MaxLevel` sınırına göre yeni kelimeler.
3. `recall` — 4 şıklı hatırlama soruları (görevin kelimelerinden).
4. `listening` — bir dinleme sorusu (TTS ile seslendirilir).
5. Tamamlama — XP, seri ve yarının önizlemesi.

Ücretsiz kullanıcı: görev kalan günlük kelime kotasına göre boyutlanır; quiz kotasını tüketmez. Kota görev ortasında biterse ilgili adım "capped" sayılır ve görev yine tamamlanabilir.

## Endpointler (`[Authorize]`, `api/v1/missions`)

### `GET /today?timeZone=Europe/Istanbul&utcOffsetMinutes=180`
Salt okunur. 200:
```json
{ "missionDate": "2026-10-06", "status": "notStarted|inProgress|completed",
  "mission": null,
  "preview": { "estimatedMinutes": 6, "reviewCount": 8, "newWordCount": 5, "hasListening": true },
  "streak": { "current": 3, "longest": 7, "completedToday": false },
  "limits": { "isPremium": false, "dailyWordsRemaining": 12 } }
```
`dailyWordsRemaining`: `-1` sınırsız. `mission` görev başlamışsa `DailyMissionDto`. `pendingMission`: önceki günden kalan, hâlâ tamamlanabilir (InProgress) görev varsa `DailyMissionDto`, yoksa null; mobil bunu "Dünkü görevini bitir" olarak sunar.

### `POST /today` — body `{ "timeZone": "Europe/Istanbul", "utcOffsetMinutes": 180 }`
Bugünün görevini oluşturur veya mevcut olanı döndürür (idempotent: kullanıcı + görev günü). 200 `DailyMissionDto`; geçersiz saat dilimi 400.

`DailyMissionDto`:
```json
{ "id": "guid", "practiceSessionId": "guid", "missionDate": "2026-10-06", "status": "inProgress|completed",
  "estimatedMinutes": 6,
  "steps": [ { "key": "review|new-words|recall|listening", "order": 1, "title": "Tekrar", "required": 8, "done": 3, "completed": false } ],
  "reviewWords": [ VocabularyWordDto ], "newWords": [ VocabularyWordDto ],
  "recallQuestions": [ { "id": "guid", "prompt": "…", "speakText": "…|null", "options": [ { "key": "A", "text": "…" } ], "answered": false, "isCorrect": null } ],
  "listening": { "id": "guid", "title": "…", "level": "A2", "prompt": "…", "transcript": "…", "options": [ { "key": "A", "text": "…" } ], "answered": false, "isCorrect": null },
  "result": null }
```
Doğru şık ve çeviri cevaplanmadan önce gönderilmez; hatırlama sorusu cevaplanmadan kelime kimliği gönderilmez. `speakText` yalnızca soru İngilizce terimi gösteriyorsa doludur (cevabı seslendirmez). Kartlarda zaten gösterilen kelimelerin hatırlanması tasarım gereğidir. `listening.transcript` TTS için gönderilir ama ekranda gösterilmez. `listening` null olabilir; o zaman `listening` adımı listede yer almaz. `required: 0` olan adım (kota bitti) `completed: true` döner.

### Tekrar ve yeni kelime adımları
Mevcut `POST /api/v1/reviews` kullanılır: `{ wordId, rating, clientEventId, practiceSessionId, practiceStepKey: "review" | "new-words" }`. 429 dönebilir (ücretsiz kota).

### `POST /{id}/answers` — body `{ "stepKey": "recall|listening", "questionId": "guid", "optionKey": "A" }`
200 `{ "questionId", "isCorrect", "correctOptionKey", "explanation", "wordId", "term", "translation", "step": { "key", "done", "required", "completed" } }` (`wordId/term/translation` dinleme sorusunda null). Tamamlanmış görevde 409. İlk cevap geçerlidir; aynı soruya tekrar gönderim saklanan sonucu döndürür. Bilinmeyen adım/soru 400, başkasının görevi 404.

### `POST /{id}/complete`
200 `MissionResultDto`:
```json
{ "missionId": "guid", "xpAwarded": 86,
  "xpBreakdown": { "base": 30, "items": 31, "accuracy": 20, "streakBonus": 5 },
  "correctAnswers": 4, "totalAnswers": 5, "reviewedWords": 8, "newWords": 5,
  "streak": { "current": 4, "longest": 7, "extended": true },
  "alreadyCompleted": false,
  "tomorrow": { "date": "2026-10-07", "dueReviewCount": 6, "newWordCount": 5, "estimatedMinutes": 6 } }
```
Adımlar eksikse 409 `{ "message", "incompleteSteps": ["recall"] }`; tamamlama süresi dolmuşsa 409 ve boş `incompleteSteps`; başkasının görevi 404. Tekrar çağrı 200 ve `alreadyCompleted: true` (XP ikinci kez verilmez).

## Kurallar

- **XP (`MissionXpRules`):** taban 30 + tekrar başına 2 + yeni kelime başına 3 + doğru cevap başına 5 + seri ≥ 3 ise 5. XP yalnızca bir kez verilir (koşullu güncelleme).
- **Gün:** kullanıcının kayıtlı saat dilimi kullanılır. İlk istekte gönderilen saat dilimi kaydedilir (geçerli IANA, tanınmıyorsa geçerli `utcOffsetMinutes`, ikisi de yoksa UTC); farklı bir saat dilimi en fazla 7 günde bir kabul edilir, aksi hâlde kayıtlı olan kullanılır. Hiç kullanılabilir değer yoksa 400. Görev günü geri gidemez.
- **Tamamlama süresi:** görev gününün (kayıtlı saat diliminde) bitişinden 6 saat sonrasına kadar. Kota bittiği için "capped" sayılan adımlar görevde saklanır, gün değişince geri açılmaz.
- **Seri:** tamamlanmış görevlerin ardışık görev günleri. Dashboard'daki tekrar serisi ayrı kalır.
- **Lider tablosu:** görev XP'si haftalık puana eklenir; görevdeki kelime tekrarları ayrıca tekrar puanı kazandırır (bilinçli tercih).
- **Veri:** `daily_missions` tablosu (kullanıcı + görev günü tekil). Adımlar `PracticeSessionStep`, cevaplar `PracticeEvent` (`ClientEventId = mission:{missionId}:{questionId}`) olarak saklanır.
