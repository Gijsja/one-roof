using System;
using OneRoof.Application.Modes;
using OneRoof.Application.Modes.Commands;
using OneRoof.UI;
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
        private Vector2 _paletteScroll;

        public ModeShellSession Session
        {
            get => _session ?? (_session = new ModeShellSession());
            set => _session = value;
        }

        public InteractionMode ActiveMode => Session.CurrentMode;

        public ModeShellProjection CurrentProjection => Session.Projection();

        // IMGUI uses top-origin coordinates. Placement uses these same bounds to
        // exclude visible controls before mapping a pointer to tower cells.
        public static Rect BuildPaletteRect(int screenHeight)
        {
            var height = Mathf.Clamp(screenHeight - 420f, 180f, 300f);
            return new Rect(16, screenHeight - 116f - height, 620, height);
        }

        public static Rect ModeBarRect(int screenHeight) => new Rect(16, screenHeight - 68, 620, 52);

        public static Rect ContextRect(int screenHeight, bool isBuildMode) =>
            new Rect(16, screenHeight - 108, 620, 32);

        public static bool IsPointerOverControls(Vector2 screenPosition, int screenHeight, ModeShellProjection projection)
        {
            var imguiPosition = new Vector2(screenPosition.x, screenHeight - screenPosition.y);
            if (ModeBarRect(screenHeight).Contains(imguiPosition) ||
                ContextRect(screenHeight, projection.IsBuildMode).Contains(imguiPosition))
                return true;
            if (projection.IsBuildMode && string.IsNullOrEmpty(projection.SelectedBuildTool) &&
                BuildPaletteRect(screenHeight).Contains(imguiPosition))
                return true;
            return projection.IsDataMode &&
                   new Rect(16, screenHeight - 282, 620, 166).Contains(imguiPosition);
        }

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
            if (Session.CurrentMode == InteractionMode.Build)
            {
                if (string.IsNullOrEmpty(Session.Projection().SelectedBuildTool))
                {
                    // Palette is already open; a second press exits Build mode so
                    // the button toggles instead of trapping the player in Build.
                    Session.SwitchMode(InteractionMode.Inspect);
                    return;
                }

                // Build is also the palette toggle. Reopen it without cancelling the
                // mode so a player can choose a different tool after a placement.
                Session.SelectBuildTool(null);
                return;
            }

            // Do not preselect a room: the player must first see and choose from the
            // complete build list. Once chosen, the palette closes to free the tower
            // viewport for placement.
            Session.SwitchMode(InteractionMode.Build);
        }

        private void OnGUI()
        {
            EnsureStyles();

            var projection = CurrentProjection;

            // Keep the world clickable after selecting a tool. The palette is a
            // chooser, not a permanent overlay; right-click or Build reopens it.
            if (projection.IsBuildMode && string.IsNullOrEmpty(projection.SelectedBuildTool))
            {
                DrawBuildPalette(projection);
            }
            if (projection.IsDataMode) DrawDataPalette(projection);

            var barRect = ModeBarRect(Screen.height);

            GUILayout.BeginArea(barRect, StewardTheme.Panel);
            GUILayout.BeginHorizontal();

            DrawModeButton("INSPECT  2", InteractionMode.Inspect, projection.IsInspectMode);
            DrawModeButton("BUILD  1", InteractionMode.Build, projection.IsBuildMode);
            DrawModeButton("DATA  3", InteractionMode.Data, projection.IsDataMode);
            DrawModeButton("MANAGE  4", InteractionMode.Manage, projection.IsManageMode);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            DrawContextOverlay(projection);
        }

        private void DrawBuildPalette(ModeShellProjection projection)
        {
            var paletteRect = BuildPaletteRect(Screen.height);
            GUILayout.BeginArea(paletteRect, StewardTheme.Panel);
            GUILayout.Label("BUILD THE TOWER", StewardTheme.Label(15, StewardTheme.Text, true));
            GUILayout.Label("Choose a system, then place it in the cutaway.", StewardTheme.Label(11, StewardTheme.Muted));
            _paletteScroll = GUILayout.BeginScrollView(_paletteScroll, false, true,
                GUILayout.Height(paletteRect.height - 70f));
            GUILayout.Label("SPACE & USE", StewardTheme.Label(10, StewardTheme.Mint, true));

            // Row 1: Zoning & Structure
            GUILayout.BeginHorizontal();
            DrawToolButton("Studio Apt\n$1.5k (6c)", "residential:apartment", projection.SelectedBuildTool == "residential:apartment");
            DrawToolButton("Office\n$2.8k (8c)", "commercial:office", projection.SelectedBuildTool == "commercial:office");
            DrawToolButton("Diner\n$3.5k (10c)", "commercial:diner", projection.SelectedBuildTool == "commercial:diner");
            DrawToolButton("New Floor\n$3.1k (31c)", "floor:slab", projection.SelectedBuildTool == "floor:slab");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("SERVICES", StewardTheme.Label(10, StewardTheme.Mint, true));

            // Row 2: Services
            GUILayout.BeginHorizontal();
            DrawToolButton("Ground Expand\n$600 (6c)", "floor:ground_expansion", projection.SelectedBuildTool == "floor:ground_expansion");
            DrawToolButton("Retail Shop\n$2.1k (6c)", "commercial:retail", projection.SelectedBuildTool == "commercial:retail");
            DrawToolButton("Clinic\n$1.2k (8c)", "service:clinic", projection.SelectedBuildTool == "service:clinic");
            DrawToolButton("Workshop\n$1.2k (8c)", "service:maintenance_workshop", projection.SelectedBuildTool == "service:maintenance_workshop");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("POWER", StewardTheme.Label(10, StewardTheme.Mint, true));

            // Row 3: Physical utilities
            GUILayout.BeginHorizontal();
            DrawToolButton("Security\n$900 (6c)", "service:security_station", projection.SelectedBuildTool == "service:security_station");
            DrawToolButton("Substation\n$1.6k (4c)", "utility:electrical_substation", projection.SelectedBuildTool == "utility:electrical_substation");
            DrawToolButton("Riser Duct\n$800 (2c)", "utility:electrical_riser", projection.SelectedBuildTool == "utility:electrical_riser");
            DrawToolButton("Transformer\n$800 (2c)", "utility:floor_transformer", projection.SelectedBuildTool == "utility:floor_transformer");
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.Label("WATER & WASTE", StewardTheme.Label(10, StewardTheme.Mint, true));

            // Row 4: Water & waste utilities
            GUILayout.BeginHorizontal();
            DrawToolButton("Water Pump\n$1.6k (4c)", "utility:water_pump", projection.SelectedBuildTool == "utility:water_pump");
            DrawToolButton("Water Riser\n$800 (2c)", "utility:water_riser", projection.SelectedBuildTool == "utility:water_riser");
            DrawToolButton("Booster\n$800 (2c)", "utility:water_booster", projection.SelectedBuildTool == "utility:water_booster");
            DrawToolButton("Waste Chute\n$800 (2c)", "utility:waste_chute", projection.SelectedBuildTool == "utility:waste_chute");
            GUILayout.EndHorizontal();

            // Row 5: Ground collection
            GUILayout.BeginHorizontal();
            DrawToolButton("Waste Collection\n$1.6k (4c)", "utility:waste_collection", projection.SelectedBuildTool == "utility:waste_collection");
            GUILayout.EndHorizontal();

            GUILayout.Label("MOVEMENT & REMOVAL", StewardTheme.Label(10, StewardTheme.Mint, true));
            // Row 6: Transit & Demolition
            GUILayout.BeginHorizontal();
            DrawToolButton("Stairs\n$500 (2c)", "transit:stairwell", projection.SelectedBuildTool == "transit:stairwell");
            DrawToolButton("Shaft\n$1k/fl (2c)", "transit:elevator_shaft", projection.SelectedBuildTool == "transit:elevator_shaft");
            DrawToolButton("Elevator Car\n$2.5k (2c)", "transit:elevator_car", projection.SelectedBuildTool == "transit:elevator_car");
            DrawToolButton("Bulldoze\nReclaim 50%", "demolish:room", projection.SelectedBuildTool == "demolish:room", isDestructive: true);
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawDataPalette(ModeShellProjection projection)
        {
            GUILayout.BeginArea(new Rect(16, Screen.height - 282, 620, 166), StewardTheme.Panel);
            GUILayout.Label("READ THE TOWER", StewardTheme.Label(15, StewardTheme.Text, true));
            GUILayout.Label("Choose a pattern, then select a floor or resident to trace its cause.", StewardTheme.Label(11, StewardTheme.Muted));
            GUILayout.BeginHorizontal();
            DrawOverlay("ELEVATOR WAIT", "overlay:elevator_wait", projection.ActiveOverlayId);
            DrawOverlay("FOOT TRAFFIC", "overlay:foot_traffic", projection.ActiveOverlayId);
            DrawOverlay("POPULATION", "overlay:population", projection.ActiveOverlayId);
            DrawOverlay("SATISFACTION", "overlay:satisfaction", projection.ActiveOverlayId);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawOverlay("SCRUTINY", "overlay:scrutiny", projection.ActiveOverlayId);
            DrawOverlay("BUSINESS", "overlay:business_health", projection.ActiveOverlayId);
            DrawOverlay("UTILITIES", "overlay:utilities", projection.ActiveOverlayId);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawOverlay(string label, string id, string active)
        {
            if (GUILayout.Button(label, id == active ? _activeButtonStyle : _normalButtonStyle,
                    GUILayout.Width(144), GUILayout.Height(32))) Session.SetActiveOverlay(id);
        }

        private void DrawToolButton(string label, string toolId, bool isSelected, bool isDestructive = false)
        {
            var style = isSelected
                ? (isDestructive ? _demolishActiveButtonStyle : _activeToolButtonStyle)
                : _toolButtonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(38), GUILayout.Width(132)))
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
            if (GUILayout.Button(label, style, GUILayout.Height(32), GUILayout.Width(144)))
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
            var contextRect = ContextRect(Screen.height, projection.IsBuildMode);

            GUILayout.BeginArea(contextRect);
            GUILayout.BeginHorizontal();

            var statusText = BuildModeStatusText(projection);

            GUILayout.Label(statusText, _contextStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Pure status-line builder for the mode context bar. Manage mode has no
        /// decree panel yet (OR-902 READY), so it names the upcoming systems-level
        /// levers instead of leaving the player at a dead-end mode label.
        /// </summary>
        public static string BuildModeStatusText(ModeShellProjection projection)
        {
            var statusText = $"MODE: {projection.CurrentMode.ToString().ToUpperInvariant()}";
            if (projection.IsBuildMode)
            {
                if (!string.IsNullOrEmpty(projection.SelectedBuildTool))
                {
                    statusText += $"  /  {ReadableName(projection.SelectedBuildTool)}  /  Click to place · Right-click to cancel";
                }
                else
                {
                    statusText += "  /  Choose a tool from the palette";
                }
            }
            else if (projection.IsDataMode && !string.IsNullOrEmpty(projection.ActiveOverlayId))
            {
                statusText += $"  /  Overlay: {ReadableName(projection.ActiveOverlayId)}";
            }
            else if (projection.IsInspectMode && projection.SelectedEntityId.HasValue)
            {
                statusText += $"  |  Entity: #{projection.SelectedEntityId.Value}";
            }
            else if (projection.IsManageMode)
            {
                statusText += "  /  Treasury and leasing update automatically. Steward policies are coming soon.";
            }

            return statusText;
        }

        private static string ReadableName(string id)
        {
            var separator = id.LastIndexOf(':');
            var name = separator >= 0 ? id.Substring(separator + 1) : id;
            return name.Replace('_', ' ').ToUpperInvariant();
        }

        private void EnsureStyles()
        {
            if (_activeButtonStyle != null)
            {
                return;
            }

            _normalButtonStyle = StewardTheme.Button;
            _activeButtonStyle = StewardTheme.ActiveButton;
            _bannerStyle = StewardTheme.Label(14, StewardTheme.Text, true);
            _contextStyle = StewardTheme.Label(11, StewardTheme.Muted, true);
            _toolButtonStyle = StewardTheme.Button;
            _activeToolButtonStyle = StewardTheme.ActiveButton;
            _demolishActiveButtonStyle = StewardTheme.WarningButton;
        }
    }
}
