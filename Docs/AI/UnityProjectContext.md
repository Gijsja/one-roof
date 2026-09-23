# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `/home/geisha/Vibecode/UnityAI/one-roof`
- Last analyzed: 2026-09-23
- Last analyzed commit: `b00d715` (HEAD)
- One Roof is a deterministic, pure-C# vertical-city simulation with a Unity cutaway presentation.

## Confirmed Environment

- Unity version: 6000.3.24f1.
- Render pipeline: Universal Render Pipeline 17.3.0 (confirmed in `Packages/manifest.json`).
- Input system: Unity Input System 1.20.0, with guarded legacy-input fallback in presentation code.
- Target platforms: desktop Linux is installed; product intent specifies premium single-player desktop.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.3.0 | Confirmed | `Packages/manifest.json` |
| Input | Input System 1.20.0 | Confirmed | `Packages/manifest.json`, `TowerPlayableController.cs` |
| Content | Addressables 4.0.1 | Confirmed | `Packages/manifest.json`, `Docs/02_ARCHITECTURE.md` |
| Tests | Unity Test Framework 1.6.0 with EditMode and PlayMode assemblies | Confirmed | `Packages/manifest.json`, `Assets/OneRoof/Tests/` |
| Unity tooling | One-shot headless Unity 6000.3.24f1 compile and test runs | Confirmed | `Docs/10_DEVELOPMENT_WORKFLOW.md` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/OneRoof/Runtime/Domain` | Pure deterministic simulation records and aggregates (0 UnityEngine references) | Confirmed | `OneRoof.Domain.asmdef`, `Docs/02_ARCHITECTURE.md` |
| `Assets/OneRoof/Runtime/Application` | Commands, projections, inspectors, overlays, simulation session | Confirmed | `OneRoof.Application.asmdef` |
| `Assets/OneRoof/Runtime/Presentation` | Cutaway world, Spine NPC views, parallax city, atmosphere, overlays | Confirmed | `OneRoof.Presentation.asmdef` |
| `Assets/OneRoof/Runtime/UI` | StewardTheme, ModeShellBarController, inspection and prediction cards | Confirmed | `OneRoof.UI.asmdef` |
| `Assets/OneRoof/Runtime/Content` | Immutable authored content contracts, sprite/wardrobe registries | Confirmed | `OneRoof.Content.asmdef` |
| `Assets/OneRoof/Editor` | Tower scene authoring, validation, and AssetLab tools | Confirmed | `OneRoof.Editor.asmdef` |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `OneRoof.Domain` | Pure simulation state & aggregates | none | `noEngineReferences: true` |
| `OneRoof.Application` | Commands, projections, session, data overlays | Domain | `noEngineReferences: true` |
| `OneRoof.Content` | Content definitions and catalogs | Domain | No mutable campaign state |
| `OneRoof.Infrastructure` | Persistence and schema migration | Domain, Application | Atomic file writes |
| `OneRoof.Presentation` | Unity views, Spine NPCs, camera, city parallax | Domain, Application, Content, UI, Input | Owns view-only state |
| `OneRoof.UI` | Mode shell dock, palettes, deep cards | Application, Domain | Dispatches commands, reads projections |

## Key Presenters & Subsystems

- `TowerPlayableController`: Lean composition root (~150 lines) routing `TowerProjection` snapshots to deep presenters.
- `TowerStructurePresenter`: Floor slabs, columns, baseline geometry, ground slab expansion.
- `ElevatorBankPresenter`: Shaft geometry, rails, cars, queue indicators (up to 3 cars per bank).
- `RoomPresenter`: 9-sliced room backdrops, door/window fixtures, demolition transitions.
- `TowerResidentPresenter`: Pooled Spine 2D NPCs, 8-layer wardrobe compositor, interaction point docking.
- `OutsideCityPresenter`: 3-depth parallax skyline, day/night lighting, and street edge boundary.
- `TowerAtmospherePresenter`: Day/night cycle mapping ticks to 24-hour time and unlit lighting tint.
- `TowerDataOverlays`: Consolidated lazy projection provider for the 8 contracted data overlays.
- `ModeShellBarController` & `StewardTheme`: Unified UI shell managing Build, Inspect, Data, and Manage modes.

## Scenes And Startup Flow

- Enabled build scenes: `Assets/Scenes/Tower.unity`, then `Assets/Scenes/Testbed_Transit.unity`.
- `Tower` is the interactive startup scene; `TowerSceneBuilder` creates a `Tower World` object with `TowerPlayableController`.
- `Tower_GroundStart.unity` is a lightweight from-scratch starting topology scene.
- `AssetLab.unity` is an editor-only validation preview and is not in Build Settings.

## Architecture & Data Contracts

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Domain/presentation boundary | Pure Domain state is projected into Unity presenters by stable IDs | Confirmed | `Docs/02_ARCHITECTURE.md`, assemblies |
| Ports & adapters | Placement validation lives behind `TowerSimulation.CanExecute(ICommand)` | Confirmed | ADR-038, `ARCH-003` |
| Aggregate-owned serialization | Sub-aggregates own `ToSaveData()` / `FromSaveData()` inside one envelope | Confirmed | ADR-039, `ARCH-004` |
| Outside boundary | First-class `WorldLocation` endpoint (`Room` or `Outside`) | Confirmed | ADR-071, ADR-072 |
| Day clock | Simulation tick mapped to 24-hour presentation cycle (1440 ticks/day) | Confirmed | ADR-067, ADR-068 |
| Unit economics | Closed-loop cash conservation model across treasury, households, businesses | Confirmed | `Docs/12_ECONOMY.md` |

## Testing And Validation

- EditMode tests: 450+ tests passing across layer assemblies.
- PlayMode tests: 5+ golden acceptance tests passing.
- One-shot headless batchmode runs in isolated worktrees are the required validation workflow.
- CI compile/test workflow remains blocked on the GitHub `UNITY_LICENSE` secret (`OR-005`).

## Important Constraints

- Player actions affect systems, never individual residents directly.
- The cause chain must remain: symptom → overlay → inspector → systems-level response.
- Steady-state simulation ticks must not allocate; presentation must remain projection-driven.
- Do not put Unity references in save data or the Domain assembly.

<!-- unity-onboarding:generated:end -->
