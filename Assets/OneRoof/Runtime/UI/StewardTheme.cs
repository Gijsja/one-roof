using System.Collections.Generic;
using UnityEngine;

namespace OneRoof.UI
{
    /// <summary>Shared, code-owned styling for the first playable's IMGUI shell.</summary>
    public static class StewardTheme
    {
        public static readonly Color Ink = new Color(.055f, .105f, .14f);
        public static readonly Color Surface = new Color(.085f, .15f, .19f);
        public static readonly Color Raised = new Color(.12f, .21f, .25f);
        public static readonly Color Line = new Color(.24f, .36f, .38f);
        public static readonly Color Text = new Color(.94f, .96f, .91f);
        public static readonly Color Muted = new Color(.68f, .77f, .76f);
        public static readonly Color Mint = new Color(.55f, .87f, .71f);
        public static readonly Color Amber = new Color(1f, .75f, .43f);
        public static readonly Color Coral = new Color(1f, .54f, .45f);

        private static Texture2D _ink, _surface, _raised, _active, _warning, _line;
        private static GUIStyle _panel, _recess, _button, _activeButton, _warningButton;
        private static readonly Dictionary<(int, Color32, bool), GUIStyle> Labels = new Dictionary<(int, Color32, bool), GUIStyle>();

        public static GUIStyle Panel => _panel ?? (_panel = Box(_surface != null ? _surface : (_surface = Solid(Surface))));
        public static GUIStyle Recess => _recess ?? (_recess = Box(_ink != null ? _ink : (_ink = Solid(Ink))));
        public static GUIStyle Button => _button ?? (_button = ButtonStyle(_raised != null ? _raised : (_raised = Solid(Raised)), Text));
        public static GUIStyle ActiveButton => _activeButton ?? (_activeButton = ButtonStyle(_active != null ? _active : (_active = Solid(new Color(.18f, .42f, .36f))), Mint));
        public static GUIStyle WarningButton => _warningButton ?? (_warningButton = ButtonStyle(_warning != null ? _warning : (_warning = Solid(new Color(.39f, .24f, .21f))), Coral));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCaches()
        {
            _ink = null;
            _surface = null;
            _raised = null;
            _active = null;
            _warning = null;
            _line = null;
            _panel = null;
            _recess = null;
            _button = null;
            _activeButton = null;
            _warningButton = null;
            Labels.Clear();
        }

        public static GUIStyle Label(int size = 12, Color? color = null, bool bold = false)
        {
            var tint = (Color32)(color ?? Text);
            var key = (size, tint, bold);
            if (Labels.TryGetValue(key, out var style)) return style;
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                wordWrap = true,
                normal = { textColor = tint }
            };
            Labels.Add(key, style);
            return style;
        }

        public static void Rule(float width)
        {
            var rect = GUILayoutUtility.GetRect(width, 1, GUILayout.Width(width), GUILayout.Height(1));
            GUI.DrawTexture(rect, _line != null ? _line : (_line = Solid(Line)));
        }

        private static GUIStyle Box(Texture2D texture) => new GUIStyle(GUI.skin.box)
        {
            normal = { background = texture, textColor = Text },
            padding = new RectOffset(12, 12, 10, 10)
        };

        private static GUIStyle ButtonStyle(Texture2D texture, Color text) => new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { background = texture, textColor = text },
            hover = { background = texture, textColor = Color.white },
            active = { background = texture, textColor = Color.white },
            padding = new RectOffset(8, 8, 5, 5)
        };

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
