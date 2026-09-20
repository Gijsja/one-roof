# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `/home/geisha/Vibecode/UnityAI/one-roof`
- Last analyzed: 2026-09-20
- Last analyzed commit: `a06d5de023f507bd9e704bf6c06c5c483f42267d`
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
| Live tooling | `com.unity.pipeline` 0.7.0-exp.1 is present; its current server is unreachable | Confirmed | manifest, `unity pipeline list` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/OneRoof/Runtime/Domain` | Pure deterministic simulation records and aggregates | Confirmed | `OneRoof.Domain.asmdef`, `Docs/02_ARCHITECTURE.md` |
| `Assets/OneRoof/Runtime/Application` | Commands, projections, inspectors, overlays, simulation session | Confirmed | assembly and representative sources |
| `Assets/OneRoof/Runtime/Presentation` | Cutaway world, resident, overlay, and camera views | Confirmed | assembly and presenters |
| `Assets/OneRoof/Runtime/UI` | Mode shell and inspector/prediction views | Confirmed | assembly |
| `Assets/OneRoof/Runtime/Content` | Immutable authored content contracts | Confirmed | assembly and `NpcContentRegistry.cs` |
| `Assets/OneRoof/Editor` | Tower and AssetLab scene authoring/validation | Confirmed | `TowerSceneBuilder.cs`, `AssetLabValidator.cs` |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `OneRoof.Domain` | Simulation state | none | `noEngineReferences: true` |
| `OneRoof.Application` | Commands/projections/use cases | Domain | Presentation reads its immutable projections |
| `OneRoof.Content` | Content records | Domain | No mutable campaign state |
| `OneRoof.Presentation` | Unity views and world interaction | Domain, Application, Content, UI, Input System | Owns view-only state |
| `OneRoof.UI` | Mode/inspector UI | Application, Domain | Dispatches commands rather than mutating state |

## Scenes And Startup Flow

- Enabled build scenes: `Assets/Scenes/Tower.unity`, then `Assets/Scenes/Testbed_Transit.unity`.
- `Tower` is the likely interactive startup scene; `TowerSceneBuilder` creates a `Tower World` object with `TowerPlayableController`.
- `AssetLab.unity` is an editor-only validation preview and is not in Build Settings.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Domain/presentation boundary | Pure Domain state is projected into Unity presenters by stable IDs | Confirmed | `Docs/02_ARCHITECTURE.md`, assemblies |
| Composition root | `TowerPlayableController` coordinates four deep presenters and UI adapters | Confirmed | `TowerPlayableController.cs` |
| Fixed simulation tick | Session advances separately from frame rendering | Confirmed | `TowerPlayableController.cs`, architecture doc |
| Save state | Aggregate-owned serialization inside one versioned envelope | Confirmed | architecture doc and save tests |

## Coding Conventions

- Namespace style: `OneRoof.<layer>.<feature>`.
- Runtime Unity code uses sealed presenters/components, explicit `Initialize`, private fields, and XML intent comments for non-obvious classes.
- Domain code avoids Unity references; presentation consumes snapshots/projections.
- Tests use NUnit fixtures under matching layer-specific EditMode assemblies.

## Testing And Validation

- EditMode tests are split by runtime assembly; PlayMode golden acceptance tests are present.
- The previous OR-604 handoff records 318/318 EditMode tests passing on 2026-09-19.
- CI compile/test workflow remains blocked on the GitHub `UNITY_LICENSE` secret (`OR-005`).

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| `unity.connection.status` | unavailable | Pipeline Editor server is unreachable |
| `unity.console.read` | unavailable | Pipeline Editor server is unreachable |
| `unity.scene.inspect` | unavailable | Pipeline Editor server is unreachable |
| `unity.buildsettings.read` | available from serialized settings | `ProjectSettings/EditorBuildSettings.asset` |
| `unity.asset.search` | available from repository | workspace filesystem |
| `unity.package.read` | available from repository | `Packages/manifest.json` |
| `unity.tests.run` | unavailable currently | Pipeline Editor server is unreachable |

## Important Constraints

- Player actions affect systems, not individual residents.
- The cause chain must remain symptom → overlay → inspector → systems-level response.
- Steady-state simulation ticks must not allocate; presentation must remain projection-driven.
- Do not put Unity references in save data or the Domain assembly.

## Unknowns And Confidence

- Live Editor console, scene hierarchy, and test execution are currently unknown because the Editor Pipeline server is not reachable, despite the process being present and not in Safe Mode.
- No first-party networking usage was found in the inspected assemblies; the installed multiplayer-center package alone is not treated as a multiplayer implementation.

## Source Files Inspected

- `AGENTS.md`, `Planning/BACKLOG.md`, `Docs/01_GAME_VISION.md`, `Docs/02_ARCHITECTURE.md`, `Docs/03_DATA_CONTRACTS.md`, `Docs/04_UX_CONTRACT.md`, `Docs/09_CAUSE_CHAIN_INSPECTOR.md`
- `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/EditorBuildSettings.asset`
- `TowerPlayableController.cs`, `TowerStructurePresenter.cs`, `RoomPresenter.cs`, `ElevatorBankPresenter.cs`, `TowerSimulationSession.cs`, `TowerProjection.cs`

<!-- unity-onboarding:generated:end -->
