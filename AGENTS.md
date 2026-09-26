# MobileDeneme Agent Workflow

These instructions apply to the whole workspace unless a deeper AGENTS.md overrides them.

## Technology baseline
- The client is a React Native mobile application.
- The backend is ASP.NET Core on .NET 9 or .NET 10.
- During initial setup, solution_architect chooses .NET 9 or .NET 10 from official support lifecycle, package compatibility, installed SDKs, and target deployment. Record the decision and reasons.
- Choose Expo versus bare React Native, navigation, state, database, and hosting from product requirements.
- The backend contract is authoritative; keep mobile/backend models aligned.

## Workflow
For non-trivial work use task-orchestrator; for features and fixes also use feature-development.
1. Record Git state and trace the mobile-to-backend path.
2. Define acceptance criteria; use task_planner for complex work.
3. Use solution_architect for architecture, contracts, persistence, or technology decisions.
4. Give implementation_worker bounded, non-overlapping file ownership.
5. Run relevant checks.
6. Use mobile_reviewer for React Native changes and other reviewers only when relevant.
7. Fix confirmed blockers, re-verify, and use reviewer as final gate for substantial work.
8. Commit/push only when authorized; never force-push.

Planner, architect, test analyst, and reviewers are read-only. Root owns integration and delivery. Preserve unrelated work and report changes, actual checks/results, platform limitations, delivery details, and risks.