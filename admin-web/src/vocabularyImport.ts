import type { VocabularyImportResult, VocabularyImportWord } from './api';

export const importChunkSize = 1000;
export const maxImportFileBytes = 10 * 1024 * 1024;
export const levels = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'];

const fields: (keyof VocabularyImportWord)[] = ['term', 'pronunciation', 'partOfSpeech', 'definition', 'translation', 'level', 'category', 'exampleSentence'];
const requiredFields = fields.filter(field => field !== 'pronunciation');
/** Mirrors backend VocabularyQuality.FindImportProblems max lengths (measured after trim, UTF-16 code units like .NET string.Length). */
export const fieldMaxLengths: Record<keyof VocabularyImportWord, number> = { term: 120, pronunciation: 120, partOfSpeech: 40, definition: 500, translation: 160, level: 10, category: 80, exampleSentence: 500 };
export const pendingTranslationPlaceholder = 'Çeviri inceleme bekliyor';
// Mirrors VocabularyQuality.IsPlaceholderDefinition on the server.
const placeholderDefinitions = ['Bu kelimenin Türkçe karşılığı içerik ekibi tarafından doğrulanıyor.', 'Common English word used in everyday context.'];
export const fieldLabels: Record<keyof VocabularyImportWord, string> = { term: 'Kelime', pronunciation: 'Telaffuz', partOfSpeech: 'Sözcük türü', definition: 'Tanım', translation: 'Çeviri', level: 'Seviye', category: 'Kategori', exampleSentence: 'Örnek cümle' };

export type ImportProblem = { row: number; term: string; reason: string };
export type ParsedPack = { words: VocabularyImportWord[]; rawCount: number; packLevel?: string; errors: ImportProblem[]; warnings: ImportProblem[] };

const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value);
const escapeRegex = (value: string) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
/** Approximates .NET OrdinalIgnoreCase equality. */
const equalsIgnoreCase = (a: string, b: string) => a.toUpperCase() === b.toUpperCase();
// .NET \w is [\p{L}\p{Mn}\p{Nd}\p{Pc}]; \b is the boundary between a \w and a non-\w character (or text edge).
const dotnetWord = '[\\p{L}\\p{Mn}\\p{Nd}\\p{Pc}]';
const dotnetBoundary = `(?:(?<=${dotnetWord})(?!${dotnetWord})|(?<!${dotnetWord})(?=${dotnetWord}))`;

/** Mirrors the server's `\b{Regex.Escape(term.Trim())}\b` with IgnoreCase. */
export function containsWholeWord(sentence: string, term: string): boolean {
  const needle = term.trim();
  if (!needle || !sentence.trim()) return false;
  return new RegExp(`${dotnetBoundary}${escapeRegex(needle)}${dotnetBoundary}`, 'iu').test(sentence);
}

