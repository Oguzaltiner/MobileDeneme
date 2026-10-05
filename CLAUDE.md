@AGENTS.md

## Claude Code mapping

`AGENTS.md` names roles the Codex way (`snake_case`). In Claude Code they are subagents in `.claude/agents/` and skills in `.claude/skills/`:

| AGENTS.md role | Claude Code |
|---|---|
| task-orchestrator, feature-development | skills `task-orchestrator`, `feature-development` |
| task_planner | agent `task-planner` |
| solution_architect | agent `solution-architect` |
| implementation_worker (also admin-web-worker) | agent `implementation-worker` |
| mobile_reviewer | agent `mobile-reviewer` |
| backend_reviewer | agent `backend-reviewer` |
| db_reviewer | agent `db-reviewer` |
| security_reviewer | agent `security-reviewer` |
| test_agent | agent `test-agent` |
| reviewer | agent `reviewer` |
| admin-web-reviewer (`docs/ADMIN_WEB_AGENT.md`) | agent `admin-web-reviewer` |

The Codex definitions in `.codex/agents/` and `.agents/skills/` are kept for Codex; when changing a role, update both.

## Checks

- Mobile: `npm run typecheck` in `mobile/`
- Backend: `dotnet build backend/EnglishLearning.slnx`
- Admin web: `npm run typecheck` and `npm run build` in `admin-web/`

Never commit `*.log` files or `.codex-remote-attachments/`.
