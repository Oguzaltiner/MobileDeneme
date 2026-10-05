import { ChangeEvent, useRef, useState } from 'react';
import { adminApi, ApiError, VocabularyImportInvalid, VocabularyImportResult } from './api';
import { addImportResult, chunk, countBy, emptyImportTotals, ImportProblem, importChunkSize, maxImportFileBytes, ParsedPack, parsePack } from './vocabularyImport';

type InvalidRow = VocabularyImportInvalid & { row: number };
type ImportFailure = { message: string; status?: number; chunk: number; chunks: number; importedBefore: VocabularyImportResult; importedWords: number; invalid: InvalidRow[] };
type ImportSuccess = { totals: VocabularyImportResult; words: number; chunks: number };

const previewRows = 10;
const problemLimit = 50;

function invalidRows(error: unknown, chunkIndex: number): InvalidRow[] {
  if (!(error instanceof ApiError) || error.status !== 400 || typeof error.body !== 'object' || error.body === null) return [];
  const invalid = (error.body as { invalid?: unknown }).invalid;
  if (!Array.isArray(invalid)) return [];
  return invalid.filter((x): x is Record<string, unknown> => typeof x === 'object' && x !== null).map(x => { const index = typeof x.index === 'number' && Number.isInteger(x.index) && x.index >= 0 ? x.index : -1; return { index, row: index >= 0 ? chunkIndex * importChunkSize + index + 1 : 0, term: typeof x.term === 'string' && x.term.trim() ? x.term : '—', reason: typeof x.reason === 'string' ? x.reason : 'Geçersiz kayıt.' }; });
}

