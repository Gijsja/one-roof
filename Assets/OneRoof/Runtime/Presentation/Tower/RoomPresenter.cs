using System;
using System.Collections.Generic;
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

        public IReadOnlyCollection<EntityId> RenderedRoomIds => _renderedRoomIds;

        public void Initialize(Transform parent, Material worldMaterial, MaterialPropertyBlock colorBlock)
        {
            _parent = parent;
            _worldMaterial = worldMaterial;
            _colorBlock = colorBlock;
            _renderedRoomIds.Clear();
            _roomObjects.Clear();
        }

        public void EnsureRoomViews(BuildingTopologyState topology)
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
                    if (_parent != null)
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

                var worldLeft = -2.4f + room.Bounds.MinX * 0.5f;
                var worldRight = -2.4f + (room.Bounds.MaxX + 1) * 0.5f;
                var width = worldRight - worldLeft;
                var centerX = (worldLeft + worldRight) * 0.5f;
                var y = TowerStructurePresenter.FloorY(room.Floor);

                var isResidential = contentTypeStr.StartsWith("residential") || contentTypeStr.StartsWith("room:apartment");
                var isOffice = contentTypeStr.Equals("commercial:office", StringComparison.OrdinalIgnoreCase) || contentTypeStr.Contains("office");
                var isDiner = (contentTypeStr.StartsWith("commercial") || contentTypeStr.StartsWith("room:diner")) && !isOffice;
                var isStairwell = contentTypeStr.Equals("amenity:stairwell", StringComparison.OrdinalIgnoreCase) || contentTypeStr.Contains("stairwell");
                var isLobby = contentTypeStr.Contains("lobby");
                var isWestSide = centerX < -1.9f;

                if (isResidential || isOffice || isDiner || isLobby)
                {
                    var roomObj = new GameObject($"RoomView_{room.Id}");
                    if (_parent != null) roomObj.transform.SetParent(_parent, false);
                    roomObj.transform.position = new Vector3(centerX, y, 0f);

                    var backdropPresenter = roomObj.AddComponent<RoomBackdropPresenter>();
                    backdropPresenter.Setup(contentTypeStr, width, 1.42f, worldLeft, worldRight, y, isWestSide);

                    var furnishingPresenter = roomObj.AddComponent<RoomFurnishingPresenter>();
                    furnishingPresenter.FurnishRoom(contentTypeStr, width, 1.42f, isWestSide);

                    _roomObjects.Add(roomObj);
                }

                if (isResidential)
                {
                    var doorX = isWestSide ? (worldRight - 0.35f) : (worldLeft + 0.35f);
                    CreateQuad($"Apt Tag {room.Id}", new Color(0.30f, 0.42f, 0.56f), new Vector3(doorX, y + 0.25f, 0.42f), new Vector2(0.28f, 0.09f));
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.38f, 0.48f, 0.62f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f));
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.38f, 0.48f, 0.62f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f));
                }
                else if (isOffice)
                {
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f));
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f));
                }
                else if (isDiner)
                {
                    CreateQuad($"Diner Awning {room.Id}", new Color(0.85f, 0.42f, 0.25f), new Vector3(centerX, y + 0.58f, 0.55f), new Vector2(1.3f, 0.16f));
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f));
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f));
                }
                else if (isStairwell)
                {
                    CreateQuad($"Stair Bg {room.Id}", new Color(0.12f, 0.16f, 0.22f), new Vector3(centerX, y, 0.7f), new Vector2(width - 0.04f, 1.45f));
                    CreateQuad($"Stair Tread 1 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX - 0.30f, y - 0.45f, 0.55f), new Vector2(0.35f, 0.06f));
                    CreateQuad($"Stair Tread 2 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX - 0.10f, y - 0.15f, 0.55f), new Vector2(0.35f, 0.06f));
                    CreateQuad($"Stair Tread 3 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX + 0.10f, y + 0.15f, 0.55f), new Vector2(0.35f, 0.06f));
                    CreateQuad($"Stair Tread 4 {room.Id}", new Color(0.55f, 0.65f, 0.78f), new Vector3(centerX + 0.30f, y + 0.45f, 0.55f), new Vector2(0.35f, 0.06f));
                    CreateQuad($"Stair Rail {room.Id}", new Color(0.88f, 0.76f, 0.30f), new Vector3(centerX, y, 0.50f), new Vector2(width * 0.75f, 0.04f));
                    CreateQuad($"Stair Exit Sign {room.Id}", new Color(0.20f, 0.85f, 0.45f), new Vector3(centerX, y + 0.58f, 0.48f), new Vector2(0.26f, 0.10f));
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.42f, 0.50f, 0.62f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f));
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.42f, 0.50f, 0.62f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f));
                }
                else if (isLobby)
                {
                    CreateQuad($"Lobby Desk {room.Id}", new Color(0.48f, 0.58f, 0.70f), new Vector3(centerX - 1.2f, y - 0.46f, 0.55f), new Vector2(1.2f, 0.30f));
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.48f));
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.45f, 0.52f, 0.65f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.48f));
                }
                else
                {
                    CreateQuad($"Room {room.Id} ({contentTypeStr})", new Color(0.18f, 0.24f, 0.32f), new Vector3(centerX, y, 0.6f), new Vector2(width - 0.08f, 1.4f));
                    CreateQuad($"Room Wall L {room.Id}", new Color(0.35f, 0.45f, 0.58f), new Vector3(worldLeft, y, 0.3f), new Vector2(0.08f, 1.45f));
                    CreateQuad($"Room Wall R {room.Id}", new Color(0.35f, 0.45f, 0.58f), new Vector3(worldRight, y, 0.3f), new Vector2(0.08f, 1.45f));
                }
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _roomObjects.Count; i++)
            {
                var go = _roomObjects[i];
                if (go != null)
                {
                    if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(go);
                    else UnityEngine.Object.DestroyImmediate(go);
                }
            }

            _roomObjects.Clear();
            _renderedRoomIds.Clear();
        }

        private MeshRenderer CreateQuad(string name, Color color, Vector3 position, Vector2 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            if (_parent != null) go.transform.SetParent(_parent, false);
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
