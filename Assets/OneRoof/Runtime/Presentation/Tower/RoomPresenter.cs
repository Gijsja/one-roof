using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Architecture;
using OneRoof.Presentation.Furnishings;
using UnityEngine;
using EntityId = OneRoof.Domain.Identity.EntityId;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Deep presenter responsible for room architectural backdrops, furnishings, dividing walls,
    /// and dynamic room creation/demolition views.
    /// </summary>
    public sealed class RoomPresenter
    {
        private Transform _parent;
        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;

        private readonly HashSet<EntityId> _renderedRoomIds = new HashSet<EntityId>();
        private readonly List<GameObject> _roomObjects = new List<GameObject>();
        private readonly Dictionary<EntityId, GameObject> _roomRoots = new Dictionary<EntityId, GameObject>();
        private readonly Dictionary<EntityId, RoomFurnishingPresenter> _furnishings = new Dictionary<EntityId, RoomFurnishingPresenter>();
        private readonly HashSet<EntityId> _authoredRoomIds = new HashSet<EntityId>();

        public IReadOnlyCollection<EntityId> RenderedRoomIds => _renderedRoomIds;

        public static bool TryCalculateRoomBounds(Room room, out Bounds bounds)
        {
            if (room == null)
            {
                bounds = default;
                return false;
            }

            var worldLeft = -2.4f + room.Bounds.MinX * 0.5f;
            var worldRight = -2.4f + (room.Bounds.MaxX + 1) * 0.5f;
            var width = worldRight - worldLeft;
            var centerX = (worldLeft + worldRight) * 0.5f;
            var y = TowerStructurePresenter.FloorY(room.Floor);
            bounds = new Bounds(new Vector3(centerX, y, 0f), new Vector3(width, 1.48f, 1f));
            return true;
        }

        public bool TryGetRoomBounds(EntityId roomId, TowerTopologyProjection topology, out Bounds bounds)
        {
            if (topology != null && topology.TryGetRoom(roomId, out var room))
            {
                return TryCalculateRoomBounds(room, out bounds);
            }

            bounds = default;
            return false;
        }

        public bool TryGetRoomAt(Vector2 worldPos, TowerTopologyProjection topology, out EntityId roomId, out Bounds bounds)
        {
            if (topology != null)
            {
                foreach (var room in topology.Rooms.Values)
                {
                    var contentType = room.ContentType.Value ?? "";
                    if (contentType.Equals("transit:elevator_shaft") || contentType.Equals("elevator_shaft"))
                    {
                        continue;
                    }

                    if (TryCalculateRoomBounds(room, out var b))
                    {
                        if (worldPos.x >= b.min.x && worldPos.x <= b.max.x &&
                            worldPos.y >= b.min.y && worldPos.y <= b.max.y)
                        {
                            roomId = room.Id;
                            bounds = b;
                            return true;
                        }
                    }
                }
            }

            roomId = default;
            bounds = default;
            return false;
        }

        public void Initialize(Transform parent, Material worldMaterial, MaterialPropertyBlock colorBlock)
        {
            _parent = parent;
            _worldMaterial = worldMaterial;
            _colorBlock = colorBlock;
            _renderedRoomIds.Clear();
            _roomObjects.Clear();
            _roomRoots.Clear();
            _authoredRoomIds.Clear();
        }

        public void EnsureRoomViews(TowerTopologyProjection topology)
        {
            if (topology == null) return;

            // Remove views for rooms that were demolished
            var activeRoomIds = new HashSet<EntityId>(topology.Rooms.Keys);
            var demolishedIds = new List<EntityId>();
            foreach (var id in _renderedRoomIds)
            {
                if (!activeRoomIds.Contains(id))
                {
                    demolishedIds.Add(id);
                    if (_roomRoots.TryGetValue(id, out var root) && root != null)
                    {
                        _roomObjects.Remove(root);
                        if (UnityEngine.Application.isPlaying)
                        {
                            root.name = $"DemolishingRoom_{id}";
                            root.AddComponent<VisualEffectsPresenter>().BeginDemolition();
                        }
                        else UnityEngine.Object.DestroyImmediate(root);
                    }
                    else if (_parent != null)
                    {
                        var child = _parent.Find($"RoomView_{id}");
                        if (child != null)
                        {
                            _roomObjects.Remove(child.gameObject);
                            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(child.gameObject);
                            else UnityEngine.Object.DestroyImmediate(child.gameObject);
                        }
                    }
                }
            }
            foreach (var id in demolishedIds)
            {
                _renderedRoomIds.Remove(id);
                _roomRoots.Remove(id);
                _furnishings.Remove(id);
            }

            foreach (var room in topology.Rooms.Values)
            {
                if (_renderedRoomIds.Contains(room.Id)) continue;
                _renderedRoomIds.Add(room.Id);

                var contentTypeStr = room.ContentType.Value ?? "";
                if (contentTypeStr.Equals("transit:elevator_shaft") || contentTypeStr.Equals("elevator_shaft"))
                {
                    continue; // shaft rendered via ElevatorBankPresenter
                }

                var authoredRoot = _parent != null ? _parent.Find($"RoomView_{room.Id}") : null;
                if (authoredRoot != null)
                {
                    _roomRoots[room.Id] = authoredRoot.gameObject;
                    _roomObjects.Add(authoredRoot.gameObject);
                    _authoredRoomIds.Add(room.Id);
                    var authoredFurnishings = authoredRoot.GetComponent<RoomFurnishingPresenter>();
                    if (authoredFurnishings != null) _furnishings[room.Id] = authoredFurnishings;
                    continue;
                }

                var worldLeft = -2.4f + room.Bounds.MinX * 0.5f;
                var worldRight = -2.4f + (room.Bounds.MaxX + 1) * 0.5f;
                var width = worldRight - worldLeft;
                var centerX = (worldLeft + worldRight) * 0.5f;
                var y = TowerStructurePresenter.FloorY(room.Floor);

                var isResidential = contentTypeStr.StartsWith("residential") || contentTypeStr.StartsWith("room:apartment");
                var isOffice = contentTypeStr.Equals("commercial:office", StringComparison.OrdinalIgnoreCase) || contentTypeStr.Contains("office");
                var isRetail = contentTypeStr.Contains("retail");
                var isClinic = contentTypeStr.Contains("clinic");
                var isMaintenance = contentTypeStr.Contains("maintenance");
                var isSecurity = contentTypeStr.Contains("security");
                var isUtility = contentTypeStr.Contains("utility");
                var isDiner = (contentTypeStr.StartsWith("commercial") || contentTypeStr.StartsWith("room:diner")) && !isOffice && !isRetail;
                var isStairwell = contentTypeStr.Equals("amenity:stairwell", StringComparison.OrdinalIgnoreCase) || contentTypeStr.Contains("stairwell");
                var isLobby = contentTypeStr.Contains("lobby");
                var isWestSide = centerX < -1.9f;

                var roomRoot = new GameObject($"RoomView_{room.Id}");
                if (_parent != null) roomRoot.transform.SetParent(_parent, false);
                roomRoot.transform.position = new Vector3(centerX, y, 0f);
                _roomRoots[room.Id] = roomRoot;
                _roomObjects.Add(roomRoot);
                roomRoot.AddComponent<VisualEffectsPresenter>().BeginConstruction();

                if (isResidential || isOffice || isDiner || isLobby || isRetail || isClinic || isMaintenance || isSecurity || isUtility)
                {
                    var backdropPresenter = roomRoot.AddComponent<RoomBackdropPresenter>();
                    backdropPresenter.Setup(contentTypeStr, width, 1.42f, worldLeft, worldRight, y, isWestSide);

                    var furnishingPresenter = roomRoot.AddComponent<RoomFurnishingPresenter>();
                    furnishingPresenter.FurnishRoom(contentTypeStr, width, 1.42f, isWestSide);
                    _furnishings[room.Id] = furnishingPresenter;
                }

                if (isResidential)
                {
                    var doorX = isWestSide ? (worldRight - 0.35f) : (worldLeft + 0.35f);
                    CreateQuad($"Apt Tag {room.Id}", new Color(0.30f, 0.42f, 0.56f), new Vector3(doorX, y + 0.25f, 0.42f), new Vector2(0.28f, 0.09f), roomRoot.transform);
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.38f, 0.48f, 0.62f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.38f, 0.48f, 0.62f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else if (isOffice)
                {
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else if (isDiner)
                {
                    CreateQuad($"Diner Awning {room.Id}", new Color(0.85f, 0.42f, 0.25f), new Vector3(centerX, y + 0.58f, 0.55f), new Vector2(1.3f, 0.16f), roomRoot.transform);
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else if (isStairwell)
                {
                    var stairBackdrop = roomRoot.AddComponent<RoomBackdropPresenter>();
                    stairBackdrop.Setup(contentTypeStr, width, 1.42f, worldLeft, worldRight, y, isWestSide);
                    CreateQuad($"Stair Tread 1 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX - 0.30f, y - 0.45f, 0.55f), new Vector2(0.35f, 0.06f), roomRoot.transform);
                    CreateQuad($"Stair Tread 2 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX - 0.10f, y - 0.15f, 0.55f), new Vector2(0.35f, 0.06f), roomRoot.transform);
                    CreateQuad($"Stair Tread 3 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX + 0.10f, y + 0.15f, 0.55f), new Vector2(0.35f, 0.06f), roomRoot.transform);
                    CreateQuad($"Stair Tread 4 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX + 0.30f, y + 0.45f, 0.55f), new Vector2(0.35f, 0.06f), roomRoot.transform);
                    CreateQuad($"Stair Rail {room.Id}", new Color(0.88f, 0.76f, 0.30f), new Vector3(centerX, y, 0.50f), new Vector2(width * 0.75f, 0.04f), roomRoot.transform);
                    CreateExitSign(room.Id, centerX, y, roomRoot.transform);
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.42f, 0.50f, 0.62f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.42f, 0.50f, 0.62f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else if (isLobby)
                {
                    CreateQuad($"Lobby Desk {room.Id}", new Color(0.48f, 0.58f, 0.70f), new Vector3(centerX - 1.2f, y - 0.46f, 0.55f), new Vector2(1.2f, 0.30f), roomRoot.transform);
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else if (isRetail)
                {
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.50f, 0.64f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.50f, 0.64f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else if (isClinic)
                {
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.55f, 0.62f, 0.64f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.55f, 0.62f, 0.64f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else if (isMaintenance)
                {
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.40f, 0.44f, 0.50f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.40f, 0.44f, 0.50f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else if (isSecurity)
                {
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.30f, 0.36f, 0.48f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.30f, 0.36f, 0.48f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else if (isUtility)
                {
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.32f, 0.36f, 0.42f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.32f, 0.36f, 0.42f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f), roomRoot.transform);
                }
                else
                {
                    CreateQuad($"Room {room.Id} ({contentTypeStr})", new Color(0.18f, 0.24f, 0.32f), new Vector3(centerX, y, 0.6f), new Vector2(width - 0.08f, 1.4f), roomRoot.transform);
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.35f, 0.45f, 0.58f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.45f), roomRoot.transform);
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.35f, 0.45f, 0.58f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.45f), roomRoot.transform);
                }
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _roomObjects.Count; i++)
            {
                var go = _roomObjects[i];
                if (go != null && !_authoredRoomIds.Contains(GetRoomId(go.name)))
                {
                    if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(go);
                    else UnityEngine.Object.DestroyImmediate(go);
                }
            }

            _roomObjects.Clear();
            _roomRoots.Clear();
            _furnishings.Clear();
            _renderedRoomIds.Clear();
            _authoredRoomIds.Clear();
        }

        private static EntityId GetRoomId(string name)
        {
            return name.StartsWith("RoomView_") && int.TryParse(name.Substring("RoomView_".Length), out var value)
                ? new EntityId(value) : default;
        }

        public bool TryGetInteractionDock(Room room, InteractionPointKind kind, int slot, out Vector3 worldPosition)
        {
            if (room != null)
            {
                var matching = new List<InteractionPoint>();
                for (var i = 0; i < room.InteractionPoints.Count; i++)
                    if (room.InteractionPoints[i].Kind == kind) matching.Add(room.InteractionPoints[i]);
                if (matching.Count > 0)
                {
                    var point = matching[Mathf.Abs(slot) % matching.Count];
                    var x = -2.4f + (room.Bounds.MinX + point.LocalCellOffset + 0.5f) * 0.5f;
                    worldPosition = new Vector3(x, TowerStructurePresenter.FloorY(room.Floor) - 0.58f, -0.2f);
                    return true;
                }
                if (_furnishings.TryGetValue(room.Id, out var furnishings) && furnishings != null && furnishings.TryGetDockPosition(kind, slot, out worldPosition))
                    return true;
            }
            worldPosition = default;
            return false;
        }

        private void CreateExitSign(EntityId roomId, float centerX, float y, Transform parent)
        {
            var signObj = new GameObject($"Stair Exit Sign {roomId}");
            if (parent != null) signObj.transform.SetParent(parent, false);
            signObj.transform.position = new Vector3(centerX, y + 0.58f, 0.48f);

            var renderer = signObj.AddComponent<SpriteRenderer>();
            renderer.sprite = ArchitecturalFixtureCatalog.GetFixture(ArchitecturalFixtureCatalog.ExitSign);
            renderer.sortingOrder = -4;

            _roomObjects.Add(signObj);
        }

        private MeshRenderer CreateQuad(string name, Color color, Vector3 position, Vector2 size, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            var targetParent = parent ?? _parent;
            if (targetParent != null) go.transform.SetParent(targetParent, true);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(col);
                else UnityEngine.Object.DestroyImmediate(col);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _worldMaterial;
            if (_colorBlock != null)
            {
                _colorBlock.SetColor("_BaseColor", color);
                _colorBlock.SetColor("_Color", color);
                renderer.SetPropertyBlock(_colorBlock);
            }

            _roomObjects.Add(go);
            return renderer;
        }
    }
}
