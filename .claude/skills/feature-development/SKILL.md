---
name: feature-development
description: Implement React Native mobile, ASP.NET Core backend and admin-web features and bug fixes in MobileDeneme through repository discovery, bounded implementation, relevant checks and targeted review.
---

# MobileDeneme Feature Development

The client is React Native (Expo) and the backend is ASP.NET Core (.NET 10). Repository files define exact versions, libraries, architecture and commands.

- Trace the full path as applicable: screen/navigation → state (Zustand / TanStack Query) → network request → endpoint → application logic → persistence (EF Core + PostgreSQL) → response → admin-web consumer → tests.
- Inspect backend contracts directly and keep mobile and admin-web models aligned with them.
- The .NET version was chosen during initial setup (.NET 10, `global.json`). Have `solution-architect` revisit it only when explicitly asked, using the official support lifecycle, package compatibility, installed SDKs and deployment constraints, and record the decision.
- Choose Expo vs bare React Native, and libraries, only from product or technical requirements. Keep features working in Expo Go unless a native build is a deliberate decision.
- Never send correct answers to the client before the server validates them (quiz, games), and never grant Premium on the client.
- Preserve git state and prefer the smallest safe change.
- Give `implementation-worker` agents bounded, non-overlapping file ownership; workers do not commit or push.
- Use `mobile-reviewer` for React Native changes and other reviewers only when relevant.
- Report actual commands and results, platform limitations and risks.
