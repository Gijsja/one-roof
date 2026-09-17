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

        private GUIStyle _toolButtonStyle;
        private GUIStyle _activeToolButtonStyle;
        private GUIStyle _demolishActiveButtonStyle;

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
                    OnBuildModeRequested();
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
                OnBuildModeRequested();
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

        public void OnBuildModeRequested()
        {
            if (Session.CurrentMode != InteractionMode.Build)
            {
                if (string.IsNullOrEmpty(Session.Projection().SelectedBuildTool))
                {
                    Session.SelectBuildTool("residential:apartment");
                }
                else
                {
                    Session.SwitchMode(InteractionMode.Build);
                }
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            var projection = CurrentProjection;

            // Draw Build Palette above mode bar when in Build mode
            if (projection.IsBuildMode)
            {
                DrawBuildPalette(projection);
            }

            var barRect = new Rect(20, Screen.height - 70, 520, 50);

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

        private void DrawBuildPalette(ModeShellProjection projection)
        {
            var paletteRect = new Rect(20, Screen.height - 162, 620, 90);
            GUILayout.BeginArea(paletteRect, GUI.skin.box);

            // Row 1: Zoning & Structure
            GUILayout.BeginHorizontal();
            DrawToolButton("Studio Apt\n$1.5k (6c)", "residential:apartment", projection.SelectedBuildTool == "residential:apartment");
            DrawToolButton("Office\n$2.8k (8c)", "commercial:office", projection.SelectedBuildTool == "commercial:office");
            DrawToolButton("Diner\n$3.5k (10c)", "commercial:diner", projection.SelectedBuildTool == "commercial:diner");
            DrawToolButton("Floor Slab\n$3.1k (31c)", "floor:slab", projection.SelectedBuildTool == "floor:slab");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            // Row 2: Transit & Demolition
            GUILayout.BeginHorizontal();
            DrawToolButton("Stairs\n$500 (2c)", "transit:stairwell", projection.SelectedBuildTool == "transit:stairwell");
            DrawToolButton("Shaft\n$1k/fl (2c)", "transit:elevator_shaft", projection.SelectedBuildTool == "transit:elevator_shaft");
            DrawToolButton("Elevator Car\n$2.5k (2c)", "transit:elevator_car", projection.SelectedBuildTool == "transit:elevator_car");
            DrawToolButton("Bulldoze\nReclaim 50%", "demolish:room", projection.SelectedBuildTool == "demolish:room", isDestructive: true);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawToolButton(string label, string toolId, bool isSelected, bool isDestructive = false)
        {
            var style = isSelected
                ? (isDestructive ? _demolishActiveButtonStyle : _activeToolButtonStyle)
                : _toolButtonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(38), GUILayout.Width(144)))
            {
                if (isSelected)
                {
                    Session.SelectBuildTool(null);
                }
                else
                {
                    Session.SelectBuildTool(toolId);
                }
            }
        }

        private void DrawModeButton(string label, InteractionMode mode, bool isActive)
        {
            var style = isActive ? _activeButtonStyle : _normalButtonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(36), GUILayout.Width(118)))
            {
                if (mode == InteractionMode.Build)
                {
                    OnBuildModeRequested();
                }
                else
                {
                    Session.SwitchMode(mode);
                }
            }
        }

        private void DrawContextOverlay(ModeShellProjection projection)
        {
            var contextY = projection.IsBuildMode ? Screen.height - 198 : Screen.height - 105;
            var contextWidth = projection.IsBuildMode ? 620 : 520;
            var contextRect = new Rect(20, contextY, contextWidth, 32);

            GUILayout.BeginArea(contextRect);
            GUILayout.BeginHorizontal();

            var statusText = $"MODE: {projection.CurrentMode.ToString().ToUpperInvariant()}";
            if (projection.IsBuildMode)
            {
                if (!string.IsNullOrEmpty(projection.SelectedBuildTool))
                {
                    statusText += $"  |  Tool: {projection.SelectedBuildTool}  (Left-click grid to place, Right-click to cancel)";
                }
                else
                {
                    statusText += "  |  Select a tool from the Build Palette below";
                }
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
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };

            _toolButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };

            _activeToolButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.95f, 0.65f) }
            };

            _demolishActiveButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.45f, 0.25f) }
            };
        }
    }
}
