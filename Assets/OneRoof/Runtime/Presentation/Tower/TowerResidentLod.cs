using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Population;
using OneRoof.Presentation.Population;
using UnityEngine;
using EntityId = OneRoof.Domain.Identity.EntityId;

namespace OneRoof.Presentation.Tower
{
    public sealed partial class TowerResidentPresenter
    {
        private readonly Dictionary<int, SpriteRenderer> _lodViews = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, float> _lodWalkPositions = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _lodHeadings = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _lodProjectionIndices = new Dictionary<int, int>();
        private readonly HashSet<int> _lodVisibleIds = new HashSet<int>();
        public int VisibleResidentCount => _lodVisibleIds.Count;
        private readonly HashSet<int> _lodActiveIds = new HashSet<int>();
        private readonly List<int> _lodRetiredIds = new List<int>();
        private readonly List<int> _rigCandidates = new List<int>();
        private readonly Dictionary<int, float> _rigScores = new Dictionary<int, float>();
        private readonly List<Vector3> _macroVertices = new List<Vector3>(4800);
        private readonly List<int> _macroTriangles = new List<int>(7200);
        private readonly List<Color> _macroColors = new List<Color>(4800);
        private Mesh _macroMesh;
        private GameObject _macroObject;
        private Material _lodMaterial;
        public int? InspectedResidentId { get; set; }
        public int VisibleSpriteCount { get; private set; }
        public int VisibleMacroCount { get; private set; }
        public int VisibleRigCount { get; private set; }
        public Mesh MacroMesh => _macroMesh;
        public Camera PresentationCamera { get => _camera; set => _camera = value; }

        private void PrepareLodViews(TowerProjection snapshot, TowerTopologyProjection topology, RoomPresenter rooms)
        {
            if (_camera == null) _camera = Camera.main;
            _lodActiveIds.Clear();
            _lodVisibleIds.Clear();
            _lodProjectionIndices.Clear();
            VisibleSpriteCount = VisibleMacroCount = VisibleRigCount = 0;
            _macroVertices.Clear(); _macroTriangles.Clear(); _macroColors.Clear();
            for (var i = 0; i < snapshot.Residents.Count; i++)
            {
                var resident = snapshot.Residents[i];
                _lodActiveIds.Add(resident.ResidentId);
                _lodProjectionIndices[resident.ResidentId] = i;
                if (_lodViews.TryGetValue(resident.ResidentId, out var existing)) continue;
                var root = new GameObject($"Resident LOD {resident.ResidentId}");
                root.transform.SetParent(_parent, false);
                var spriteObject = new GameObject("Single Sprite");
                spriteObject.transform.SetParent(root.transform, false);
                var renderer = spriteObject.AddComponent<SpriteRenderer>();
                renderer.sprite = ResidentSpriteCatalog.GetResidentSprite(resident.ResidentId - 1);
                renderer.sharedMaterial = GetLodMaterial();
                var record = ResidentSpriteCatalog.GetRecord(resident.ResidentId - 1);
                var height = record != null ? record.WorldHeight : 0.64f;
                var width = record != null ? record.WorldWidth : 0.32f;
                if (renderer.sprite != null)
                    renderer.transform.localScale = new Vector3(width / renderer.sprite.bounds.size.x,
                        height / renderer.sprite.bounds.size.y, 1f);
                root.transform.position = InitialLodPosition(resident, topology, rooms);
                renderer.enabled = false;
                _lodViews[resident.ResidentId] = renderer;
            }
            _lodRetiredIds.Clear();
            foreach (var pair in _lodViews)
                if (!_lodActiveIds.Contains(pair.Key)) _lodRetiredIds.Add(pair.Key);
            foreach (var id in _lodRetiredIds)
            {
                DestroyLodObject(_lodViews[id].transform.parent.gameObject);
                _lodViews.Remove(id);
                _lodWalkPositions.Remove(id); _lodHeadings.Remove(id);
            }
        }

        private static Vector3 InitialLodPosition(TransitResidentProjection resident, TowerTopologyProjection topology, RoomPresenter rooms)
        {
            if (resident.RoomId.HasValue && topology != null &&
                (resident.Status == TransitResidentStatus.InRoom || resident.Status == TransitResidentStatus.Arrived) &&
                topology.TryGetRoom(new EntityId(resident.RoomId.Value), out var room))
            {
                var kind = resident.Activity == ActivityKind.Sleeping ? OneRoof.Domain.Topology.InteractionPointKind.Sleep :
                    resident.Activity == ActivityKind.Working ? OneRoof.Domain.Topology.InteractionPointKind.Work : OneRoof.Domain.Topology.InteractionPointKind.Seat;
                if (rooms != null && rooms.TryGetInteractionDock(room, kind, resident.SlotInRoom, out var dock)) return dock;
                return new Vector3(-2.4f + (room.Bounds.MinX + room.Bounds.MaxX + 1) * 0.25f,
                    TowerStructurePresenter.FloorY(room.Floor) - 0.58f, -0.15f);
            }
            return new Vector3(-2.4f + resident.CellX * 0.5f + 0.25f,
                TowerStructurePresenter.FloorY(Mathf.Max(0, resident.Floor)) - 0.58f, -0.2f);
        }

