# MobileDeneme Agent Workflow

These instructions apply to the whole workspace unless a deeper `AGENTS.md`
overrides them.

## Working principles

- Treat the user's current request as the source of scope and acceptance criteria.
- Inspect the repository and its instructions before proposing or editing code.
- Derive technology, commands, architecture, and conventions from repository files;
  do not assume a framework because of the project name.
- Preserve unrelated user changes and keep task changes isolated.
- Prefer the smallest safe change that follows existing patterns.
- Do not add dependencies, migrations, broad refactors, or generated files unless the
  task requires them.

## Delivery workflow

For non-trivial implementation work, use the `task-orchestrator` skill. Use the
`feature-development` skill for features, fixes, and behavior changes.

The normal flow is:

1. Record Git state and discover the relevant execution path.
2. Define acceptance criteria and a proportional plan.
3. Ask `task_planner` for complex or cross-cutting work.
4. Ask `solution_architect` when architecture, contracts, persistence, or several
   components are affected.
5. Assign implementation to `implementation_worker` with exclusive file ownership.
6. Run relevant builds, tests, linters, or static checks.
7. Select only reviewers relevant to the actual diff.
8. Fix confirmed blocking findings and re-verify.
9. Ask `reviewer` for the final production-focused diff review on substantial work.
10. Commit or push only when the user explicitly requested it or a clear standing
    project instruction authorizes it. Never force-push.

## Multi-agent safety

- Parallelize only independent read-only analysis or work on disjoint files.
- Never let two implementation agents edit the same files concurrently.
- The root agent owns scope, integration, verification, and the final report.
- Reviewer, planner, architect, and test-analysis agents are read-only.
- Agent reports are advisory: verify important findings against the repository.

## Verification and reporting

- Never claim a command passed unless it completed successfully.
- If verification is blocked, report the command, the blocker, and the remaining risk.
- Final reports should state what changed, what was verified, material findings, and
  any unresolved risks. Distinguish pre-existing changes from task changes.
