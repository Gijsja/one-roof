using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Crisis;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace OneRoof.Tests.PlayMode
{
    public sealed class CaretakerCrisisPlayModeTests : OwnedPlayModeSceneFixture
    {
        [UnityTest]
        public IEnumerator GoldenTower_LobbyContactAndPracticeRestoreManagementAndCamera()
        {
            yield return RunPracticeAcceptance(captureDiagnostics: false);
        }

        [UnityTest, Explicit("Optional graphical captures and timing diagnostics."), Category("VisualCapture")]
        public IEnumerator GoldenTower_PracticeCapturesAndPerformanceDiagnostics()
        {
            yield return RunPracticeAcceptance(captureDiagnostics: true);
        }

        [UnityTest]
        public IEnumerator GoldenTower_NavigationWeatherAndModalInputsStayExclusive()
        {
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
            yield return LoadTestScene("Assets/Scenes/Tower_GoldStandard30.unity");
            yield return null;
            var playground = UnityEngine.Object.FindFirstObjectByType<GoldStandardPlayground>();
            var tower = playground.Tower;
            var presenter = tower.CaretakerPresenter;
            var camera = Camera.main;
            Assert.That(camera.GetComponent<TowerCameraController>(), Is.Not.Null);
            tower.SetPaused(true);
            playground.FrameStreet();
            tower.ModeSession.SwitchMode(InteractionMode.Inspect);
            var inputSettings = InputSystem.settings;
            var originalEditorInputBehavior = inputSettings.editorInputBehaviorInPlayMode;
            var originalBackgroundBehavior = inputSettings.backgroundBehavior;
            // Batch acceptance has no focused GameView; keep synthetic keyboard events in
            // the game update rather than routing them into the Editor's input state.
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var previousKeyboard = Keyboard.current;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                var weather = tower.WeatherOverride;
                var position = camera.transform.position;
                yield return HoldKey(keyboard, Key.W);
                Assert.That(camera.transform.position.y, Is.GreaterThan(position.y), "W must navigate the Tower.");
                Assert.That(tower.WeatherOverride, Is.EqualTo(weather), "Navigation must not override weather.");
                position = camera.transform.position;
                yield return HoldKey(keyboard, Key.F6);
                Assert.That(tower.WeatherOverride, Is.EqualTo(weather + 1), "F6 cycles weather once per press.");
                Assert.That(Vector3.Distance(position, camera.transform.position), Is.LessThan(.001f), "Weather must not pan the camera.");
                position = camera.transform.position;
                yield return HoldKey(keyboard, Key.D);
                Assert.That(camera.transform.position.x, Is.GreaterThan(position.x));
                Assert.That(tower.ModeSession.CurrentMode, Is.EqualTo(InteractionMode.Inspect), "D pans without selecting Data mode.");
                yield return HoldKey(keyboard, Key.Digit3);
                Assert.That(tower.ModeSession.CurrentMode, Is.EqualTo(InteractionMode.Data), "Data remains available through 3.");

                presenter.ToggleContact();
                yield return null;
                Assert.That(tower.CaretakerInputBlocked, Is.True);
                position = camera.transform.position;
                weather = tower.WeatherOverride;
                yield return HoldKey(keyboard, Key.D);
                yield return HoldKey(keyboard, Key.F6);
                yield return HoldKey(keyboard, Key.Digit2);
                yield return HoldKey(keyboard, Key.Digit6);
                yield return HoldKey(keyboard, Key.Space);
                Assert.That(Vector3.Distance(position, camera.transform.position), Is.LessThan(.001f));
                Assert.That(tower.WeatherOverride, Is.EqualTo(weather));
                Assert.That(tower.ModeSession.CurrentMode, Is.EqualTo(InteractionMode.Data));
                Assert.That(tower.IsPaused, Is.True, "Modal input must not resume management time.");

                presenter.ToggleContact();
                yield return null;
                Assert.That(tower.CaretakerInputBlocked, Is.False);
                position = camera.transform.position;
                yield return HoldKey(keyboard, Key.D);
                Assert.That(camera.transform.position.x, Is.GreaterThan(position.x), "Closing contact restores navigation.");
                yield return HoldKey(keyboard, Key.F6);
                Assert.That(tower.WeatherOverride, Is.EqualTo(weather + 1), "Closing contact restores weather controls.");
                yield return HoldKey(keyboard, Key.Digit2);
                Assert.That(tower.ModeSession.CurrentMode, Is.EqualTo(InteractionMode.Inspect));
                var managementPosition = camera.transform.position;
                var managementSize = camera.orthographicSize;
                var managementTick = tower.SimulationSession.CurrentTick;
                var timeScale = Time.timeScale;
                weather = tower.WeatherOverride;
                Assert.That(presenter.RequestAction(CaretakerCrisisAction.Practice), Is.True);
                yield return null;
                Assert.That(presenter.IsArcadeActive, Is.True);
                Assert.That(tower.CaretakerInputBlocked, Is.True);
                var caretakerX = tower.SimulationSession.CaretakerProjection().X;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                position = camera.transform.position;
                // Isolate the navigation consumer from intentional arcade camera following.
                camera.GetComponent<TowerCameraController>().SendMessage("Update");
                Assert.That(Vector3.Distance(position, camera.transform.position), Is.LessThan(.001f), "D cannot pan management camera during practice.");
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(tower.SimulationSession.CaretakerProjection().X, Is.GreaterThan(caretakerX), "D still moves the Caretaker during practice.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                yield return HoldKey(keyboard, Key.F6);
                yield return HoldKey(keyboard, Key.R);
                yield return HoldKey(keyboard, Key.Space);
                yield return HoldKey(keyboard, Key.Digit6);
                Assert.That(presenter.IsArcadeActive, Is.True, "Management reset cannot replace active practice.");
                Assert.That(tower.SimulationSession.CurrentTick, Is.EqualTo(managementTick));
                Assert.That(tower.WeatherOverride, Is.EqualTo(weather));
                Assert.That(tower.IsPaused, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(timeScale));
                Assert.That(camera.orthographicSize, Is.LessThan(3f), "Street framing cannot replace the arcade view.");
                Assert.That(presenter.RequestAction(CaretakerCrisisAction.Exit), Is.True);
                yield return null;
                Assert.That(presenter.ContactOpen, Is.True);
                presenter.ToggleContact();
                yield return null;
                Assert.That(tower.CaretakerInputBlocked, Is.False);
                Assert.That(tower.IsPaused, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(timeScale));
                Assert.That(tower.SimulationSession.CurrentTick, Is.EqualTo(managementTick));
                Assert.That(Vector3.Distance(camera.transform.position, managementPosition), Is.LessThan(.001f));
                Assert.That(camera.orthographicSize, Is.EqualTo(managementSize).Within(.001f));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
                inputSettings.editorInputBehaviorInPlayMode = originalEditorInputBehavior;
                inputSettings.backgroundBehavior = originalBackgroundBehavior;
            }
#else
            Assert.Ignore("Input acceptance requires the Editor and Input System.");
            yield break;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static IEnumerator HoldKey(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            // Hold across frame boundaries so the real Update consumers process the event.
            for (var frame = 0; frame < 3; frame++) yield return null;
            Assert.That(Keyboard.current, Is.SameAs(keyboard), "Synthetic keyboard must own the current game input state.");
            Assert.That(keyboard[key].isPressed, Is.True, "Queued input must reach the game before behavior is asserted.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }
#endif

        private IEnumerator RunPracticeAcceptance(bool captureDiagnostics)
        {
#if UNITY_EDITOR
            yield return LoadTestScene("Assets/Scenes/Tower_GoldStandard30.unity");
            yield return null;
            var playground = UnityEngine.Object.FindFirstObjectByType<GoldStandardPlayground>();
            var tower = playground.Tower;
            var presenter = tower.CaretakerPresenter;
            var camera = Camera.main;
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.LobbyCaretaker, Is.Not.Null);
            Assert.That(presenter.LobbyCaretaker.gameObject.activeInHierarchy, Is.True);
            tower.SetPaused(true);
            playground.FrameStreet();
            var gridWasEnabled = tower.GridPlacement.enabled;
            var inspectWasEnabled = tower.InspectSelection.enabled;
            presenter.ToggleContact();
            yield return null;
            Assert.That(tower.CaretakerInputBlocked, Is.True);
            Assert.That(tower.GridPlacement.enabled, Is.False);
            Assert.That(tower.InspectSelection.enabled, Is.False);
            var mode = tower.ModeSession.CurrentMode;
            tower.ModeSession.SwitchMode(mode == InteractionMode.Build ? InteractionMode.Inspect : InteractionMode.Build);
            Assert.That(tower.ModeSession.CurrentMode, Is.EqualTo(mode), "Modal contact must block mode shortcuts.");
            var output = "/tmp/one-roof-caretaker-validation";
            if (captureDiagnostics) Directory.CreateDirectory(output);
            if (captureDiagnostics && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                Capture(camera, Path.Combine(output, "lobby-contact-world.png"));
                yield return null;
                yield return CaptureGameView(Path.Combine(output, "lobby-contact-full.png"));
            }
            presenter.ToggleContact();
            yield return null;
            Assert.That(tower.CaretakerInputBlocked, Is.False);
            Assert.That(tower.GridPlacement.enabled, Is.EqualTo(gridWasEnabled));
            Assert.That(tower.InspectSelection.enabled, Is.EqualTo(inspectWasEnabled));
            tower.InspectSelection.enabled = false;
            presenter.ToggleContact();
            presenter.ToggleContact();
            Assert.That(tower.InspectSelection.enabled, Is.False, "Closing repeated contact must preserve a previously disabled controller.");
            tower.InspectSelection.enabled = inspectWasEnabled;
            var originalPosition = camera.transform.position;
            var originalSize = camera.orthographicSize;
            Assert.That(tower.SimulationSession.ExecuteCaretaker(CaretakerCrisisAction.Practice).Accepted, Is.True);
            presenter.RefreshPresentation();
            yield return null;
            Assert.That(presenter.IsArcadeActive, Is.True);
            Assert.That(tower.ResidentPresenter.ArcadeSuppressed, Is.True);
            Assert.That(tower.ResidentPresenter.ViewPool.ActiveCount, Is.Zero, "Autonomous resident rigs and bubbles must not overlap the arcade combat views.");
            Assert.That(tower.CaretakerInputBlocked, Is.True);
            Assert.That(tower.SimulationSession.FloorCount, Is.EqualTo(30));
            Assert.That(tower.SimulationSession.ResidentCount, Is.EqualTo(300));
            var tick = tower.SimulationSession.CurrentTick;
            // Exercise the real lobby-to-stairs approach using centralized fixed combat steps.
            var data = tower.SimulationSession.ExportSaveData().caretakerCrisis;
            var exitX = data.route[0].exitX;
            for (var step = 0; step < 4000 && !tower.SimulationSession.CaretakerProjection().FloorCleared; step++)
            {
                var state = tower.SimulationSession.CaretakerProjection();
                var nearestX = state.X;
                var nearestDistance = int.MaxValue;
                foreach (var opponent in state.Opponents)
                    if (opponent.Health > 0 && Math.Abs(opponent.X - state.X) < nearestDistance)
                    { nearestX = opponent.X; nearestDistance = Math.Abs(opponent.X - state.X); }
                var direction = nearestX >= state.X ? 1 : -1;
                var move = nearestDistance > 1200 || direction != state.Facing ? direction : 0;
                Assert.That(tower.SimulationSession.ExecuteCaretaker(CaretakerCrisisAction.Step,
                    new CaretakerCombatInput(move, kick: true, jump: true)).Accepted, Is.True, "Practice must remain playable while clearing the lobby approach.");
            }
            Assert.That(tower.SimulationSession.CaretakerProjection().FloorCleared, Is.True);
            data = tower.SimulationSession.ExportSaveData().caretakerCrisis;
            for (var step = 0; step < 1000 && Math.Abs(data.x - exitX) > 900; step++)
            {
                tower.SimulationSession.ExecuteCaretaker(CaretakerCrisisAction.Step, new CaretakerCombatInput(data.x < exitX ? 1 : -1));
                data = tower.SimulationSession.ExportSaveData().caretakerCrisis;
            }
            Assert.That(tower.SimulationSession.ExecuteCaretaker(CaretakerCrisisAction.Advance).Accepted, Is.True);
            presenter.RefreshPresentation();
            yield return null;
            Assert.That(tower.SimulationSession.CurrentTick, Is.EqualTo(tick));
            Assert.That(tower.SimulationSession.ExportSaveData().caretakerCrisis.routeIndex, Is.GreaterThan(0));
            if (captureDiagnostics && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                for (var step = 0; step < 240; step++)
                {
                    var state = tower.SimulationSession.CaretakerProjection();
                    var nearestX = state.X;
                    var distance = int.MaxValue;
                    foreach (var opponent in state.Opponents)
                        if (opponent.Health > 0 && Math.Abs(opponent.X - state.X) < distance)
                        { nearestX = opponent.X; distance = Math.Abs(opponent.X - state.X); }
                    if (distance <= 1100) break;
                    tower.SimulationSession.ExecuteCaretaker(CaretakerCrisisAction.Step,
                        new CaretakerCombatInput(nearestX >= state.X ? 1 : -1, jump: true));
                }
                for (var step = 0; step < 10; step++)
                    tower.SimulationSession.ExecuteCaretaker(CaretakerCrisisAction.Step, new CaretakerCombatInput(0, kick: true));
                for (var settleFrame = 0; settleFrame < 20; settleFrame++) yield return null;
                Capture(camera, Path.Combine(output, "floor-combat-world.png"));
                yield return null;
                yield return CaptureGameView(Path.Combine(output, "floor-combat-full.png"));
                var drawCalls = 0;
                var elapsed = 0f;
                var renderTimes = new List<double>();
                var performanceTarget = new RenderTexture(1280, 720, 24);
                var previousTarget = camera.targetTexture;
                if (UnityEngine.Application.isBatchMode) camera.targetTexture = performanceTarget;
                try
                {
                    var renderTimer = new System.Diagnostics.Stopwatch();
                    for (var frame = 0; frame < 90; frame++)
                    {
                        renderTimer.Restart();
                        if (UnityEngine.Application.isBatchMode) camera.Render();
                        renderTimer.Stop();
                        yield return null;
                        if (frame < 30) continue;
                        elapsed += Time.unscaledDeltaTime;
                        drawCalls += UnityEditor.UnityStats.drawCalls;
                        renderTimes.Add(UnityEngine.Application.isBatchMode ? renderTimer.Elapsed.TotalMilliseconds : Time.unscaledDeltaTime * 1000);
                    }
                }
                finally { camera.targetTexture = previousTarget; UnityEngine.Object.Destroy(performanceTarget); }
                renderTimes.Sort();
                var performanceFile = UnityEngine.Application.isBatchMode ? "presentation-performance-batch.txt" : "presentation-performance-editor.txt";
                File.WriteAllText(Path.Combine(output, performanceFile),
                    "Device=" + SystemInfo.graphicsDeviceName + "\nRenderedResolution=" + (UnityEngine.Application.isBatchMode ? "1280x720" : Screen.width + "x" + Screen.height) +
                    "\nRenderMode=" + (UnityEngine.Application.isBatchMode ? "Explicit Camera.Render" : "Native GameView with HUD") + "\nFPS=" + (60f / elapsed) + "\nAverageDrawCalls=" + (drawCalls / 60f) +
                    (UnityEngine.Application.isBatchMode ? "\nCameraRenderP95ms=" : "\nFrameP95ms=") + renderTimes[(int)((renderTimes.Count - 1) * .95)] +
                    (UnityEngine.Application.isBatchMode ? "\nGUI=Not captured by explicit Camera.Render" : "\nCameraSubmitTiming=Unavailable; measured native frame timing"));
            }
            if (captureDiagnostics)
            {
                var samples = new List<double>();
                var timer = new System.Diagnostics.Stopwatch();
                for (var step = 0; step < 120; step++)
                {
                    timer.Restart();
                    var result = tower.SimulationSession.ExecuteCaretaker(CaretakerCrisisAction.Step, new CaretakerCombatInput(0, kick: true, jump: true));
                    timer.Stop();
                    if (!result.Accepted) break;
                    samples.Add(timer.Elapsed.TotalMilliseconds);
                }
                samples.Sort();
                if (samples.Count > 0)
                    File.WriteAllText(Path.Combine(output, "combat-performance.txt"), "Samples=" + samples.Count + "\nP95ms=" + samples[(int)((samples.Count - 1) * .95)]);
            }
            Assert.That(tower.SimulationSession.ExecuteCaretaker(CaretakerCrisisAction.Exit).Accepted, Is.True);
            presenter.RefreshPresentation();
            yield return null;
            Assert.That(presenter.ContactOpen, Is.True);
            presenter.ToggleContact();
            yield return null;
            Assert.That(tower.CaretakerInputBlocked, Is.False);
            Assert.That(presenter.LobbyCaretaker.gameObject.activeInHierarchy, Is.True);
            Assert.That(tower.ResidentPresenter.ArcadeSuppressed, Is.False);
            Assert.That(tower.SimulationSession.ResidentCount, Is.EqualTo(300));
            Assert.That(tower.GridPlacement.enabled, Is.EqualTo(gridWasEnabled));
            Assert.That(tower.InspectSelection.enabled, Is.EqualTo(inspectWasEnabled));
            Assert.That(Vector3.Distance(camera.transform.position, originalPosition), Is.LessThan(.001f));
            Assert.That(camera.orthographicSize, Is.EqualTo(originalSize).Within(.001f));
            Assert.That(tower.IsPaused, Is.True, "An already-paused management simulation must remain paused.");
            LogAssert.NoUnexpectedReceived();
#else
            Assert.Ignore("The authored scene acceptance fixture runs in the Editor.");
            yield break;
#endif
        }

        private static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(image);
            }
        }

        private static IEnumerator CaptureGameView(string path)
        {
            if (UnityEngine.Application.isBatchMode) yield break;
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            if (image == null) yield break;
            try { File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { UnityEngine.Object.Destroy(image); }
        }
    }
}
