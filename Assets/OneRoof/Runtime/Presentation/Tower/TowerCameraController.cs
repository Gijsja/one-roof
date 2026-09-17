using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Interactive camera controller for navigating multi-floor vertical towers.
    /// Provides smooth keyboard pan (WASD/Arrows), middle/right mouse drag pan,
    /// mouse wheel zoom, and bounds clamping.
    /// </summary>
    [DisallowMultipleComponent]
    public class TowerCameraController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _panSpeed = 12f;
        [SerializeField] private float _dragSensitivity = 1.0f;
        [SerializeField] private float _zoomSensitivity = 1.5f;
        [SerializeField] private float _minOrthographicSize = 3.2f;
        [SerializeField] private float _maxOrthographicSize = 22.0f;

        private Vector3 _dragOriginScreen;
        private bool _isDragging;
        private Vector2 _boundsX = new Vector2(-15f, 15f);
        private Vector2 _boundsY = new Vector2(-2f, 15f);
        private Vector3 _defaultPosition = new Vector3(-1.6f, 3.5f, -10f);
        private float _defaultOrthoSize = 6.8f;

        public Camera Camera
        {
            get => _camera;
            set => _camera = value;
        }

        public float MinOrthographicSize
        {
            get => _minOrthographicSize;
            set => _minOrthographicSize = value;
        }

        public float MaxOrthographicSize
        {
            get => _maxOrthographicSize;
            set => _maxOrthographicSize = value;
        }

        public Vector2 BoundsX => _boundsX;
        public Vector2 BoundsY => _boundsY;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>() ?? Camera.main;
            }
        }

        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            _boundsX = new Vector2(minX, maxX);
            _boundsY = new Vector2(minY, maxY);
        }

        public void SetOverviewDefaults(Vector3 position, float orthoSize)
        {
            _defaultPosition = position;
            _defaultOrthoSize = orthoSize;
        }

        public void FocusOverview()
        {
            if (_camera == null) return;
            _camera.transform.position = _defaultPosition;
            _camera.orthographicSize = Mathf.Clamp(_defaultOrthoSize, _minOrthographicSize, _maxOrthographicSize);
        }

        public void PanBy(Vector2 deltaWorld)
        {
            if (_camera == null) return;
            var pos = _camera.transform.position;
            pos.x = Mathf.Clamp(pos.x + deltaWorld.x, _boundsX.x, _boundsX.y);
            pos.y = Mathf.Clamp(pos.y + deltaWorld.y, _boundsY.x, _boundsY.y);
            _camera.transform.position = pos;
        }

        public void ZoomBy(float deltaZoom)
        {
            if (_camera == null) return;
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize + deltaZoom, _minOrthographicSize, _maxOrthographicSize);
        }

        private void Update()
        {
            if (_camera == null) return;

            HandleKeyboardPan();
            HandleMouseDragPan();
            HandleScrollZoom();
            HandleFocusShortcut();
        }

        private void HandleKeyboardPan()
        {
            var move = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1f;
#endif

            if (move != Vector2.zero)
            {
                var dt = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                var zoomFactor = _camera.orthographicSize / 6.8f;
                PanBy(move.normalized * (_panSpeed * zoomFactor * dt));
            }
        }

        private void HandleMouseDragPan()
        {
            var isDragButtonDown = false;
            var isDragButtonPressed = false;
            var isDragButtonReleased = false;
            var mousePos = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                var mPos = mouse.position.ReadValue();
                mousePos = new Vector3(mPos.x, mPos.y, 0f);
                isDragButtonPressed = mouse.middleButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
                isDragButtonDown = mouse.middleButton.isPressed || mouse.rightButton.isPressed;
                isDragButtonReleased = mouse.middleButton.wasReleasedThisFrame || mouse.rightButton.wasReleasedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            mousePos = Input.mousePosition;
            isDragButtonPressed = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            isDragButtonDown = Input.GetMouseButton(1) || Input.GetMouseButton(2);
            isDragButtonReleased = Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2);
#endif

            if (isDragButtonPressed)
            {
                _dragOriginScreen = mousePos;
                _isDragging = true;
            }
            else if (_isDragging && isDragButtonDown)
            {
                var deltaScreen = mousePos - _dragOriginScreen;
                if (deltaScreen.sqrMagnitude > 0.001f && Screen.height > 0)
                {
                    // Convert screen pixels delta to world units based on current camera orthographic size
                    var worldPerPixel = (_camera.orthographicSize * 2f) / Screen.height;
                    var worldDelta = new Vector2(-deltaScreen.x * worldPerPixel, -deltaScreen.y * worldPerPixel) * _dragSensitivity;
                    PanBy(worldDelta);
                    _dragOriginScreen = mousePos;
                }
            }
            else if (isDragButtonReleased)
            {
                _isDragging = false;
            }
        }

        private void HandleScrollZoom()
        {
            var scrollY = 0f;

#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                scrollY = mouse.scroll.ReadValue().y;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            scrollY = Input.mouseScrollDelta.y;
#endif

            if (Mathf.Abs(scrollY) > 0.01f)
            {
                var zoomDelta = -Mathf.Sign(scrollY) * _zoomSensitivity;
                ZoomBy(zoomDelta);
            }
        }

        private void HandleFocusShortcut()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && (keyboard.fKey.wasPressedThisFrame || keyboard.homeKey.wasPressedThisFrame))
            {
                FocusOverview();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Home))
            {
                FocusOverview();
            }
#endif
        }
    }
}
