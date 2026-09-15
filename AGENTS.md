# AGENTS.md — One Roof

## Mission

Build a vertical-city simulation in which the player shapes architecture, infrastructure, leases, and policy while autonomous residents form routines, relationships, businesses, neighborhoods, and factions.

The first proof is five floors and fifty residents. The player must be able to observe elevator congestion, understand it through an overlay, add capacity, and see measurable improvement.

## Required reading order

1. `Docs/01_GAME_VISION.md`
2. `Docs/02_ARCHITECTURE.md`
3. `Docs/03_DATA_CONTRACTS.md`
4. The task entry in `Planning/BACKLOG.md`
5. The latest relevant file in `Handoffs/Active/`

Read `Docs/04_UX_CONTRACT.md`, `Docs/05_ASSET_PIPELINE.md`, or `Docs/06_TEST_STRATEGY.md` when the task touches those areas.

## Non-negotiable product rules

- The player shapes systems; they do not issue commands to individual NPCs.
- Important failures must be explainable through world symptom → overlay → inspector cause → player response.
- Simulation truth lives outside GameObjects and MonoBehaviours.
- Visible NPCs are views of persistent simulation entities, not the entities themselves.
- Beta scope must not expand until the five-floor slice meets its exit criteria.
- Generative art never enters runtime content without validation and registry assignment.

## Technical boundaries

| Layer | May depend on | Must not depend on |
| --- | --- | --- |
| Domain | Plain C# and domain interfaces | UnityEngine, scenes, prefabs, UI |
| Application | Domain, ports, use cases | Concrete Unity views |
| Infrastructure | Domain interfaces, serialization, pathfinding adapters | UI presentation rules |
| Presentation | Application APIs, read-only projections | Mutable domain internals |
| Editor | Content schemas and validation APIs | Runtime-only scene state |

Use stable integer IDs across layers. Do not store GameObject, Transform, MonoBehaviour, or scene references in save-state records.

## Unity rules

- Target Unity 6 LTS and URP after the exact installed version is recorded in `ProjectSettings/ProjectVersion.txt`.
- Never guess package versions or hand-edit `Packages/manifest.json`; use Unity's Package Manager API.
- Do not hand-edit `.unity`, `.prefab`, `.asset`, or `.meta` YAML unless the task explicitly requires text serialization and validation.
- Commit every asset with its `.meta` file.
- Never commit `Library/`, `Temp/`, `Obj/`, `Logs/`, `Build/`, or `Builds/`.
- Prefer additive scenes and prefabs over a monolithic scene.
- New runtime systems require assembly definitions and tests.
- Avoid global mutable singletons. Bootstrap composition may own service lifetimes.

## Change protocol

Before editing:

1. Confirm the task ID and acceptance criteria.
2. List the files you expect to own in the handoff.
3. Check `Handoffs/Active/` for overlapping work.

During work:

- Keep the task narrow; record discovered work in `Planning/BACKLOG.md` instead of absorbing it.
- Preserve user changes and unrelated work.
- Add or update tests with behavior changes.
- Record architectural choices in `Docs/07_DECISION_LOG.md`.

Before handoff:

1. Run the validation relevant to the task.
2. Record exact commands and results.
3. List changed files, known risks, and the next safe action.
4. Do not claim success if Unity compilation or tests were not run; say `NOT RUN` and why.
5. Create `Handoffs/Active/<TASK-ID>_<short-name>.md` from the template.

## Definition of done

- Acceptance criteria are demonstrated by tests, profiler evidence, or a reproducible Editor check.
- Unity compiles without new warnings.
- Relevant Edit Mode and Play Mode tests pass.
- Save data remains versioned and migratable.
- Domain code can run in tests without loading a scene.
- No generated folders or secrets are committed.
- Documentation and handoff accurately reflect the result.

