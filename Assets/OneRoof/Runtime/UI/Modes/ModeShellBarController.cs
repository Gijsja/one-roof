using System;
using OneRoof.Application.Modes;
using OneRoof.Application.Modes.Commands;
using UnityEngine;

namespace OneRoof.UI.Modes
{
    /// <summary>
    /// Player-facing navigation bar controlling Build, Inspect, Data, and Manage modes.
    /// Captures hotkeys and button presses, dispatching commands to <see cref="ModeShellSession"/>
    /// without directly mutating simulation state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ModeShellBarController : MonoBehaviour
    {
        private ModeShellSession _session;
        private GUIStyle _activeButtonStyle;
        private GUIStyle _normalButtonStyle;
        private GUIStyle _bannerStyle;
        private GUIStyle _contextStyle;

        public ModeShellSession Session
        {
            get => _session ?? (_session = new ModeShellSession());
            set => _session = value;
        }

        public InteractionMode ActiveMode => Session.CurrentMode;

        public ModeShellProjection CurrentProjection => Session.Projection();

        private void Update()
        {
            HandleKeyboardShortcuts();
        }

        public void HandleKeyboardShortcuts()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.bKey.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame)
                {
                    Session.SwitchMode(InteractionMode.Build);
                }
                else if (keyboard.iKey.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame)
                {
                    Session.SwitchMode(InteractionMode.Inspect);
                }
                else if (keyboard.dKey.wasPressedThisFrame || keyboard.digit3Key.wasPressedThisFrame)
                {
                    Session.SwitchMode(InteractionMode.Data);
                }
                else if (keyboard.mKey.wasPressedThisFrame || keyboard.digit4Key.wasPressedThisFrame)
                {
                    Session.SwitchMode(InteractionMode.Manage);
                }
                else if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    Session.CancelOrEscape();
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha1))
            {
                Session.SwitchMode(InteractionMode.Build);
            }
            else if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Alpha2))
            {
                Session.SwitchMode(InteractionMode.Inspect);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Alpha3))
            {
                Session.SwitchMode(InteractionMode.Data);
            }
            else if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Alpha4))
            {
                Session.SwitchMode(InteractionMode.Manage);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Session.CancelOrEscape();
            }
#endif
        }

        private void OnGUI()
        {
            EnsureStyles();

            var projection = CurrentProjection;
            var barRect = new Rect(20, Screen.height - 70, 480, 50);

            GUILayout.BeginArea(barRect, GUI.skin.box);
            GUILayout.BeginHorizontal();

            DrawModeButton("Inspect [I]", InteractionMode.Inspect, projection.IsInspectMode);
            DrawModeButton("Build [B]", InteractionMode.Build, projection.IsBuildMode);
            DrawModeButton("Data [D]", InteractionMode.Data, projection.IsDataMode);
            DrawModeButton("Manage [M]", InteractionMode.Manage, projection.IsManageMode);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            DrawContextOverlay(projection);
        }

        private void DrawModeButton(string label, InteractionMode mode, bool isActive)
        {
            var style = isActive ? _activeButtonStyle : _normalButtonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(36), GUILayout.Width(110)))
            {
                Session.SwitchMode(mode);
            }
        }

        private void DrawContextOverlay(ModeShellProjection projection)
        {
            var contextRect = new Rect(20, Screen.height - 110, 480, 32);
            GUILayout.BeginArea(contextRect);
            GUILayout.BeginHorizontal();

            var statusText = $"MODE: {projection.CurrentMode.ToString().ToUpperInvariant()}";
            if (projection.IsBuildMode && !string.IsNullOrEmpty(projection.SelectedBuildTool))
            {
                statusText += $"  |  Tool: {projection.SelectedBuildTool}";
            }
            else if (projection.IsDataMode && !string.IsNullOrEmpty(projection.ActiveOverlayId))
            {
                statusText += $"  |  Overlay: {projection.ActiveOverlayId}";
            }
            else if (projection.IsInspectMode && projection.SelectedEntityId.HasValue)
            {
                statusText += $"  |  Entity: #{projection.SelectedEntityId.Value}";
            }

            GUILayout.Label(statusText, _contextStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_activeButtonStyle != null)
            {
                return;
            }

            _normalButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal
            };

            _activeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.9f, 0.6f) }
            };

            _bannerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _contextStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
        }
    }
}
