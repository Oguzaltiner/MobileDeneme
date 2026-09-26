---
name: feature-development
description: Implement React Native mobile and ASP.NET Core backend features and bug fixes in MobileDeneme through repository discovery, bounded implementation, relevant checks, and targeted review.
---

# MobileDeneme Feature Development
The client is React Native and the backend is ASP.NET Core. Repository files define exact versions, libraries, architecture, and commands.

- Trace screen/navigation, state, network request, endpoint, application logic, persistence, response, and tests as applicable.
- Inspect backend contracts directly and keep mobile models aligned.
- During initial backend setup, have solution_architect choose .NET 9 or .NET 10 using official support lifecycle, package compatibility, installed SDKs, and deployment constraints. Record the decision.
- Choose Expo versus bare React Native and libraries only from product or technical requirements.
- Preserve Git state and prefer the smallest safe change.
- Give workers bounded, non-overlapping file ownership; workers do not commit/push.
- Use mobile_reviewer for React Native changes and other reviewers only when relevant.
- Report actual commands/results, platform limitations, and risks.