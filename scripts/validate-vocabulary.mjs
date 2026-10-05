// Validates vocabulary content packs before they are imported through
// POST /api/v1/admin/content/vocabulary/import.
// Usage: node scripts/validate-vocabulary.mjs content/vocabulary/*.json
// Accepts pack files ({ version, level, words: [...] }) or bare arrays.
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const LEVELS = new Set(['A1', 'A2', 'B1', 'B2', 'C1', 'C2']);
export const CATEGORIES = new Set([
  'People & Family', 'Daily Life', 'Food & Drink', 'Home', 'Travel', 'Nature', 'Health', 'Work',
  'Shopping & Money', 'Academic', 'Technology', 'Personality', 'Society', 'Time & Numbers', 'Entertainment',
  // Categories already used by the curated seed.
  'Exam'
]);
const PARTS_OF_SPEECH = new Set(['noun', 'verb', 'adjective', 'adverb', 'number', 'preposition', 'pronoun', 'conjunction', 'interjection']);
const MAX = { term: 120, pronunciation: 120, partOfSpeech: 40, definition: 500, translation: 160, level: 10, category: 80, exampleSentence: 500 };
const TURKISH = /^[a-zçğıöşüâîû0-9 ,'’\-\/]+$/i;

const escape = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
export const containsWholeWord = (sentence, term) => new RegExp(`\\b${escape(term)}\\b`, 'i').test(sentence);

export function validateWord(w) {
  const errors = [];
  for (const field of Object.keys(MAX)) {
    const value = w[field];
    if (typeof value !== 'string' || value.trim() === '') { errors.push(`${field} is required`); continue; }
    if (value.length > MAX[field]) errors.push(`${field} longer than ${MAX[field]}`);
    if (value !== value.trim()) errors.push(`${field} has surrounding whitespace`);
  }
  if (errors.length) return errors;
  if (!LEVELS.has(w.level)) errors.push(`unknown level ${w.level}`);
  if (!CATEGORIES.has(w.category)) errors.push(`unknown category ${w.category}`);
  if (!PARTS_OF_SPEECH.has(w.partOfSpeech)) errors.push(`unknown partOfSpeech ${w.partOfSpeech}`);
  if (!/^\/.+\/$/.test(w.pronunciation)) errors.push('pronunciation must be IPA in slashes');
  if (w.translation.toLowerCase() === w.term.toLowerCase()) errors.push('translation equals term');
  if (w.translation.length > 60) errors.push('translation longer than 60 characters');
  if (!TURKISH.test(w.translation)) errors.push(`translation has unexpected characters: ${w.translation}`);
  if (containsWholeWord(w.definition, w.term)) errors.push('definition contains the term');
  if (!containsWholeWord(w.exampleSentence, w.term)) errors.push('exampleSentence does not contain the term as a whole word');
  if (!/[.!?]$/.test(w.exampleSentence)) errors.push('exampleSentence must end with punctuation');
  const words = w.exampleSentence.split(/\s+/).length;
  if (words < 5 || words > 20) errors.push(`exampleSentence has ${words} words (5-20 expected)`);
  return errors;
}

export function loadWords(path) {
  const data = JSON.parse(readFileSync(path, 'utf8'));
  return Array.isArray(data) ? data : data.words;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const files = process.argv.slice(2);
  if (files.length === 0) { console.error('usage: node scripts/validate-vocabulary.mjs <pack.json>...'); process.exit(2); }
  const seen = new Map();
  const translations = new Map();
  let total = 0, failed = 0;
  for (const file of files) {
    for (const w of loadWords(file)) {
      total++;
      const errors = validateWord(w);
      const key = String(w.term ?? '').toLowerCase();
      if (seen.has(key)) errors.push(`duplicate term (also in ${seen.get(key)})`);
      seen.set(key, file);
      if (errors.length) { failed++; console.log(`${file}: ${w.term}: ${errors.join('; ')}`); }
      if (w.translation) translations.set(w.translation, [...(translations.get(w.translation) ?? []), w.term]);
    }
  }
  const shared = [...translations].filter(([, terms]) => terms.length > 1);
  if (shared.length) {
    console.log(`\nWarning: ${shared.length} translations are shared by several terms (quiz answers may look alike):`);
    for (const [translation, terms] of shared) console.log(`  ${translation}: ${terms.join(', ')}`);
  }
  console.log(`\n${total} words, ${failed} invalid`);
  process.exit(failed ? 1 : 0);
}
