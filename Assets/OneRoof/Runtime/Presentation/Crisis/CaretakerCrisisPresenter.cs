using System.Collections.Generic;
using OneRoof.Application.Crisis;
using OneRoof.Application.Tower;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Crisis;
using OneRoof.Presentation.Population;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OneRoof.Presentation.Crisis
{
    /// <summary>Exclusive arcade input, pooled articulated views, lobby contact and crisis cause-chain HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class CaretakerCrisisPresenter : MonoBehaviour
    {
        private TowerPlayableController _owner;
        private TowerSimulationSession _session;
        private CaretakerCrisisProjection _state;
        private CaretakerCombatRig _caretaker;
        private readonly List<CaretakerCombatRig> _opponents = new List<CaretakerCombatRig>(4);
        private readonly Dictionary<int, int> _lastHealth = new Dictionary<int, int>();
        private CaretakerImpactPool _impacts;
        private Transform _viewRoot;
        private TextMesh _contactMarker, _stairsMarker;
        private Camera _camera;
        private Vector3 _savedCameraPosition;
        private float _savedCameraSize, _accumulator;
        private float _trauma, _hitStopTimer;
        private bool _cameraCaptured, _lastActive, _wasInAir;
        private const float TraumaDecay = 1.35f;
        private static readonly Vector2 MaxShakeOffset = new Vector2(0.32f, 0.20f);
        private const float MaxShakeAngle = 3.5f;

        private int _previousHealth = 100, _previousFloor = -1, _lastX;
        private string _message = "";
        private Vector2 _contactScroll;
        private GUIStyle _title, _label, _small, _button;
        private static readonly Color Ink = new Color(.055f, .071f, .091f, .95f);
        private static readonly Color Gold = new Color(1f, .78f, .35f);
        public bool ContactOpen { get; private set; }
        public bool IsArcadeActive => _session != null && _session.CaretakerOwnsInput;
        public Transform LobbyCaretaker => _caretaker?.Transform;

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public void Initialize(TowerPlayableController owner, TowerSimulationSession session, Material sharedMaterial)
        {
            _owner = owner;
            _session = session;
            if (_viewRoot == null)
            {
                _viewRoot = new GameObject("Caretaker crisis views").transform;
                _viewRoot.SetParent(transform, false);
                var material = CaretakerCombatRig.GetOrCreateDefaultCombatMaterial();
                _caretaker = new CaretakerCombatRig(_viewRoot, "Caretaker • Lobby contact", 9, material);
                _impacts = new CaretakerImpactPool(_viewRoot, material);
                _contactMarker = Marker("Caretaker contact", "CARETAKER", Gold);
                _stairsMarker = Marker("Arcade exit stairs", "STAIRS ↑", Gold);
            }

            _camera = Camera.main;
            RefreshPresentation();
        }

        private TextMesh Marker(string name, string text, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_viewRoot, false);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = .032f;
            mesh.fontSize = 32;
            mesh.color = color;
            go.GetComponent<MeshRenderer>().sortingOrder = 950;
            return mesh;
        }

        public void ToggleContact()
        {
            if (IsArcadeActive || (_owner != null && _owner.WordBattlePanel != null && _owner.WordBattlePanel.IsOpen))
                return;
            ContactOpen = !ContactOpen;
            _message = "";
            _owner?.RefreshCaretakerInputLock();
        }

        public bool RequestAction(CaretakerCrisisAction action)
        {
            if (_session == null || (_owner != null && _owner.WordBattlePanel != null && _owner.WordBattlePanel.IsOpen))
                return false;
            var result = _session.ExecuteCaretaker(action);
            _message = result.Accepted ? "" : result.Reason;
            if (result.Accepted && (action == CaretakerCrisisAction.Start || action == CaretakerCrisisAction.Retry || action == CaretakerCrisisAction.Practice))
                ContactOpen = false;
            RefreshPresentation();
            _owner?.RefreshCaretakerInputLock();
            return result.Accepted;
        }

        private void Update()
        {
            if (_session == null || (_owner != null && _owner.WordBattlePanel != null && _owner.WordBattlePanel.IsOpen))
                return;
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if (!IsArcadeActive)
            {
                if ((keyboard != null && keyboard.cKey.wasPressedThisFrame) || (pad != null && pad.selectButton.wasPressedThisFrame))
                    ToggleContact();
                if (ContactOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                    ToggleContact();
            }
            else
            {
                _state = _session.CaretakerProjection();
                if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame))
                    RequestAction(_state.Paused ? CaretakerCrisisAction.Resume : CaretakerCrisisAction.Pause);
                if ((keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)) || (pad != null && pad.buttonNorth.wasPressedThisFrame))
                    RequestAction(CaretakerCrisisAction.Advance);
                var move = 0;
                if (keyboard != null)
                {
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                        move--;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                        move++;
                }

                if (pad != null && Mathf.Abs(pad.leftStick.x.ReadValue()) > .2f)
                    move = pad.leftStick.x.ReadValue() > 0 ? 1 : -1;
                var punch = keyboard != null && keyboard.jKey.isPressed || pad != null && pad.buttonWest.isPressed;
                var kick = keyboard != null && keyboard.kKey.isPressed || pad != null && pad.buttonEast.isPressed;
                var duck = keyboard != null && (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) || pad != null && pad.leftStick.y.ReadValue() < -.4f;
                var jump = keyboard != null && keyboard.spaceKey.wasPressedThisFrame || pad != null && pad.buttonSouth.wasPressedThisFrame;
                if (_hitStopTimer > 0f)
                {
                    _hitStopTimer = Mathf.Max(0f, _hitStopTimer - Time.unscaledDeltaTime);
                    // During arcade hit-stop freeze frames, hold simulation steps to emphasize impact weight
                }
                else
                {
                    _accumulator = Mathf.Min(_accumulator + Time.unscaledDeltaTime, .15f);
                    while (_accumulator >= 1f / CaretakerCrisisState.StepsPerSecond)
                    {
                        _session.ExecuteCaretaker(CaretakerCrisisAction.Step, new CaretakerCombatInput(move, punch, kick, duck, jump));
                        jump = false;
                        _accumulator -= 1f / CaretakerCrisisState.StepsPerSecond;
                    }
                }
            }

            RefreshPresentation();
            _impacts.Tick(Time.unscaledDeltaTime);
            _trauma = Mathf.Max(0f, _trauma - TraumaDecay * Time.unscaledDeltaTime);
        }

        private static string AttackName(CaretakerAttack attack) => attack == CaretakerAttack.Punch ? "Punch" : attack == CaretakerAttack.Kick ? "Kick" : attack == CaretakerAttack.JumpKick ? "JumpKick" : "None";
        private static float WorldX(int x) => GridPlacementController.DefaultCellOriginX + x / (float)CaretakerCrisisState.UnitsPerCell * GridPlacementController.DefaultCellWidth;
        private static float GroundY(int floor) => TowerPlayableController.FloorY(floor) - .58f;
        private float LobbyX()
        {
            var topology = _session.TopologyProjection();
            foreach (var room in topology.Rooms.Values)
                if (room.Floor == 0 && room.ContentType.Value == "room:lobby")
                    return GridPlacementController.DefaultCellOriginX + (room.Bounds.MinX + room.Bounds.MaxX + 1) * GridPlacementController.DefaultCellWidth * .5f;
            if (topology.TryGetFloorSlab(0, out var slab))
                return GridPlacementController.DefaultCellOriginX + (slab.MinX + slab.MaxX + 1) * GridPlacementController.DefaultCellWidth * .5f;
            return GridPlacementController.DefaultCellOriginX;
        }

        public void RefreshPresentation()
        {
            if (_session == null || _caretaker == null)
                return;
            _state = _session.CaretakerProjection();
            var active = IsArcadeActive;
            if (active && !_lastActive)
            {
                ContactOpen = false;
                _accumulator = 0;
                _previousHealth = _state.Health;
                _lastHealth.Clear();
                if (_camera == null)
                    _camera = Camera.main;
                if (_camera != null)
                {
                    _savedCameraPosition = _camera.transform.position;
                    _savedCameraSize = _camera.orthographicSize;
                    _cameraCaptured = true;
                }
            }

            if (!active && _lastActive)
            {
                RestoreCamera();
                _accumulator = 0;
                ContactOpen = true;
            }

            _lastActive = active;
            if (!active)
            {
                _caretaker.Present(new Vector3(LobbyX(), GroundY(0), -.8f), 1, "None", false, 0, false, false);
                foreach (var rig in _opponents)
                    rig.GameObject.SetActive(false);
                _stairsMarker.gameObject.SetActive(false);
                _contactMarker.gameObject.SetActive(true);
                _contactMarker.transform.position = _caretaker.Transform.position + new Vector3(0, 1, -.1f);
                return;
            }

            _contactMarker.gameObject.SetActive(false);
            var inAir = _state.JumpFrames > 0;
            var jumpHeight = inAir ? Mathf.Sin(_state.JumpFrames / 36f * Mathf.PI) * .55f : 0;
            var caretakerPosition = new Vector3(WorldX(_state.X), GroundY(_state.CurrentFloor) + jumpHeight, -.85f);

            // Landing dust puff detection when touching down from jump
            if (_wasInAir && !inAir)
            {
                _impacts.EmitLandingDust(new Vector3(caretakerPosition.x, GroundY(_state.CurrentFloor) - 0.05f, -.85f));
                AddTrauma(0.08f);
            }
            _wasInAir = inAir;

            var hit = _state.Health < _previousHealth;
            if (hit)
            {
                _impacts.Emit(caretakerPosition + Vector3.up * .45f, true);
                _impacts.EmitStreak(caretakerPosition + Vector3.up * .45f, new Vector2(-_state.Facing, 0.2f), true, false);
                _hitStopTimer = 0.06f; // Hit-stop frames on Caretaker damage
                AddTrauma(0.40f);
            }

            var isDashing = Mathf.Abs(_state.X - _lastX) > 1;
            _caretaker.Present(caretakerPosition, _state.Facing, AttackName(_state.Attack), _lastX != _state.X, _state.AttackFrame / (_state.Attack == CaretakerAttack.Punch ? 18f : 28f), hit, _state.Phase == CaretakerCrisisPhase.Failed, _state.Ducking, isBoss: false, attackFrame: _state.AttackFrame, isCritical: false, isDashing: isDashing);
            _previousHealth = _state.Health;
            _lastX = _state.X;
            if (_previousFloor != _state.RouteIndex)
            {
                _lastHealth.Clear();
                _previousFloor = _state.RouteIndex;
            }

            while (_opponents.Count < _state.Opponents.Count)
                _opponents.Add(new CaretakerCombatRig(_viewRoot, "Faction encounter opponent " + _opponents.Count, 13 + _opponents.Count * 7, CaretakerCombatRig.GetOrCreateDefaultCombatMaterial()));
            for (var i = 0; i < _opponents.Count; i++)
            {
                var rig = _opponents[i];
                rig.GameObject.SetActive(i < _state.Opponents.Count);
                if (i >= _state.Opponents.Count)
                    continue;
                var opponent = _state.Opponents[i];
                var position = new Vector3(WorldX(opponent.X), GroundY(_state.CurrentFloor), -.8f);
                var isBoss = opponent.Role == CaretakerOpponentRole.FloorBoss;
                var opponentHit = _lastHealth.TryGetValue(opponent.Id, out var oldHealth) && opponent.Health < oldHealth;
                var isCritical = opponentHit && (isBoss || _state.Attack == CaretakerAttack.JumpKick);

                if (opponentHit)
                {
                    _impacts.Emit(position + Vector3.up * .45f, false);
                    Vector2 attackVector = _state.Attack == CaretakerAttack.Kick
                        ? new Vector2(_state.Facing, 0.45f)
                        : _state.Attack == CaretakerAttack.JumpKick
                            ? new Vector2(_state.Facing, -0.45f)
                            : new Vector2(_state.Facing, 0.1f);

                    _impacts.EmitStreak(position + Vector3.up * .45f, attackVector, false, isCritical);
                    _hitStopTimer = isBoss ? 0.10f : 0.05f; // Extended hit-stop frames for floor bosses
                    AddTrauma(isBoss ? 0.55f : 0.28f);
                }

                rig.Present(position, opponent.Facing, opponent.AttackFrame > 0 ? "Punch" : "None", opponent.Timer == 0, opponent.AttackFrame / 42f, opponentHit, opponent.Health <= 0, crouching: false, isBoss: isBoss, attackFrame: opponent.AttackFrame, isCritical: isCritical, isDashing: false);
                _lastHealth[opponent.Id] = opponent.Health;
            }

            var route = _state.Route.Count > _state.RouteIndex ? _state.Route[_state.RouteIndex] : null;
            _stairsMarker.gameObject.SetActive(route != null);
            if (route != null)
            {
                _stairsMarker.text = _state.FloorCleared ? "E / Y   STAIRS ↑" : "CLEAR THE FLOOR";
                _stairsMarker.transform.position = new Vector3(WorldX(route.ExitX), GroundY(route.Floor) + 1.1f, -.9f);
            }

            // Corgi-style polynomial trauma camera shake: shake = trauma^2 * maxOffset
            if (_camera != null)
            {
                var desired = new Vector3(caretakerPosition.x + _state.Facing * .8f, GroundY(_state.CurrentFloor) + .8f, _savedCameraPosition.z);
                var shake = _trauma * _trauma;
                if (shake > 0.001f)
                {
                    var time = Time.unscaledTime * 32f;
                    var offsetX = (Mathf.Sin(time * 3.1f) + Mathf.Sin(time * 7.3f) * 0.5f) * MaxShakeOffset.x * shake;
                    var offsetY = (Mathf.Cos(time * 2.7f) + Mathf.Cos(time * 6.1f) * 0.5f) * MaxShakeOffset.y * shake;
                    var roll = Mathf.Sin(time * 4.2f) * MaxShakeAngle * shake;
                    desired += new Vector3(offsetX, offsetY, 0f);
                    _camera.transform.rotation = Quaternion.Euler(0f, 0f, roll);
                }
                else
                {
                    _camera.transform.rotation = Quaternion.identity;
                }

                _camera.transform.position = Vector3.Lerp(_camera.transform.position, desired, .2f);
                _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, 2.15f, .2f);
            }
        }

        private void RestoreCamera()
        {
            if (_cameraCaptured && _camera != null)
            {
                _camera.transform.position = _savedCameraPosition;
                _camera.transform.rotation = Quaternion.identity;
                _camera.orthographicSize = _savedCameraSize;
            }

            _cameraCaptured = false;
            _trauma = 0f;
            _hitStopTimer = 0f;
            _wasInAir = false;
        }

        private void OnDisable()
        {
            ContactOpen = false;
            RestoreCamera();
            _lastActive = false;
            _owner?.RefreshCaretakerInputLock();
        }

        private void OnDestroy()
        {
            _impacts?.Dispose();
            _caretaker?.Dispose();
            for (var i = 0; i < _opponents.Count; i++)
                _opponents[i]?.Dispose();
            if (_viewRoot != null)
            {
                if (UnityEngine.Application.isPlaying)
                    Destroy(_viewRoot.gameObject);
                else
                    DestroyImmediate(_viewRoot.gameObject);
            }
        }

        private void EnsureStyles()
        {
            if (_title != null)
                return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Gold
                }
            };
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal =
                {
                    textColor = new Color(.91f, .93f, .95f)
                }
            };
            _small = new GUIStyle(_label)
            {
                fontSize = 12,
                normal =
                {
                    textColor = new Color(.65f, .73f, .8f)
                }
            };
            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fixedHeight = 36
            };
        }

        private static void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void OnGUI()
        {
            if (_session == null || _state == null || (_owner != null && _owner.WordBattlePanel != null && _owner.WordBattlePanel.IsOpen))
                return;
            EnsureStyles();
            var oldMatrix = GUI.matrix;
            var scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            var width = Screen.width / scale;
            var height = Screen.height / scale;
            if (IsArcadeActive)
                DrawArcade(width, height);
            else if (ContactOpen)
                DrawContact(width, height);
            else
            {
                if (GUI.Button(new Rect(width - 278, height - 64, 258, 44), "CARETAKER  /  Lobby contact   [C]", _button))
                    ToggleContact();
                if (_state.Phase == CaretakerCrisisPhase.Warning || _state.Phase == CaretakerCrisisPhase.Ready)
                    GUI.Label(new Rect(width - 278, height - 92, 258, 26), "FACTION PRESSURE  ·  " + _state.Phase.ToString().ToUpperInvariant(), _small);
            }

            GUI.matrix = oldMatrix;
        }

        private void DrawContact(float width, float height)
        {
            Fill(new Rect(0, 0, width, height), new Color(0, 0, 0, .55f));
            var rect = new Rect((width - 680) / 2, (height - 620) / 2, 680, 620);
            Fill(rect, Ink);
            Fill(new Rect(rect.x, rect.y, 4, rect.height), Gold);
            GUILayout.BeginArea(new Rect(rect.x + 28, rect.y + 22, rect.width - 56, rect.height - 44));
            GUILayout.Label("THE CARETAKER", _title);
            GUILayout.Label("Lobby contact  /  Kungfu eviction crisis", _small);
            GUILayout.Space(16);
            _contactScroll = GUILayout.BeginScrollView(_contactScroll, GUILayout.ExpandHeight(true));
            GUILayout.Label("Residents bring their concerns here. Factions organize around sustained grievances; their membership alone never makes someone an opponent.", _label);
            GUILayout.Space(14);
            GUILayout.Label("STATUS  ·  " + _state.Phase.ToString().ToUpperInvariant(), _title);
            GUILayout.Label("Faction: " + (string.IsNullOrEmpty(_state.FactionId) ? "No organized escalation" : _state.FactionId) + "    Pressure days: " + _state.WarningDays, _label);
            GUILayout.Label("Cause: " + _state.Cause + "    Affected residents: " + _state.AffectedResidentIds.Count, _label);
            GUILayout.Space(12);
            GUILayout.Label("CAUSE-CHAIN", _small);
            GUILayout.Label("Sustained grievance → faction tension overlay → organized local action → improve leases, services or policy → watch pressure recover.", _label);
            GUILayout.Space(12);
            GUILayout.Label(_state.ConsequencesApplied ? "Eviction recorded for " + _state.Evictions.Count + " organized participants. Their whole households lose housing; resident thoughts, scrutiny and the decision record persist. The original grievance remains." : "Enforcement is consequential: winning evicts the whole households of " + _state.AffectedResidentIds.Count + " organized participants, including household members who did not participate. It adds resident thoughts, scrutiny and a decision record. The original grievance remains; management relief is available before entry.", _label);
            if (!string.IsNullOrEmpty(_message))
                GUILayout.Label(_message, _small);
            GUILayout.EndScrollView();
            GUILayout.Space(12);
            if (GUILayout.Button("Rehearse Kungfu  /  No resident consequences", _button))
                RequestAction(CaretakerCrisisAction.Practice);
            if (GUILayout.Button("Inspect faction pressure", _button))
            {
                ToggleContact();
                _owner.ShowFactionTensionOverlay();
            }

            GUI.enabled = _state.Phase == CaretakerCrisisPhase.Ready;
            if (GUILayout.Button("Take care of business  /  Enter Kungfu eviction", _button))
                RequestAction(CaretakerCrisisAction.Start);
            GUI.enabled = true;
            if (GUILayout.Button("Return to Tower  [Esc]", _button))
                ToggleContact();
            GUILayout.EndArea();
        }

        private void DrawArcade(float width, float height)
        {
            Fill(new Rect(0, 0, width, 114), Ink);
            Fill(new Rect(0, height - 70, width, 70), Ink);
            GUI.Label(new Rect(26, 16, 450, 32), _state.Practice ? "KUNGFU REHEARSAL" : "KUNGFU EVICTION", _title);
            GUI.Label(new Rect(26, 53, 500, 24), "CARETAKER   /   Floor " + _state.CurrentFloor + "   ·   Route " + (_state.RouteIndex + 1) + "/" + _state.Route.Count, _small);
            Fill(new Rect(26, 86, 320, 9), new Color(.22f, .26f, .3f));
            Fill(new Rect(26, 86, 320 * _state.Health / 100f, 9), Gold);
            GUI.Label(new Rect(364, 79, 180, 26), "HEALTH  " + _state.Health, _label);
            GUI.Label(new Rect(width - 400, 20, 374, 32), "TIME  " + Mathf.CeilToInt(_state.Clock / 60f) + "    SCORE  " + _state.Score, _title);
            GUI.Label(new Rect(width - 400, 60, 374, 40), _state.FloorCleared ? "FLOOR CLEAR  ·  Reach the stair marker and press E / Y" : "Watch the opponent windup; jump or duck ranged threats.", _label);
            GUI.Label(new Rect(26, height - 57, width - 52, 25), "A/D or stick: move   ·   J / X: punch   ·   K / B: kick   ·   S / ↓: duck   ·   Space / A: jump   ·   E / Y: stairs   ·   Esc / Start: pause", _small);
            if (!string.IsNullOrEmpty(_message))
                GUI.Label(new Rect(26, height - 32, width - 52, 24), _message, _small);
            else
                GUI.Label(new Rect(26, height - 32, width - 52, 24), "Actual Tower floors and stairs  /  Management clock held while the Caretaker is playable", _small);
            if (_camera != null)
                for (var i = 0; i < _state.Opponents.Count; i++)
                {
                    var o = _state.Opponents[i];
                    if (o.Health <= 0)
                        continue;
                    var screen = _camera.WorldToScreenPoint(new Vector3(WorldX(o.X), GroundY(_state.CurrentFloor) + 1, -.85f));
                    var x = screen.x / (Screen.width / width);
                    var y = (Screen.height - screen.y) / (Screen.height / height);
                    Fill(new Rect(x - 40, y, 80, 5), new Color(.24f, .25f, .29f));
                    Fill(new Rect(x - 40, y, 80 * o.Health / (o.Role == CaretakerOpponentRole.FloorBoss ? 80f : 35f), 5), o.Role == CaretakerOpponentRole.FloorBoss ? Gold : new Color(.85f, .34f, .32f));
                    GUI.Label(new Rect(x - 65, y - 25, 145, 24), o.AttackFrame > 0 && o.AttackFrame < 24 ? "!  " + (o.Role == CaretakerOpponentRole.Thrower ? "DUCK / JUMP" : "WINDUP") : o.Role.ToString(), _small);
                }

            if (_state.Paused || _state.Phase == CaretakerCrisisPhase.Failed)
            {
                Fill(new Rect(0, 114, width, height - 184), new Color(0, 0, 0, .55f));
                GUILayout.BeginArea(new Rect((width - 420) / 2, (height - 230) / 2, 420, 230));
                GUILayout.Label(_state.Phase == CaretakerCrisisPhase.Failed ? "CARETAKER DOWN" : "ENCOUNTER PAUSED", _title);
                GUILayout.Label("The Tower clock is held. Return to management or continue the encounter.", _label);
                if (_state.Phase == CaretakerCrisisPhase.Failed)
                {
                    if (GUILayout.Button("Retry route", _button))
                        RequestAction(CaretakerCrisisAction.Retry);
                }
                else if (GUILayout.Button("Resume", _button))
                    RequestAction(CaretakerCrisisAction.Resume);
                if (GUILayout.Button("Return to lobby", _button))
                    RequestAction(CaretakerCrisisAction.Exit);
                GUILayout.EndArea();
            }
        }
    }
}
