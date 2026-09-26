---
name: feature-development
description: Implement features and bug fixes in MobileDeneme through repository discovery, bounded implementation, relevant checks, and targeted review.
---

# MobileDeneme Feature Development
Repository files define the stack, architecture, commands, and conventions.
- Trace relevant entry points, logic, persistence, contracts, UI consumers, and tests.
- Read manifests, lockfiles, project files, CI, and nearby code before choosing tools.
- Inspect source contracts and Git state; preserve existing work.
- Prefer the smallest safe change; avoid unnecessary dependencies, abstractions, schema changes, migrations, and refactors.
- Use planner/architect proportionally; assign bounded worker scope, no worker commit/push.
- Run relevant repository checks and select reviewers based on the diff.
- Report actual commands/results, limitations, and risks.