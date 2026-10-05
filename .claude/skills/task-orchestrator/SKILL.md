---
name: task-orchestrator
description: Orchestrate non-trivial MobileDeneme work from discovery through planning, bounded implementation, targeted review, verification and authorized delivery. Use for any multi-step feature, fix, or task-list item.
---

# Task Orchestrator

1. Read `AGENTS.md`, inspect the workspace, record git state (branch, status, last commit), preserve unrelated work, and define acceptance criteria.
2. Use the `task-planner` agent for complex work and `solution-architect` for cross-component, contract, persistence or technology decisions.
3. Give `implementation-worker` agents bounded tasks with exclusive file ownership. Independent tasks can run in parallel; tasks touching the same files run one after another.
4. Trace React Native, admin-web and ASP.NET Core changes across the API boundary; the backend contract is authoritative.
5. Run the relevant checks yourself:
   - `npm run typecheck` in `mobile/`
   - `dotnet build backend/EnglishLearning.slnx`
   - `npm run typecheck` and `npm run build` in `admin-web/`
6. Use `mobile-reviewer` for React Native changes. Use `backend-reviewer`, `db-reviewer`, `security-reviewer`, `admin-web-reviewer` and `test-agent` only when relevant. Reviewers are read-only.
7. Verify each finding yourself, fix confirmed Critical/High issues, and re-verify.
8. Use `reviewer` as the final gate for substantial work.
9. Inspect the final diff and status. Stage only intended files: never logs (`*.log`), `.codex-remote-attachments/`, `dist/` or `node_modules/`. Commit/push only when authorized, to `dev`; never force-push.
10. Report changes, the checks actually run and their results, findings, delivery (commit hash, push) and remaining risks.
