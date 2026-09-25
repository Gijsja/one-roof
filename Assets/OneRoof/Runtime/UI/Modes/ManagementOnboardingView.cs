using OneRoof.Application.Modes;
using UnityEngine;

namespace OneRoof.UI.Modes
{
    /// <summary>Optional text-first guide for the first management loop.</summary>
    [DisallowMultipleComponent]
    public sealed class ManagementOnboardingView : MonoBehaviour
    {
        public ManagementOnboarding Lesson { get; set; }

        public static Rect PanelRect(int screenWidth) =>
            new Rect(screenWidth - Mathf.Min(360f, screenWidth - 32f) - 16f, 16f,
                Mathf.Min(360f, screenWidth - 32f), 116f);

        private void Update()
        {
            if (Lesson == null) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame) ToggleGuide();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F1)) ToggleGuide();
#endif
        }

        private void ToggleGuide()
        {
            if (Lesson.IsSkipped || Lesson.IsComplete) Lesson.Restart();
            else Lesson.Skip();
        }

        private void OnGUI()
        {
            if (Lesson == null) return;
            GUILayout.BeginArea(PanelRect(Screen.width), StewardTheme.Panel);
            GUILayout.Label("STEWARD GUIDE", StewardTheme.Label(14, StewardTheme.Text, true));
            if (Lesson.IsComplete)
            {
                GUILayout.Label("Next: compare rent in Manage and watch Utilities in Data as the tower grows. F1 restarts.", StewardTheme.Label(11, StewardTheme.Muted));
                if (GUILayout.Button("Restart guide (F1)")) Lesson.Restart();
            }
            else if (Lesson.IsSkipped)
            {
                GUILayout.Label("Guide paused. Press F1 to restart.", StewardTheme.Label(11, StewardTheme.Muted));
                if (GUILayout.Button("Restart guide (F1)")) Lesson.Restart();
            }
            else
            {
                GUILayout.Label(Lesson.Instruction, StewardTheme.Label(11, StewardTheme.Text));
                if (GUILayout.Button("Skip guide (F1)")) Lesson.Skip();
            }
            GUILayout.EndArea();
        }
    }
}
