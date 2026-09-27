# Security policy

## Supported branches

Security fixes are applied to `master` (release) and `dev` (integration).

## Reporting a vulnerability

Do not open a public issue with credentials, tokens, purchase receipts, or exploit details. Send a private report to the repository owner with reproduction steps, affected version, and impact. Remove secrets from logs and rotate any exposed key immediately.

## Release safeguards

- Production JWT signing keys must come from a secret manager or deployment secret.
- Store purchase verification credentials must never be committed.
- CI runs dependency vulnerability checks; Dependabot tracks NuGet, npm, and GitHub Actions updates.
