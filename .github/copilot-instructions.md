# GitHub Copilot Instructions — One Roof

Please read and follow [AGENTS.md](../AGENTS.md) for all architectural rules, layer boundaries, and coding conventions.

## Core Rules
- **Domain layer is pure C#:** Never add `UnityEngine` references to `OneRoof.Domain` or `OneRoof.Application`.
- **Visible NPCs are pooled views:** NPCs are views of persistent simulation entities, bound via entity IDs and read-only projections. 40-view cap.
- **Assets and .meta files:** Every asset must have its matching `.meta` file committed.
- **Save data:** Stored as versioned envelopes; never store GameObject or Transform references in save data.
- **Decisions & Handoffs:** Follow the protocols in `AGENTS.md`, record decisions in `Docs/07_DECISION_LOG.md`, and track tasks in `Planning/BACKLOG.md`.
