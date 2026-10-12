# Active Handoff — Baseline, Transit & 3D Presentation Architecture

Updated: 2026-10-12. Authoritative active status summary.

## Current Status
- **Baseline**: 832 EditMode tests passing, 15 PlayMode tests passing. All core assemblies (Domain, Content, Application, UI, Presentation) compile cleanly via `/home/geisha/.dotnet/dotnet build OneRoof.Presentation.csproj` (0 errors, 0 warnings).
- **Fast Domain Runner**: `/home/geisha/.dotnet/dotnet run --project Tools/DesignValidation/CrisisCheckpointTests.csproj` passes 190/190 tests in ~1.5s.
- **Transit Tasks 1–3 Complete**: Required-intent continuity, movement/dispatch fixes, and production employment routines verified.
- **Transit Tasks 4–5 Pending**: Reachable providers and physiological budgets; complete-demand validation, save replay verification.
- **3D Rooms & Hybrid Facade Architecture (OR-1200 Series) Ready**: Roadmap and backlog updated with OR-1201 through OR-1205. The execution plan and copyable sub-agent prompts are authoritative in `Planning/3D_ROOMS_AND_FACADE_ARCHITECTURE.md`.

## Acceptance & Architectural Constraints
- **Transit Goals**: Retain >=99% required episodes within 360 ticks, terminal nearest-rank wait p95 <= 60 ticks, live max wait <= 240 ticks, and zero unresolved unreachable demand.
- **Presentation & Scale Goals**: Sustain reference scale (30 floors / 300 residents) under <180 draw calls, <100 batches, >=60 FPS, and 0 steady-state heap allocations per tick.
- **Simulation Invariants**:
  - Keep 3-car elevator cap and authored reference 3x20 capacities.
  - Domain purity: `OneRoof.Domain` and `OneRoof.Application` maintain zero `UnityEngine` references.
  - Simulation runs on discrete 1440 ticks/day.
  - Aggregate persistence lives strictly in domain `ToSaveData()` / `FromSaveData()`. Zero scene references in saves.
- **Testing Protocol**: Tier 1 (`dotnet build` / `dotnet run` standalone runner) for all routine iteration; `unity-cli` when Editor is live; headless Unity batchmode strictly on-demand for full milestone validation.
