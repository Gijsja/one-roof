using System;
using System.Collections.Generic;
using OneRoof.Application.Transit;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using UnityEngine;
using UnityEngine.Rendering;
using EntityId = OneRoof.Domain.Identity.EntityId;
using UnityApplication = UnityEngine.Application;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Presentation-only spatial sound and window-light layer. It consumes projections and
    /// topology, keeping all audio timing and visual atmosphere out of the simulation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TowerAtmospherePresenter : MonoBehaviour
    {
        private readonly Dictionary<EntityId, AudioSource> _roomTones = new Dictionary<EntityId, AudioSource>();
        private readonly List<GameObject> _windowVolumes = new List<GameObject>();
        private readonly List<MeshRenderer> _windowLightRenderers = new List<MeshRenderer>();
        private readonly List<Material> _windowMaterials = new List<Material>();
        private readonly List<Mesh> _generatedMeshes = new List<Mesh>();
        private readonly List<AudioClip> _generatedClips = new List<AudioClip>();
        private readonly Dictionary<int, int> _lastElevatorFloor = new Dictionary<int, int>();

        private AudioSource _elevatorFoley;
        private AudioSource _footstepFoley;
        private long _lastFootstepTick = -1;
        private BuildingTopologyState _syncedTopology;
        private int _syncedRoomCount = -1;

        public int RoomToneSourceCount => _roomTones.Count;
        public int WindowLightCount => _windowVolumes.Count;
        public IReadOnlyList<MeshRenderer> WindowLightRenderers => _windowLightRenderers;
        public int LastFootstepResidentId { get; private set; } = -1;
        public AudioSource ElevatorFoley => _elevatorFoley;
        public AudioSource FootstepFoley => _footstepFoley;

        private void OnDestroy() => Clear();

        public void Initialize()
        {
            if (_elevatorFoley != null) return;

            _elevatorFoley = CreateSpatialSource("Elevator Mechanical Foley", 0.22f, 14f);
            _elevatorFoley.clip = CreateTone("elevator-hum", 58f, 1.2f, 0.11f);
            _elevatorFoley.loop = true;

            _footstepFoley = CreateSpatialSource("Resident Footstep Foley", 0.38f, 5f);
            _footstepFoley.clip = CreateTone("footstep", 170f, 0.08f, 0.18f);
        }

        public void UpdateSoundscape(TowerProjection snapshot, BuildingTopologyState topology)
        {
            if (snapshot == null || topology == null) return;
            Initialize();
            SyncRoomTonesAndWindowLighting(topology);
            UpdateElevatorFoley(snapshot);
            UpdateFootsteps(snapshot);
        }

        public void Clear()
        {
            foreach (var source in _roomTones.Values) DestroyUnityObject(source != null ? source.gameObject : null);
            _roomTones.Clear();
            foreach (var volume in _windowVolumes) DestroyUnityObject(volume);
            _windowVolumes.Clear();
            _windowLightRenderers.Clear();
            foreach (var material in _windowMaterials) DestroyUnityObject(material);
            _windowMaterials.Clear();
            foreach (var mesh in _generatedMeshes) DestroyUnityObject(mesh);
            _generatedMeshes.Clear();
            DestroyUnityObject(_elevatorFoley != null ? _elevatorFoley.gameObject : null);
            DestroyUnityObject(_footstepFoley != null ? _footstepFoley.gameObject : null);
            _elevatorFoley = null;
            _footstepFoley = null;
            foreach (var clip in _generatedClips) DestroyUnityObject(clip);
            _generatedClips.Clear();
            _lastElevatorFloor.Clear();
            _lastFootstepTick = -1;
            _syncedTopology = null;
            _syncedRoomCount = -1;
            LastFootstepResidentId = -1;
        }

        private void SyncRoomTonesAndWindowLighting(BuildingTopologyState topology)
        {
            if (ReferenceEquals(_syncedTopology, topology) && _syncedRoomCount == topology.Rooms.Count) return;
            var active = new HashSet<EntityId>(topology.Rooms.Keys);
            var removed = new List<EntityId>();
            foreach (var pair in _roomTones)
                if (!active.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (var id in removed)
            {
                DestroyUnityObject(_roomTones[id].gameObject);
                _roomTones.Remove(id);
            }

            foreach (var room in topology.Rooms.Values)
            {
                if (IsTransitRoom(room)) continue;
                if (!_roomTones.TryGetValue(room.Id, out var source) || source == null)
                {
                    source = CreateSpatialSource($"Roomtone {room.Id}", RoomVolume(room), 7f);
                    source.clip = CreateTone($"roomtone-{room.Id}", RoomFrequency(room), 1.6f, RoomVolume(room));
                    source.loop = true;
                    _roomTones[room.Id] = source;
                    CreateWindowVolume(room);
                }
                source.transform.position = RoomCenter(room, -0.05f);
                if (UnityApplication.isPlaying && !source.isPlaying) source.Play();
            }
            _syncedTopology = topology;
            _syncedRoomCount = topology.Rooms.Count;
        }

        private void UpdateElevatorFoley(TowerProjection snapshot)
        {
            if (snapshot.Elevators.Count == 0) return;
            var elevator = snapshot.Elevators[0];
            _elevatorFoley.transform.position = new Vector3(-1.9f, TowerStructurePresenter.FloorY(elevator.Floor), -0.1f);
            var moved = _lastElevatorFloor.TryGetValue(elevator.ElevatorId, out var previousFloor) && previousFloor != elevator.Floor;
            _lastElevatorFloor[elevator.ElevatorId] = elevator.Floor;
            _elevatorFoley.pitch = moved ? 1.18f : 0.88f;
            _elevatorFoley.volume = 0.10f + (elevator.Capacity > 0 ? 0.12f * elevator.PassengerCount / elevator.Capacity : 0f);
            if (UnityApplication.isPlaying && !_elevatorFoley.isPlaying) _elevatorFoley.Play();
        }

        private void UpdateFootsteps(TowerProjection snapshot)
        {
            if (_lastFootstepTick == snapshot.Tick) return;
            for (var i = 0; i < snapshot.Residents.Count; i++)
            {
                var resident = snapshot.Residents[i];
                if (resident.Status != TransitResidentStatus.Walking) continue;
                _lastFootstepTick = snapshot.Tick;
                LastFootstepResidentId = resident.ResidentId;
                _footstepFoley.transform.position = new Vector3(-2.4f + resident.CellX * 0.5f + 0.25f, TowerStructurePresenter.FloorY(resident.Floor) - 0.58f, -0.1f);
                if (UnityApplication.isPlaying) _footstepFoley.PlayOneShot(_footstepFoley.clip, 0.8f);
                return;
            }
        }

        private void CreateWindowVolume(Room room)
        {
            var go = new GameObject($"Volumetric Window Light {room.Id}");
            go.name = $"Volumetric Window Light {room.Id}";
            go.transform.SetParent(transform, false);
            // The camera looks along +Z. Keep the cone in front of the backdrop
            // (z=0.7) so its transparent light cannot be hidden by room geometry.
            go.transform.position = new Vector3(0f, 0f, 0.18f);
            var mesh = CreateWindowLightCone(room);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new MissingReferenceException("No unlit shader found for window-light volume.");
            var material = new Material(shader);
            material.color = new Color(1f, 0.77f, 0.36f, 0.34f);
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Transparent;
            renderer.sharedMaterial = material;
            _windowMaterials.Add(material);
            _windowVolumes.Add(go);
            _windowLightRenderers.Add(renderer);
            _generatedMeshes.Add(mesh);
        }

        private static Mesh CreateWindowLightCone(Room room)
        {
            var worldLeft = -2.4f + room.Bounds.MinX * 0.5f;
            var worldRight = -2.4f + (room.Bounds.MaxX + 1) * 0.5f;
            var centerY = TowerStructurePresenter.FloorY(room.Floor);
            var isWestSide = (worldLeft + worldRight) * 0.5f < -1.9f;
            var windowX = isWestSide ? worldLeft + 0.55f : worldRight - 0.55f;
            var inwardDirection = isWestSide ? 1f : -1f;
            var depth = Mathf.Min(1.25f, room.Bounds.Width * 0.22f);

            var mesh = new Mesh { name = $"WindowLightCone_{room.Id}" };
            mesh.vertices = new[]
            {
                new Vector3(windowX - 0.20f, centerY + 0.22f, 0f),
                new Vector3(windowX + 0.20f, centerY + 0.22f, 0f),
                new Vector3(windowX + inwardDirection * depth + 0.62f, centerY - 0.56f, 0f),
                new Vector3(windowX + inwardDirection * depth - 0.62f, centerY - 0.56f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private AudioSource CreateSpatialSource(string name, float volume, float maxDistance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 0.8f;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
            source.playOnAwake = false;
            source.volume = volume;
            return source;
        }

        private AudioClip CreateTone(string name, float frequency, float seconds, float amplitude)
        {
            const int sampleRate = 22050;
            var sampleCount = Mathf.CeilToInt(sampleRate * seconds);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var envelope = Mathf.Clamp01(Mathf.Min(i / (sampleRate * 0.02f), (sampleCount - i) / (sampleRate * 0.04f)));
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * amplitude * envelope;
            }
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.DontSave;
            _generatedClips.Add(clip);
            return clip;
        }

        private static bool IsTransitRoom(Room room) => room.ContentType.Value != null && room.ContentType.Value.Contains("elevator_shaft");
        private static float RoomVolume(Room room) => room.ContentType.Value != null && room.ContentType.Value.Contains("diner") ? 0.11f : 0.055f;
        private static float RoomFrequency(Room room) => room.ContentType.Value != null && room.ContentType.Value.Contains("office") ? 106f : room.ContentType.Value != null && room.ContentType.Value.Contains("diner") ? 142f : 83f;
        private static Vector3 RoomCenter(Room room, float yOffset) => new Vector3(-2.4f + (room.Bounds.MinX + room.Bounds.MaxX + 1) * 0.25f, TowerStructurePresenter.FloorY(room.Floor) + yOffset, 0.7f);

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (UnityApplication.isPlaying) UnityEngine.Object.Destroy(target); else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
