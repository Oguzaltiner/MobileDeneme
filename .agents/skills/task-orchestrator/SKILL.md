---
name: task-orchestrator
description: Orchestrate non-trivial repository tasks from discovery through implementation, targeted review, verification, and authorized delivery.
---

# Task Orchestrator
The user request defines scope; AGENTS.md and repository evidence define constraints.
1. Read instructions, inspect workspace, record Git state for affected repositories.
2. Preserve unrelated work and define acceptance criteria.
3. Use task_planner for complex work and solution_architect for cross-component decisions.
4. Give implementation_worker bounded tasks with exclusive file ownership.
5. Run relevant checks; select specialist reviewers only when relevant to the diff.
6. Reviewers are read-only; verify findings. Fix confirmed Critical/High issues and re-verify.
7. Use reviewer as final gate for substantial changes.
8. Inspect final diff/status. Commit or push only when authorized. Never force-push.
9. Report changes, actual checks/results, findings, delivery, and risks.