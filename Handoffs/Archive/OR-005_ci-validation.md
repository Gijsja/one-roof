# OR-005 — CI validation workflow

**Status:** BLOCKED
**Owner:** Codex session
**Started:** 2026-09-15
**Updated:** 2026-09-15

## Objective

Add CI compilation plus Edit and Play Mode test coverage for a clean checkout.

## Acceptance criteria

- [ ] A GitHub-hosted clean checkout completes compile, Edit Mode, and Play Mode validation.

## Scope and ownership

Expected files/directories:

- `.github/workflows/unity-validation.yml`
- `Assets/OneRoof/Tests/PlayMode/`
- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`
- `Handoffs/Active/OR-005_ci-validation.md`

Do not modify:

- User-authored scene and project-setting changes.

## State at handoff

The GitHub Actions workflow is ready. It runs GameCI v4 Edit Mode and Play Mode jobs independently, uploads their reports, then compiles a `StandaloneLinux64` player. A Play Mode smoke test ensures that CI has a Play Mode test to execute.

GitHub has no repository secret named `UNITY_LICENSE`, so the remote workflow cannot yet activate Unity and demonstrate its clean-checkout acceptance criterion.

## Changes made

- `.github/workflows/unity-validation.yml` — Added cache-backed test matrix, Linux compile, and artifacts.
- `Assets/OneRoof/Tests/PlayMode/PlayModeTestRunnerSmokeTests.cs` — Added a one-frame Play Mode test runner smoke test.
- `Docs/07_DECISION_LOG.md` — Added ADR-011.
- `Planning/BACKLOG.md` — Marked OR-005 blocked on the required GitHub secret.

## Decisions

- Use `game-ci/unity-test-runner@v4` for separate Edit and Play Mode results and `game-ci/unity-builder@v4` for the Linux compile.
- Keep the Unity license in GitHub Actions secrets; never commit it or pass it in prompts.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Edit Mode | `unity test /home/geisha/Vibecode/UnityAI/one-roof --editor-version 6000.3.24f1 --mode EditMode --output /tmp/one-roof-or005-editmode.xml --timeout 180 --format json` | PASS — 2 tests passed, 0 failed |
| Play Mode | `Unity -batchmode -nographics -quit -projectPath /home/geisha/Vibecode/UnityAI/one-roof -runTests -testPlatform PlayMode -testResults /tmp/one-roof-or005-playmode.xml` | PASS — 1 test passed, 0 failed |
| Fresh GitHub checkout | GitHub Actions workflow | BLOCKED — `UNITY_LICENSE` is not configured as a repository secret |

## Known risks or failures

- The Unity CLI Pipeline registry retains a dead Editor PID after a batch test run. The direct headless Play Mode runner was used after confirming the PID and project lock no longer existed.

## Next safe action

Add the Unity activation file as the repository's `UNITY_LICENSE` Actions secret, then run the `Unity validation` workflow and record the clean-checkout result.

## References

- Backlog: OR-005
- Decisions: ADR-011
- Evidence: `.github/workflows/unity-validation.yml`, `/tmp/one-roof-or005-editmode.xml`, `/tmp/one-roof-or005-playmode.xml`