        private bool IsLodVisible(Vector3 position)
        {
            if (_camera == null) return true;
            var viewport = _camera.WorldToViewportPoint(position + new Vector3(0f, 0.3f, 0f));
            return viewport.z > 0f && viewport.x >= -0.05f && viewport.x <= 1.05f &&
                viewport.y >= -0.05f && viewport.y <= 1.05f;
        }

        private void SelectRigResidents(TowerProjection snapshot, IReadOnlyCollection<int> undergroundCrewResidentIds)
        {
            _visibleResidentIds.Clear();
            _rigCandidates.Clear(); _rigScores.Clear();
            var close = _camera == null || _camera.orthographicSize < 8f;
            for (var i = 0; i < snapshot.Residents.Count; i++)
            {
                var resident = snapshot.Residents[i];
                if (resident.Status == TransitResidentStatus.Outside || ContainsResidentId(undergroundCrewResidentIds, resident.ResidentId)) continue;
                var selected = resident.ResidentId == InspectedResidentId;
                if (!close && !selected) continue;
                var position = _lodViews[resident.ResidentId].transform.parent.position;
                if (!IsLodVisible(position) && !selected) continue;
                var score = _camera == null ? i : ((Vector2)(position - _camera.transform.position)).sqrMagnitude;
                // Retain a rig near the allocation boundary to avoid shuffling at tiny zoom/pan changes.
                if (_viewPool.TryGetView(resident.ResidentId, out _)) score *= 0.8f;
                _rigScores[resident.ResidentId] = selected ? float.MinValue : score;
                _rigCandidates.Add(resident.ResidentId);
            }
            _rigCandidates.Sort((a, b) => { var order = _rigScores[a].CompareTo(_rigScores[b]); return order != 0 ? order : a.CompareTo(b); });
            var limit = Math.Min(_targetResidentCount, Math.Min(_viewPool.MaxCapacity, 60));
            for (var i = 0; i < _rigCandidates.Count && i < limit; i++) _visibleResidentIds.Add(_rigCandidates[i]);
        }

