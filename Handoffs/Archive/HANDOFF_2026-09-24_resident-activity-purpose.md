# Resident activity purpose and duration

## Delivered

- Residents carry a deterministic, saved purpose interval. Standard schedules give outside work 480 ticks after actual arrival, diner meals 30 ticks, and home activities 60 ticks. The compressed five-floor acceptance fixture uses 5-tick room episodes; outside work still holds for 480 ticks.
- Save data preserves actual schedule blocks and active purpose start/end ticks. An Outside workplace reloads without trying to construct a room ID from its empty room field.
- Transit projections expose the resident's current purpose or active trip destination. Captions and inspector details explain eating, work, reading, learning, sitting, chilling, and travel intent. Outside residents have no visible or selectable view; captions are smaller at gameplay zoom.
- Domain remains pure C# with `noEngineReferences: true`. No scene or prefab setup was added.

## Validation

- Isolated worktree: `/home/geisha/.codex/worktrees/resident-activity-validation/one-roof`.
- Unity 6000.3.24f1 headless baseline compile at original HEAD: exit 0, `/tmp/one-roof-activity-baseline-compile.log`.
- Integrated headless compile after correcting a new NUnit assertion: exit 0, `/tmp/one-roof-activity-integrated-compile-2.log`.
- Focused `ResidentPurposeTests`: exit 0, 3/3 passed, `/tmp/one-roof-activity-purpose-tests-2.xml`.
- Full EditMode: exit 0, 501/501 passed, `/tmp/one-roof-activity-editmode-2.xml`.
- Full PlayMode: exit 0, 7/7 passed, `/tmp/one-roof-activity-playmode.xml`.
- Scoped `git diff --check` for this feature: exit 0. The shared checkout has unrelated trailing whitespace in concurrently edited shader and material files.

## Scope and next action

- The validation worktree copied all currently changed `Assets/OneRoof` files from the shared checkout, including concurrent economy and presentation changes; the suite result is for that integrated snapshot. The feature code was included in shared commit `6204ea0`; the compressed-fixture timing correction and this handoff follow in a scoped commit.
- Headless tests do not establish visual readability at the screenshot's exact camera zoom. Inspect a dense occupied floor in Play Mode when a visible editor session is available.
