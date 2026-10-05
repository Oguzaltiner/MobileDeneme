// Imports vocabulary content packs through the admin API and, with --publish,
// bulk-publishes the imported words that are not listed in the publish reserve.
// Usage:
//   node scripts/import-vocabulary.mjs [--publish] [content/vocabulary/a1.json ...]
// Environment: API_BASE_URL (default http://localhost:5057), ADMIN_EMAIL, ADMIN_PASSWORD.
// The local development defaults match scripts/smoke.ps1 and only apply to localhost.
import { readFileSync } from 'node:fs';
import { loadWords, validateWord } from './validate-vocabulary.mjs';

const baseUrl = (process.env.API_BASE_URL ?? 'http://localhost:5057').replace(/\/$/, '');
const isLocal = /^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/.test(baseUrl);
const email = process.env.ADMIN_EMAIL ?? (isLocal ? 'test@example.com' : undefined);
const password = process.env.ADMIN_PASSWORD ?? (isLocal ? 'Test1234!' : undefined);
if (!email || !password) { console.error('ADMIN_EMAIL and ADMIN_PASSWORD are required for non-local APIs.'); process.exit(2); }

const args = process.argv.slice(2);
const publish = args.includes('--publish');
const files = args.filter(a => !a.startsWith('--'));
if (files.length === 0) files.push(...['a1', 'a2', 'b1', 'b2'].map(l => `content/vocabulary/${l}.json`));
const reserve = new Set(JSON.parse(readFileSync('content/vocabulary/publish-reserve.json', 'utf8')).map(t => t.toLowerCase()));

async function call(path, token, init = {}) {
  const response = await fetch(`${baseUrl}/api/v1${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...init.headers }
  });
  const text = await response.text();
  const body = text ? JSON.parse(text) : null;
  if (!response.ok) { const error = new Error(`${init.method ?? 'GET'} ${path} -> ${response.status}`); error.body = body; throw error; }
  return body;
}

const words = files.flatMap(loadWords);
const invalid = words.map(w => [w.term, validateWord(w)]).filter(([, errors]) => errors.length);
if (invalid.length) { for (const [term, errors] of invalid) console.error(`${term}: ${errors.join('; ')}`); process.exit(1); }

const { accessToken } = await call('/auth/login', null, { method: 'POST', body: JSON.stringify({ email, password }) });
try {
  for (let i = 0; i < words.length; i += 1000) {
    const chunk = words.slice(i, i + 1000);
    const result = await call('/admin/content/vocabulary/import', accessToken, { method: 'POST', body: JSON.stringify({ words: chunk }) });
    console.log(`import ${i + 1}-${i + chunk.length}: created ${result.created}, updated ${result.updated}, skipped published ${result.skippedPublished.length}`);
  }

  if (publish) {
    const packTerms = new Set(words.map(w => w.term.toLowerCase()));
    const queue = await call('/admin/content/vocabulary/review-queue?status=InReview', accessToken);
    const ids = queue.filter(w => packTerms.has(w.term.toLowerCase()) && !reserve.has(w.term.toLowerCase())).map(w => w.id);
    let published = 0;
    for (let i = 0; i < ids.length; i += 100) {
      const result = await call('/admin/content/vocabulary/bulk-publish', accessToken, { method: 'POST', body: JSON.stringify({ ids: ids.slice(i, i + 100) }) });
      published += result.published ?? ids.slice(i, i + 100).length;
    }
    console.log(`published ${published}; ${queue.length - ids.length} words stay in review`);
  }
} catch (error) {
  console.error(error.message, JSON.stringify(error.body ?? ''));
  process.exit(1);
}
