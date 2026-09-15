using System.Collections.Generic;
using OneRoof.Application.Transit;
using UnityEngine;

namespace OneRoof.Presentation.Transit
{
    /// <summary>Renders the deterministic prototype snapshot and keeps all mutable commute state in Domain.</summary>
    public sealed class TransitPrototypeController : MonoBehaviour
    {
        private const float TickSeconds = 0.35f;
        private readonly List<MeshRenderer> _residentViews = new List<MeshRenderer>(TransitPrototypeSession.ResidentCount);
        private readonly List<MeshRenderer> _elevatorViews = new List<MeshRenderer>();
        private TransitPrototypeSession _session;
        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;
        private float _tickAccumulator;
        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;

        private void Awake()
        {
            _worldMaterial = CreateWorldMaterial();
            _colorBlock = new MaterialPropertyBlock();
            _session = new TransitPrototypeSession();
            CreateWorld();
            RenderSnapshot();
        }

        private void Update()
        {
            _tickAccumulator += Time.deltaTime;
            while (_tickAccumulator >= TickSeconds)
            {
                _tickAccumulator -= TickSeconds;
                _session.AdvanceOneTick();
            }

            RenderSnapshot();
        }

        private void OnGUI()
        {
            EnsureGuiStyles();
            var snapshot = _session.Projection();
            GUILayout.BeginArea(new Rect(20, 20, 300, 180), GUI.skin.box);
            GUILayout.Label("MORNING COMMUTE", _headerStyle);
            GUILayout.Label($"Tick {snapshot.Tick}  •  {snapshot.Elevators.Count} elevator(s)", _bodyStyle);
            GUILayout.Label($"Lobby queue: {snapshot.QueueLength} residents", _bodyStyle);
            GUILayout.Label($"Arrived: {snapshot.ArrivedCount}/{TransitPrototypeSession.ResidentCount}", _bodyStyle);
            GUILayout.Label($"Average completed wait: {snapshot.AverageWaitTicks:F1} ticks", _bodyStyle);
            GUILayout.Space(8);
            if (GUILayout.Button("Add elevator capacity", _buttonStyle, GUILayout.Height(32)))
            {
                AddCapacity();
            }

            GUILayout.Label("Use the button to add capacity", _bodyStyle);
            GUILayout.EndArea();
        }

        private void AddCapacity()
        {
            _session.AddCapacity();
            EnsureElevatorViews();
        }

        private void EnsureElevatorViews()
        {
            while (_elevatorViews.Count < _session.Projection().Elevators.Count)
            {
                _elevatorViews.Add(CreateRectangle("Elevator Car", new Color(0.25f, 0.9f, 0.65f), Vector3.zero, new Vector2(0.95f, 0.42f), transform));
            }
        }

        private void CreateWorld()
        {
            var cameraObject = new GameObject("Prototype Camera");
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 6.2f;
            cameraComponent.backgroundColor = new Color(0.04f, 0.06f, 0.1f);

            for (var floor = 0; floor < TransitPrototypeSession.FloorCount; floor++)
            {
                var y = FloorY(floor);
                CreateRectangle($"Floor {floor + 1}", new Color(0.12f, 0.17f, 0.25f), new Vector3(1.1f, y, 1), new Vector2(10.4f, 1.55f), transform);
                CreateRectangle($"Floor Line {floor + 1}", new Color(0.38f, 0.46f, 0.58f), new Vector3(1.1f, y - 0.72f, 0), new Vector2(10.4f, 0.05f), transform);
            }

            CreateRectangle("Elevator Shaft", new Color(0.06f, 0.1f, 0.16f), new Vector3(-0.8f, 0.2f, 0), new Vector2(2.25f, 9.2f), transform);
            CreateRectangle("Lobby", new Color(0.17f, 0.25f, 0.34f), new Vector3(-4.15f, FloorY(0), 0), new Vector2(3.15f, 1.35f), transform);

            for (var resident = 0; resident < TransitPrototypeSession.ResidentCount; resident++)
            {
                _residentViews.Add(CreateRectangle($"Resident {resident + 1}", ResidentColor(resident), Vector3.zero, new Vector2(0.2f, 0.2f), transform));
            }

            EnsureElevatorViews();
        }

        private void RenderSnapshot()
        {
            var snapshot = _session.Projection();
            var queuedIndex = 0;
            var floorResidentCounts = new int[TransitPrototypeSession.FloorCount];
            for (var elevatorIndex = 0; elevatorIndex < snapshot.Elevators.Count; elevatorIndex++)
            {
                var elevator = snapshot.Elevators[elevatorIndex];
                _elevatorViews[elevatorIndex].transform.position = new Vector3(-1.35f + elevatorIndex * 1.05f, FloorY(elevator.Floor), -1);
            }

            for (var residentIndex = 0; residentIndex < snapshot.Residents.Count; residentIndex++)
            {
                var resident = snapshot.Residents[residentIndex];
                var view = _residentViews[residentIndex];
                switch (resident.Status)
                {
                    case TransitResidentStatus.Queued:
                        view.transform.position = new Vector3(-5.3f + queuedIndex % 9 * 0.3f, -3.95f + queuedIndex / 9 * 0.25f, -2);
                        queuedIndex++;
                        break;
                    case TransitResidentStatus.Riding:
                        var elevatorIndex = FindPassengerElevator(snapshot, resident.ResidentId);
                        var elevator = snapshot.Elevators[elevatorIndex];
                        view.transform.position = new Vector3(-1.55f + elevatorIndex * 1.05f + residentIndex % 3 * 0.18f, FloorY(elevator.Floor), -2);
                        break;
                    case TransitResidentStatus.Arrived:
                        var floor = resident.DestinationFloor;
                        var floorIndex = floorResidentCounts[floor]++;
                        view.transform.position = new Vector3(0.75f + floorIndex % 12 * 0.42f, FloorY(floor) - 0.12f + floorIndex / 12 * 0.31f, -2);
                        break;
                }
            }
        }

        private int FindPassengerElevator(TransitPrototypeProjection snapshot, int residentId)
        {
            // The prototype has no passenger manifest projection yet. Assign riders deterministically to a visible active car.
            return residentId % snapshot.Elevators.Count;
        }

        private MeshRenderer CreateRectangle(string objectName, Color color, Vector3 position, Vector2 size, Transform parent)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 0.1f);
            Destroy(gameObject.GetComponent<BoxCollider>());
            var renderer = gameObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _worldMaterial;
            _colorBlock.SetColor("_BaseColor", color);
            _colorBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(_colorBlock);
            return renderer;
        }

        private static Material CreateWorldMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                throw new MissingReferenceException("No unlit shader is available for the transit prototype.");
            }

            return new Material(shader);
        }

        private void EnsureGuiStyles()
        {
            if (_headerStyle != null)
            {
                return;
            }

            _headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = new Color(0.82f, 0.9f, 1f) } };
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
        }

        private static float FloorY(int floor) => -3.2f + floor * 1.85f;

        private static Color ResidentColor(int residentIndex) => residentIndex % 3 == 0 ? new Color(1f, 0.65f, 0.32f) : residentIndex % 3 == 1 ? new Color(0.38f, 0.78f, 1f) : new Color(0.95f, 0.42f, 0.7f);
    }
}
