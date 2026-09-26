---
name: task-orchestrator
description: Orchestrate non-trivial MobileDeneme work from discovery through implementation, targeted review, verification, and authorized delivery.
---

# Task Orchestrator
1. Read instructions, inspect workspace, record Git state, preserve unrelated work, and define acceptance criteria.
2. Use task_planner for complex work and solution_architect for cross-component or technology decisions.
3. Give implementation_worker bounded tasks with exclusive file ownership.
4. Trace React Native and ASP.NET Core changes across the API boundary.
5. Run relevant checks; use mobile_reviewer for React Native changes and other reviewers only when relevant.
6. Reviewers are read-only. Verify findings, fix confirmed Critical/High issues, and re-verify.
7. Use reviewer as final gate for substantial work.
8. Inspect final diff/status. Commit/push only when authorized; never force-push.
9. Report changes, actual checks/results, findings, delivery, and risks.