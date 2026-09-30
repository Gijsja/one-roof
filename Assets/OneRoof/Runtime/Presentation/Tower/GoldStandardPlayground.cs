using UnityEngine;
using OneRoof.UI;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Scene-only bootstrap keeps generated gameplay views out of the authored scene.</summary>
    public sealed class GoldStandardPlayground : MonoBehaviour
    {
        [SerializeField] private TowerPlayableController _tower;
        [SerializeField] private GoldCityEnvironment _environmentPrefab;
        private GoldCityEnvironment _environment;
        public TowerPlayableController Tower => _tower;
        public GoldCityEnvironment Environment => _environment;
        private Camera _camera;
        private TowerDashboardHudView _dashboard;
        private void Awake()
        {
            _tower.gameObject.SetActive(true);
            _tower.Initialize();
            _tower.Onboarding.Skip();
            // Keep dense repeated light shafts subordinate to the furnished cutaway.
            _tower.AtmospherePresenter.WindowLightStrength = .2f;
            _tower.ExteriorPresenter.WindowLightStrength = .22f;
            _environment = Instantiate(_environmentPrefab, transform);
            _environment.name = "Living City";
            _environment.transform.localPosition = new Vector3(0, TowerStructurePresenter.BaseFloorY, 0);
            _environment.Bind(_tower);
            _camera = Camera.main;
            _dashboard = _tower.GetComponent<TowerDashboardHudView>();
        }
        public void FrameStreet()
        {
            _camera.transform.position = new Vector3(2, 1, -10);
            _camera.orthographicSize = 9;
        }

        public void FrameCrown()
        {
            _camera.transform.position = new Vector3(0, TowerStructurePresenter.FloorY(29)-2, -10);
            _camera.orthographicSize = 9;
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit5Key.wasPressedThisFrame) _camera.GetComponent<TowerCameraController>().FocusOverview();
            if (keyboard.digit6Key.wasPressedThisFrame) FrameStreet();
            if (keyboard.digit7Key.wasPressedThisFrame) FrameCrown();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha5)) _camera.GetComponent<TowerCameraController>().FocusOverview();
            if (Input.GetKeyDown(KeyCode.Alpha6)) FrameStreet();
            if (Input.GetKeyDown(KeyCode.Alpha7)) FrameCrown();
#endif
        }

        private void OnGUI()
        {
            if (_dashboard != null && _dashboard.IsCollapsed) return;
            GUILayout.BeginArea(new Rect(Mathf.Max(650, Screen.width-348), Screen.height-98, 332, 82), StewardTheme.Panel);
            GUILayout.Label("GOLD STANDARD  /  CITY PLAYGROUND", StewardTheme.Label(12, StewardTheme.Amber, true));
            GUILayout.Label("5  Full tower   6  Street   7  Rooftop   F  Facade", StewardTheme.Label(11, StewardTheme.Text));
            GUILayout.Label("Space  Pause    W  Weather    R  Reset    H  Hide", StewardTheme.Label(10, StewardTheme.Muted));
            GUILayout.EndArea();
        }
    }
}
