using OneRoof.Application.Management;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.UI;
using UnityEngine;

namespace OneRoof.UI.Management
{
    /// <summary>Keyboard-accessible IMGUI decree panel for Manage mode.</summary>
    [DisallowMultipleComponent]
    public sealed class PolicyDecreePanelView : MonoBehaviour
    {
        private PolicyDecreeDraft _draft;
        private ModeShellSession _modes;
        private Vector2 _scroll;
        private string _message = string.Empty;
        private int _focusRow;

        public PolicyDecreeDraft Draft => _draft;
        public string Message => _message;
        public static Rect PanelRect(int screenWidth, int screenHeight) =>
            new Rect(16, 112, Mathf.Min(620, screenWidth - 32), Mathf.Max(300, screenHeight - 330));

        public void Bind(TowerSimulationSession simulation, ModeShellSession modes)
        {
            _draft = new PolicyDecreeDraft(simulation);
            _modes = modes;
            _message = string.Empty;
        }

        private void Update()
        {
            if (_draft == null || _modes == null || _modes.CurrentMode != InteractionMode.Manage) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.tabKey.wasPressedThisFrame) _focusRow = (_focusRow + 1) % 6;
            if (keyboard.leftArrowKey.wasPressedThisFrame) Step(-1);
            if (keyboard.rightArrowKey.wasPressedThisFrame) Step(1);
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Confirm();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Tab)) _focusRow = (_focusRow + 1) % 6;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) Step(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow)) Step(1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
#endif
        }

        public void Step(int direction)
        {
            if (_draft == null || direction == 0) return;
            var p = _draft.Draft;
            switch (_focusRow)
            {
                case 0:
                    var rent = p.RentCapMultiplier < .8f ? 0 : p.RentCapMultiplier < 1.1f ? 1 : 2;
                    _draft.SelectRent(new[] { .7f, 1f, 1.3f }[Mathf.Clamp(rent + direction, 0, 2)]);
                    break;
                case 1:
                    var tax = p.CommercialTaxRate < .05f ? 0 : p.CommercialTaxRate < .15f ? 1 : 2;
                    _draft.SelectTax(new[] { 0f, .1f, .2f }[Mathf.Clamp(tax + direction, 0, 2)]);
                    break;
                case 2: _draft.SelectTransitSubsidy(direction > 0); break;
                case 3: _draft.SelectQuietHours(direction > 0); break;
                case 4: if (direction > 0) Confirm(); break;
                case 5: if (direction > 0) _draft.ResetDraft(); break;
            }
        }

        public void Confirm()
        {
            if (_draft == null) return;
            var result = _draft.Confirm();
            _message = result.Accepted ? "Decree enacted. Read the dated receipt below." :
                result.Rejections[0].Code.Value + ": " + result.Rejections[0].Message;
        }

        private void OnGUI()
        {
            if (_draft == null || _modes == null || _modes.CurrentMode != InteractionMode.Manage) return;
            var rect = PanelRect(Screen.width, Screen.height);
            GUILayout.BeginArea(rect, StewardTheme.Panel);
            GUILayout.Label("MANAGE  /  DECREES", StewardTheme.Label(15, StewardTheme.Text, true));
            GUILayout.Label("Draft changes have no effect until confirmed. Tab selects a row; arrows change it; Enter confirms.", StewardTheme.Label(11, StewardTheme.Muted));
            _scroll = GUILayout.BeginScrollView(_scroll);
            var current = _draft.Current;
            var proposed = _draft.Draft;
            GUILayout.Label($"Rent cap  [current {current.RentCapMultiplier:0.0}x | proposed {proposed.RentCapMultiplier:0.0}x]{Focus(0)}");
            DrawOptions(0, new[] { "0.7x", "1.0x", "1.3x" }, new[] { .7f, 1f, 1.3f }, proposed.RentCapMultiplier);
            GUILayout.Label($"Commercial tax  [current {current.CommercialTaxRate:P0} | proposed {proposed.CommercialTaxRate:P0}]{Focus(1)}");
            DrawOptions(1, new[] { "0%", "10%", "20%" }, new[] { 0f, .1f, .2f }, proposed.CommercialTaxRate);
            GUILayout.Label($"Transit subsidy  [current {OnOff(current.TransitSubsidyEnabled)} | proposed {OnOff(proposed.TransitSubsidyEnabled)}]{Focus(2)}");
            DrawToggle(2, proposed.TransitSubsidyEnabled);
            GUILayout.Label($"Quiet hours  [current {OnOff(current.QuietHoursEnabled)} | proposed {OnOff(proposed.QuietHoursEnabled)}]{Focus(3)}");
            DrawToggle(3, proposed.QuietHoursEnabled);
            GUILayout.Space(8);
            GUILayout.Label(_draft.Estimate(), StewardTheme.Label(11, StewardTheme.Text));
            GUILayout.Label(_draft.Impacts(), StewardTheme.Label(11, StewardTheme.Muted));
            if (GUILayout.Button("CONFIRM DECREE" + Focus(4), GUILayout.Height(30))) Confirm();
            if (GUILayout.Button("RESET DRAFT" + Focus(5), GUILayout.Height(26))) _draft.ResetDraft();
            if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message, StewardTheme.Label(11, StewardTheme.Text));
            if (!string.IsNullOrEmpty(_draft.Receipt)) GUILayout.Label(_draft.SettlementReceipt(), StewardTheme.Label(11, StewardTheme.Text));
            if (GUILayout.Button("VIEW FACTION TENSION", GUILayout.Height(26)))
                _modes.SetActiveOverlay("overlay:faction_tension");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawOptions(int row, string[] labels, float[] values, float selected)
        {
            GUILayout.BeginHorizontal();
            for (var i = 0; i < values.Length; i++)
                if (GUILayout.Button((Mathf.Abs(values[i] - selected) < .001f ? "[" + labels[i] + "]" : labels[i]), GUILayout.Width(112)))
                {
                    _focusRow = row;
                    if (row == 0) _draft.SelectRent(values[i]); else _draft.SelectTax(values[i]);
                }
            GUILayout.EndHorizontal();
        }

        private void DrawToggle(int row, bool selected)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(selected ? "OFF" : "[OFF]", GUILayout.Width(112))) { _focusRow = row; SetToggle(row, false); }
            if (GUILayout.Button(selected ? "[ON]" : "ON", GUILayout.Width(112))) { _focusRow = row; SetToggle(row, true); }
            GUILayout.EndHorizontal();
        }

        private void SetToggle(int row, bool enabled)
        { if (row == 2) _draft.SelectTransitSubsidy(enabled); else _draft.SelectQuietHours(enabled); }
        private string Focus(int row) => _focusRow == row ? "  <selected>" : string.Empty;
        private static string OnOff(bool value) => value ? "on" : "off";
    }
}
