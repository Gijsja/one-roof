# Handoff Protocol

Use one active handoff per task: `Active/OR-NNN_short-name.md`.

The outgoing agent fills the template completely. The incoming agent verifies repository state instead of assuming the narrative is correct, then continues from `Next safe action`.

When accepted, move the handoff to `Archive/` and update the backlog. Do not archive a handoff with unresolved failures; mark it blocked.

Handoffs are facts, not diaries. Include decisions, changed files, commands, evidence, risks, and unfinished work. Omit speculative detail that does not change the next action.

For Unity validation commands, use [Docs/10_DEVELOPMENT_WORKFLOW.md](../Docs/10_DEVELOPMENT_WORKFLOW.md) rather than repeating environment-specific recovery steps in every handoff.