        private void PresentLodResident(TransitResidentProjection resident, Transform transform, NpcSkeletalHierarchy skeleton, float time)
        {
            var renderer = _lodViews[resident.ResidentId];
            var fallback = renderer.transform.parent;
            if (skeleton != null)
            {
                fallback.position = transform.position;
                fallback.localScale = new Vector3(Mathf.Sign(transform.localScale.x), 1f, 1f);
            }
            var visible = resident.Status != TransitResidentStatus.Outside && IsLodVisible(transform.position);
            renderer.enabled = false;
            if (visible) _lodVisibleIds.Add(resident.ResidentId);
            var rigAlpha = _camera == null ? 1f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.5f, 8f, _camera.orthographicSize));
            if (resident.ResidentId == InspectedResidentId) rigAlpha = 1f;
            if (skeleton != null)
            {
                skeleton.gameObject.SetActive(visible);
                if (skeleton.StatusPlateRenderer != null) { var tint = skeleton.StatusPlateRenderer.color; tint.a = rigAlpha; skeleton.StatusPlateRenderer.color = tint; }
                if (skeleton.EmoteRenderer != null) { var tint = skeleton.EmoteRenderer.color; tint.a = rigAlpha; skeleton.EmoteRenderer.color = tint; }
                foreach (var limb in skeleton.LimbRenderers.Values)
                    if (limb != null) { var tint = limb.color; tint.a = rigAlpha; limb.color = tint; }
                foreach (var slot in skeleton.WardrobeSlots.Values)
                    if (slot != null) { var tint = slot.color; tint.a = rigAlpha; slot.color = tint; }
                if (visible) VisibleRigCount++;
                if (!visible || rigAlpha >= 0.999f) return;
            }
            if (!visible) return;
            var projection = new OneRoof.Application.Population.NpcProjection(resident.ResidentId, resident.ResidentId,
                resident.Floor, resident.RoomId ?? 0, ToNpcActivity(resident.Activity),
                resident.Status == TransitResidentStatus.Queued || resident.Status == TransitResidentStatus.Riding,
                resident.DestinationFloor, resident.RoomId, resident.WaitTicks, resident.CellX);
            var color = Color.Lerp(Color.white, NpcView.ResolveNpcColor(projection), 0.35f);
            var bob = resident.Status == TransitResidentStatus.Walking ? Mathf.Sin(time * 12f + resident.ResidentId) * 0.018f : 0f;
            renderer.transform.localPosition = new Vector3(0f, bob, 0f);
            var macroAlpha = _camera == null ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(16f, 18f, _camera.orthographicSize));
            var spriteAlpha = (skeleton != null ? 1f - rigAlpha : 1f) * (1f - macroAlpha);
            if (spriteAlpha > 0.001f)
            {
                color.a = spriteAlpha;
                renderer.color = color;
                renderer.enabled = true;
                VisibleSpriteCount++;
            }
            if (macroAlpha > 0.001f && skeleton == null)
            {
                color.a = macroAlpha;
                AddMacroResident(transform.position + new Vector3(0f, bob, 0f), color);
                VisibleMacroCount++;
            }
        }

        private void AddMacroResident(Vector3 position, Color color)
        {
            // A readable head, body and two legs at distant zoom; every glyph follows the same interior pose path as rigs.
            AddMacroQuad(position, -0.07f, 0.07f, 0.44f, 0.60f, new Color(0.88f, 0.65f, 0.48f, color.a));
            AddMacroQuad(position, -0.12f, 0.12f, 0.20f, 0.44f, color);
            AddMacroQuad(position, -0.09f, -0.015f, 0f, 0.20f, color * 0.7f);
            AddMacroQuad(position, 0.015f, 0.09f, 0f, 0.20f, color * 0.7f);
        }

        private void AddMacroQuad(Vector3 p, float left, float right, float bottom, float top, Color color)
        {
            var start = _macroVertices.Count;
            _macroVertices.Add(_parent.InverseTransformPoint(p + new Vector3(left, bottom, 0f))); _macroVertices.Add(_parent.InverseTransformPoint(p + new Vector3(right, bottom, 0f)));
            _macroVertices.Add(_parent.InverseTransformPoint(p + new Vector3(right, top, 0f))); _macroVertices.Add(_parent.InverseTransformPoint(p + new Vector3(left, top, 0f)));
            for (var i = 0; i < 4; i++) _macroColors.Add(color);
            _macroTriangles.Add(start); _macroTriangles.Add(start + 2); _macroTriangles.Add(start + 1);
            _macroTriangles.Add(start); _macroTriangles.Add(start + 3); _macroTriangles.Add(start + 2);
        }

        private void FinishLodFrame()
        {
            if (_macroObject == null)
            {
                _macroObject = new GameObject("Resident Macro LOD Batch");
                _macroObject.transform.SetParent(_parent, false);
                _macroMesh = new Mesh { name = "Resident Macro LOD" }; _macroMesh.MarkDynamic();
                _macroObject.AddComponent<MeshFilter>().sharedMesh = _macroMesh;
                _macroObject.AddComponent<MeshRenderer>().sharedMaterial = GetLodMaterial();
            }
            _macroMesh.Clear();
            _macroMesh.SetVertices(_macroVertices); _macroMesh.SetTriangles(_macroTriangles, 0); _macroMesh.SetColors(_macroColors);
            _macroMesh.RecalculateBounds();
            _macroObject.transform.localPosition = Vector3.zero; _macroObject.transform.localRotation = Quaternion.identity;
            _macroObject.transform.localScale = Vector3.one;
            _macroObject.SetActive(VisibleMacroCount > 0);
        }

        private Material GetLodMaterial()
        {
            if (_lodMaterial != null) return _lodMaterial;
            _lodMaterial = new Material(Shader.Find("OneRoof/Unlit") ?? Shader.Find("Sprites/Default")) { name = "Resident LOD", enableInstancing = true };
            _lodMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lodMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lodMaterial.SetInt("_ZWrite", 0); _lodMaterial.renderQueue = 3000;
            return _lodMaterial;
        }

        private void ClearLodViews()
        {
            foreach (var renderer in _lodViews.Values) if (renderer != null) DestroyLodObject(renderer.transform.parent.gameObject);
            _lodViews.Clear(); _lodProjectionIndices.Clear();
            _lodWalkPositions.Clear(); _lodHeadings.Clear();
            DestroyLodObject(_macroObject); _macroObject = null;
            DestroyLodObject(_macroMesh); _macroMesh = null;
            DestroyLodObject(_lodMaterial); _lodMaterial = null;
            VisibleRigCount = VisibleSpriteCount = VisibleMacroCount = 0;
            _lodVisibleIds.Clear();
        }

        private float LodHeading(int id, float x, float initialHeading)
        {
            var heading = _lodHeadings.TryGetValue(id, out var previousHeading) ? previousHeading : initialHeading;
            if (_lodWalkPositions.TryGetValue(id, out var previousX))
            {
                if (x > previousX + 0.001f) heading = 1f;
                else if (x < previousX - 0.001f) heading = -1f;
            }
            _lodWalkPositions[id] = x; _lodHeadings[id] = heading;
            return heading;
        }

        private bool TryGetLodView(int index, out Bounds bounds, out Sprite sprite, out Transform residentTransform)
        {
            foreach (var pair in _lodProjectionIndices)
            {
                if (pair.Value != index || !_lodVisibleIds.Contains(pair.Key)) continue;
                var renderer = _lodViews[pair.Key];
                residentTransform = renderer.transform.parent;
                sprite = renderer.sprite;
                bounds = new Bounds(residentTransform.position + new Vector3(0f, 0.3f, 0f), new Vector3(0.45f, 0.65f, 1f));
                return true;
            }
            bounds = default; sprite = null; residentTransform = null;
            return false;
        }

        private static void DestroyLodObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (value is GameObject gameObject) gameObject.SetActive(false);
            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