/** Parses a pack file (`{ version, level, words: [...] }` or a bare array) and runs the client-side pre-check. Throws on structural problems. */
export function parsePack(content: string): ParsedPack {
  let root: unknown;
  try { root = JSON.parse(content.replace(/^﻿/, '')); } catch (e) { throw new Error(`Dosya geçerli bir JSON değil: ${e instanceof Error ? e.message : 'okunamadı'}`); }
  const rawWords = Array.isArray(root) ? root : isRecord(root) && Array.isArray(root.words) ? root.words : null;
  if (!rawWords) throw new Error('Beklenen biçim: { "version": 1, "level": "A1", "words": [ ... ] } veya kelime dizisi.');
  if (rawWords.length === 0) throw new Error('Dosyada içe aktarılacak kelime yok.');
  const packLevel = isRecord(root) && typeof root.level === 'string' && root.level.trim() ? root.level.trim() : undefined;

  const words: VocabularyImportWord[] = []; const errors: ImportProblem[] = []; const warnings: ImportProblem[] = []; const firstRowByTerm = new Map<string, number>();
  rawWords.forEach((raw, index) => {
    const row = index + 1;
    if (!isRecord(raw)) { errors.push({ row, term: '—', reason: 'Kayıt bir nesne değil.' }); return; }
    const word = {} as VocabularyImportWord;
    for (const field of fields) {
      const value = raw[field];
      if (value !== undefined && value !== null && typeof value !== 'string') errors.push({ row, term: typeof raw.term === 'string' ? raw.term : '—', reason: `${fieldLabels[field]} alanı metin olmalı.` });
      // Normalised exactly like the server (trim; level also upper-cased) and sent in this form.
      word[field] = typeof value === 'string' ? (field === 'level' ? value.trim().toUpperCase() : value.trim()) : '';
    }
    words.push(word);
    const term = word.term || '—';
    const blank = requiredFields.filter(field => !word[field]);
    if (blank.length > 0) errors.push({ row, term, reason: `Boş alan: ${blank.map(field => fieldLabels[field]).join(', ')}.` });
    const tooLong = fields.filter(field => word[field].length > fieldMaxLengths[field]);
    if (tooLong.length > 0) errors.push({ row, term, reason: `Çok uzun: ${tooLong.map(field => `${fieldLabels[field]} (en fazla ${fieldMaxLengths[field]})`).join(', ')}.` });
    if (!word.pronunciation) warnings.push({ row, term, reason: 'Telaffuz boş.' });
    if (word.level && !levels.includes(word.level)) errors.push({ row, term, reason: `Seviye A1–C2 olmalı (“${word.level}”).` });
    if (packLevel && levels.includes(word.level) && word.level !== packLevel.toUpperCase()) warnings.push({ row, term, reason: `Seviye (${word.level}) paket seviyesinden (${packLevel}) farklı.` });
    if (word.term && equalsIgnoreCase(word.term, word.translation)) errors.push({ row, term, reason: 'Çeviri kelimenin kendisiyle aynı olamaz.' });
    if (equalsIgnoreCase(word.translation, pendingTranslationPlaceholder)) errors.push({ row, term, reason: `Çeviri doğrulanmış olmalı (“${pendingTranslationPlaceholder}” kabul edilmez).` });
    if (placeholderDefinitions.some(text => equalsIgnoreCase(word.definition, text))) errors.push({ row, term, reason: 'Tanım yer tutucu metin olamaz.' });
    if (word.term && word.exampleSentence && !containsWholeWord(word.exampleSentence, word.term)) errors.push({ row, term, reason: 'Örnek cümle kelimeyi tam kelime olarak içermiyor.' });
    const key = word.term.toUpperCase();
    if (key) { const first = firstRowByTerm.get(key); if (first === undefined) firstRowByTerm.set(key, row); else errors.push({ row, term, reason: `Dosyada tekrar eden kelime (ilk geçtiği satır: ${first}).` }); }
  });
  return { words, rawCount: rawWords.length, packLevel, errors, warnings };
}

export function countBy<T>(items: T[], key: (item: T) => string): [string, number][] {
  const counts = new Map<string, number>();
  for (const item of items) { const value = key(item).trim() || '—'; counts.set(value, (counts.get(value) ?? 0) + 1); }
  return [...counts.entries()].sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0], 'tr'));
}

export function chunk<T>(items: T[], size: number): T[][] {
  const chunks: T[][] = [];
  for (let index = 0; index < items.length; index += size) chunks.push(items.slice(index, index + size));
  return chunks;
}

export const emptyImportTotals = (): VocabularyImportResult => ({ created: 0, updated: 0, skippedPublished: [], total: 0 });
export function addImportResult(totals: VocabularyImportResult, result: Partial<VocabularyImportResult> | null | undefined): VocabularyImportResult {
  return {
    created: totals.created + (Number(result?.created) || 0),
    updated: totals.updated + (Number(result?.updated) || 0),
    skippedPublished: [...totals.skippedPublished, ...(Array.isArray(result?.skippedPublished) ? result.skippedPublished.filter((x): x is string => typeof x === 'string') : [])],
    total: totals.total + (Number(result?.total) || 0)
  };
}