function ProblemList({ title, items, tone }: { title: string; items: ImportProblem[]; tone: 'error' | 'warning' }) {
  if (items.length === 0) return null;
  return <div className={`problem-list ${tone}`}><strong>{title} ({items.length})</strong><ul>{items.slice(0, problemLimit).map((item, index) => <li key={`${item.row}-${index}`}><span>#{item.row}</span> <b>{item.term}</b> — {item.reason}</li>)}</ul>{items.length > problemLimit ? <small>+{items.length - problemLimit} kayıt daha…</small> : null}</div>;
}

export function ImportPanel({ onImported }: { onImported: () => void }) {
  const [fileName, setFileName] = useState(''); const [pack, setPack] = useState<ParsedPack>(); const [parseError, setParseError] = useState('');
  const [busy, setBusy] = useState(false); const [progress, setProgress] = useState(''); const [success, setSuccess] = useState<ImportSuccess>(); const [failure, setFailure] = useState<ImportFailure>();
  const running = useRef(false);

  const pick = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    setPack(undefined); setParseError(''); setSuccess(undefined); setFailure(undefined); setFileName(file?.name ?? '');
    if (!file) return;
    if (file.size > maxImportFileBytes) { setParseError('Dosya çok büyük (en fazla 10 MB).'); return; }
    try { setPack(parsePack(await file.text())); } catch (e) { setParseError(e instanceof Error ? e.message : 'Dosya okunamadı.'); }
  };

  const run = async () => {
    if (!pack || pack.errors.length > 0 || pack.words.length === 0 || running.current) return;
    running.current = true; setBusy(true); setSuccess(undefined); setFailure(undefined);
    const chunks = chunk(pack.words, importChunkSize); let totals = emptyImportTotals(); let importedWords = 0; let mayHaveSaved = false;
    try {
      for (let index = 0; index < chunks.length; index++) {
        setProgress(chunks.length > 1 ? `Parça ${index + 1}/${chunks.length} gönderiliyor (${chunks[index].length} kelime)…` : `${chunks[index].length} kelime gönderiliyor…`);
        try {
          totals = addImportResult(totals, await adminApi.importVocabulary(chunks[index]));
          importedWords += chunks[index].length;
        } catch (e) {
          setFailure({ message: e instanceof Error ? e.message : 'İçe aktarma başarısız.', status: e instanceof ApiError ? e.status : undefined, chunk: index + 1, chunks: chunks.length, importedBefore: totals, importedWords, invalid: invalidRows(e, index) });
          if (!(e instanceof ApiError)) mayHaveSaved = true;
          return;
        }
      }
      setSuccess({ totals, words: importedWords, chunks: chunks.length });
    } finally {
      running.current = false; setBusy(false); setProgress('');
      if (importedWords > 0 || mayHaveSaved) onImported();
    }
  };

  const levelCounts = pack ? countBy(pack.words, word => word.level) : [];
  const categoryCounts = pack ? countBy(pack.words, word => word.category) : [];
  const chunkCount = pack ? Math.ceil(pack.words.length / importChunkSize) : 0;
  const canImport = !!pack && pack.words.length > 0 && pack.errors.length === 0 && !busy;

  return <section className="panel import-panel">
    <div className="panel-header"><div><p className="eyebrow">CONTENT PACK</p><h2>Toplu içe aktar</h2><p className="muted import-hint">Kelime paketi JSON dosyasını seç (ör. <code>content/vocabulary/a1.json</code>). Yeni ve güncellenen kelimeler <b>İncelemede</b> durumuna alınır, yayınlanmış kelimeler atlanır.</p></div></div>
    <div className="import-picker"><input type="file" accept=".json,application/json" disabled={busy} onChange={e => void pick(e)} aria-label="Kelime paketi JSON dosyası" />{fileName ? <span className="muted">{fileName}</span> : null}</div>
    {parseError ? <div className="alert error">{parseError}</div> : null}
    {pack ? <div className="import-preview">
      <div className="chip-row"><span className="chip strong">{pack.words.length} kelime</span>{pack.packLevel ? <span className="chip">Paket seviyesi: {pack.packLevel}</span> : null}{chunkCount > 1 ? <span className="chip">{chunkCount} parça × en fazla {importChunkSize}</span> : null}</div>
      <div className="chip-row"><span className="chip-label">Seviye</span>{levelCounts.map(([key, count]) => <span className="chip" key={key}>{key}: {count}</span>)}</div>
      <div className="chip-row"><span className="chip-label">Kategori</span>{categoryCounts.map(([key, count]) => <span className="chip" key={key}>{key}: {count}</span>)}</div>
      <div className="catalog-table-wrap"><table className="catalog-table preview-table"><thead><tr><th>#</th><th>Kelime</th><th>Çeviri</th><th>Seviye</th><th>Kategori</th><th>Örnek cümle</th></tr></thead><tbody>{pack.words.slice(0, previewRows).map((word, index) => <tr key={index}><td>{index + 1}</td><td><strong>{word.term}</strong><small>{word.partOfSpeech}{word.pronunciation ? ` · ${word.pronunciation}` : ''}</small></td><td>{word.translation}</td><td>{word.level}</td><td>{word.category}</td><td>{word.exampleSentence}</td></tr>)}</tbody></table></div>
      {pack.words.length > previewRows ? <p className="muted catalog-count">İlk {previewRows} satır gösteriliyor.</p> : null}
      <ProblemList title="Ön kontrol hataları — düzeltmeden içe aktarılamaz" items={pack.errors} tone="error" />
      <ProblemList title="Uyarılar" items={pack.warnings} tone="warning" />
      <div className="import-actions"><button className="primary" disabled={!canImport} onClick={() => void run()}>{busy ? 'İçe aktarılıyor…' : `İçe aktar (${pack.words.length} kelime)`}</button>{progress ? <span className="muted">{progress}</span> : null}</div>
    </div> : null}
    {success ? <div className="alert success"><strong>İçe aktarma tamamlandı.</strong> {success.totals.created} yeni, {success.totals.updated} güncellendi, {success.totals.skippedPublished.length} yayınlanmış kelime atlandı (sunucu toplamı: {success.totals.total}{success.chunks > 1 ? `, ${success.chunks} parça` : ''}). Yeni ve güncellenen kelimeler inceleme kuyruğunda; oradan toplu yayınlayabilirsin.{success.totals.skippedPublished.length > 0 ? <details><summary>Atlanan yayınlanmış kelimeler</summary><p>{success.totals.skippedPublished.join(', ')}</p></details> : null}</div> : null}
    {failure ? <div className="alert error">
      <strong>İçe aktarma başarısız{failure.chunks > 1 ? ` — parça ${failure.chunk}/${failure.chunks}` : ''}.</strong> {failure.message}
      {failure.status === 401 ? ' Çıkış yapıp yeniden giriş yapman gerekiyor.' : null}
      {failure.invalid.length > 0 ? ' Bu parçadan hiçbir kelime yazılmadı.' : null}
      {failure.status === undefined ? <p>Sunucudan yanıt alınamadı (bağlantı hatası). {failure.chunks > 1 ? `Parça ${failure.chunk}` : 'Gönderilen kelimeler'} kaydedilmiş olabilir; içe aktarma mevcut kelimeleri güncellediği (upsert) için aynı dosyayı yeniden içe aktarmak güvenlidir.</p> : null}
      {failure.chunk > 1 ? <p>Önceki {failure.chunk - 1} parça ({failure.importedWords} kelime) zaten içe aktarıldı: {failure.importedBefore.created} yeni, {failure.importedBefore.updated} güncellendi, {failure.importedBefore.skippedPublished.length} atlandı. Kalan parçalar gönderilmedi.</p> : failure.chunks > 1 ? <p>{failure.status === undefined ? "Sonraki parçalar gönderilmedi." : "Hiçbir parça içe aktarılmadı; kalan parçalar gönderilmedi."}</p> : null}
      {failure.invalid.length > 0 ? <div className="problem-list error"><strong>Sunucunun reddettiği kayıtlar ({failure.invalid.length})</strong><ul>{failure.invalid.slice(0, problemLimit).map((item, index) => <li key={`${item.row}-${index}`}><span>{item.row > 0 ? `#${item.row}` : '#?'}</span> <b>{item.term}</b> — {item.reason}</li>)}</ul>{failure.invalid.length > problemLimit ? <small>+{failure.invalid.length - problemLimit} kayıt daha…</small> : null}</div> : null}
    </div> : null}
  </section>;
}
