using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Content;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Population;
using UnityEngine;
using EntityId = OneRoof.Domain.Identity.EntityId;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Deep presenter responsible for persistent resident views, Spine/procedural skeletal rigs,
    /// in-room slot positioning, corridor walking, elevator queuing, riding, and emotional reaction bubbles.
    /// </summary>
    public sealed class TowerResidentPresenter
    {
        private Transform _parent;
        private readonly List<Renderer> _residentViews = new List<Renderer>();
        private readonly List<NpcSkeletalHierarchy> _residentSkeletons = new List<NpcSkeletalHierarchy>();
        private readonly List<GameObject> _residentObjects = new List<GameObject>();
        private readonly HashSet<GameObject> _authoredObjects = new HashSet<GameObject>();
        // Animator facing memory: last walk x per resident plus latched travel direction,
        // so walkers face where they travel and idle poses keep their last heading.
        private readonly List<float> _lastWalkX = new List<float>();
        private readonly List<float> _walkFacing = new List<float>();

        public IReadOnlyList<Renderer> ResidentViews => _residentViews;
        public IReadOnlyList<NpcSkeletalHierarchy> ResidentSkeletons => _residentSkeletons;
        public int ResidentCount => _residentViews.Count;

        public bool TryGetResidentView(int residentIndex, out Bounds bounds, out Sprite sprite, out Transform residentTransform)
        {
            if (residentIndex >= 0 && residentIndex < _residentSkeletons.Count)
            {
                var skeletal = _residentSkeletons[residentIndex];
                if (skeletal != null && skeletal.MainRenderer != null)
                {
                    sprite = skeletal.MainRenderer.sprite;
                    residentTransform = skeletal.transform;
                    var pos = residentTransform.position;
                    bounds = new Bounds(new Vector3(pos.x, pos.y + 0.35f, pos.z), new Vector3(0.5f, 0.75f, 1f));
                    return true;
                }
            }

            bounds = default;
            sprite = null;
            residentTransform = null;
            return false;
        }

        public bool TryGetResidentAt(Vector2 worldPos, float hitRadius, out int residentIndex, out Bounds bounds, out Sprite sprite, out Transform residentTransform)
        {
            var closestDistSqr = hitRadius * hitRadius;
            var foundIndex = -1;

            for (var i = 0; i < _residentSkeletons.Count; i++)
            {
                var skeletal = _residentSkeletons[i];
                if (skeletal == null || skeletal.transform == null) continue;

                var pos = (Vector2)skeletal.transform.position + new Vector2(0f, 0.35f);
                var distSqr = (pos - worldPos).sqrMagnitude;
                if (distSqr <= closestDistSqr)
                {
                    closestDistSqr = distSqr;
                    foundIndex = i;
                }
            }

            if (foundIndex >= 0)
            {
                residentIndex = foundIndex;
                return TryGetResidentView(foundIndex, out bounds, out sprite, out residentTransform);
            }

            residentIndex = -1;
            bounds = default;
            sprite = null;
            residentTransform = null;
            return false;
        }

        public void Initialize(Transform parent)
        {
            _parent = parent;
            _residentViews.Clear();
            _residentSkeletons.Clear();
            _residentObjects.Clear();
            _authoredObjects.Clear();
            _lastWalkX.Clear();
            _walkFacing.Clear();
            // Tolerate gaps and unrigged children: skip instead of breaking so
            // one unprepared child cannot orphan every higher-index resident.
            // Cap consecutive misses to avoid unbounded hierarchy scans.
            var consecutiveMisses = 0;
            for (var index = 1; _parent != null && consecutiveMisses < 4 && index <= 256; index++)
            {
                var child = _parent.Find($"Resident View {index}");
                if (child == null)
                {
                    consecutiveMisses++;
                    continue;
                }
                var skeletal = child.GetComponent<NpcSkeletalHierarchy>();
                if (skeletal == null || skeletal.MainRenderer == null)
                {
                    consecutiveMisses++;
                    continue;
                }
                consecutiveMisses = 0;

                // Scene-authored residents can carry the pre-rig composite sprite
                // from an earlier presentation pass. Reapply the canonical skeletal
                // setup so that composite is disabled and only current wardrobe/layer
                // sprites remain visible.
                skeletal.Initialize(index - 1);
                _residentViews.Add(skeletal.MainRenderer);
                _residentSkeletons.Add(skeletal);
                _residentObjects.Add(child.gameObject);
                _authoredObjects.Add(child.gameObject);
            }
        }

        public void EnsureResidentViews(int targetCount)
        {
            targetCount = Math.Max(0, targetCount);
            while (_residentViews.Count > targetCount)
            {
                var last = _residentViews.Count - 1;
                var go = last < _residentObjects.Count ? _residentObjects[last] : null;
                _residentViews.RemoveAt(last);
                if (last < _residentSkeletons.Count) _residentSkeletons.RemoveAt(last);
                if (last < _residentObjects.Count) _residentObjects.RemoveAt(last);
                if (last < _lastWalkX.Count) _lastWalkX.RemoveAt(last);
                if (last < _walkFacing.Count) _walkFacing.RemoveAt(last);
                if (go == null) continue;

                // Destroy is deferred in Play Mode. Rename first so a same-frame
                // hierarchy lookup cannot adopt this stale scene-authored view.
                go.name = $"Retired {go.name}";
                _authoredObjects.Remove(go);
                if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }

            while (_residentViews.Count < targetCount)
            {
                var index = _residentViews.Count;
                var go = new GameObject($"Resident View {index + 1}");
                if (_parent != null) go.transform.SetParent(_parent, false);
                go.transform.position = Vector3.zero;

                var skeletal = go.AddComponent<NpcSkeletalHierarchy>();
                skeletal.Initialize(index);

                _residentViews.Add(skeletal.MainRenderer);
                _residentSkeletons.Add(skeletal);
                _residentObjects.Add(go);
                _lastWalkX.Add(0f);
                _walkFacing.Add(1f);
            }
        }

        public void UpdateResidentPositions(TowerProjection snapshot, TowerTopologyProjection topology, float time, RoomPresenter roomPresenter = null, ElevatorBankPresenter elevatorPresenter = null)
        {
            if (snapshot == null) return;

            var floorCount = Math.Max(TowerStructurePresenter.InitialFloorCount, topology != null ? topology.FloorCount : TowerStructurePresenter.InitialFloorCount);
            var queuedCountsPerFloor = new int[floorCount];
            var arrivedCountsPerFloor = new int[floorCount];

            for (var i = 0; i < snapshot.Residents.Count; i++)
            {
                if (i >= _residentViews.Count)
                {
                    break;
                }

                var resident = snapshot.Residents[i];
                var residentTransform = _residentViews[i].transform;
                var skeletal = i < _residentSkeletons.Count ? _residentSkeletons[i] : null;

                switch (resident.Status)
                {
                    case TransitResidentStatus.InRoom:
                    case TransitResidentStatus.Arrived:
                    {
                        var placedInRoom = false;
                        if (resident.RoomId.HasValue && topology != null && topology.TryGetRoom(new EntityId(resident.RoomId.Value), out var room))
                        {
                            var worldLeft = -2.4f + room.Bounds.MinX * 0.5f;
                            var worldRight = -2.4f + (room.Bounds.MaxX + 1) * 0.5f;
                            var roomWidth = worldRight - worldLeft;
                            var interiorLeft = worldLeft + 0.35f;
                            var interiorRight = worldRight - 0.35f;

                            float roomX;
                            if (roomWidth <= 1.2f)
                            {
                                roomX = (worldLeft + worldRight) * 0.5f;
                            }
                            else
                            {
                                var slot = resident.SlotInRoom;
                                var step = (interiorRight - interiorLeft) / 3f;
                                roomX = Mathf.Clamp(interiorLeft + (slot % 4) * step, interiorLeft, interiorRight);
                            }

                            var dockKind = resident.Activity == ActivityKind.Sleeping ? InteractionPointKind.Sleep :
                                resident.Activity == ActivityKind.Working ? InteractionPointKind.Work : InteractionPointKind.Seat;
                            if (roomPresenter == null || !roomPresenter.TryGetInteractionDock(room, dockKind, resident.SlotInRoom, out var dockPosition))
                                dockPosition = new Vector3(roomX, TowerStructurePresenter.FloorY(room.Floor) - 0.58f, -0.15f);
                            residentTransform.position = dockPosition;

                            var roomCenterX = (worldLeft + worldRight) * 0.5f;
                            var faceScaleX = (residentTransform.position.x < roomCenterX) ? 1f : -1f;
                            // Facing composes with stature inside the hierarchy; never write
                            // localScale here or the variant's body scale gets wiped.
                            if (skeletal != null) skeletal.SetFacing(faceScaleX);
                            else residentTransform.localScale = new Vector3(faceScaleX, 1f, 1f);
                            if (skeletal != null) skeletal.SleepDirection = resident.Activity == ActivityKind.Sleeping ? (int)faceScaleX : 1;

                            skeletal?.SetTransitStatus(TransitResidentStatus.InRoom);
                            skeletal?.SetAnimationClip(resident.Activity == ActivityKind.Sleeping ? NpcAnimationClip.Sleep :
                                resident.Activity == ActivityKind.Working || resident.Activity == ActivityKind.Leisure ? NpcAnimationClip.Sit : NpcAnimationClip.Idle);
                            placedInRoom = true;
                        }

                        if (!placedInRoom)
                        {
                            var destFloor = resident.DestinationFloor;
                            if (destFloor < 0 || destFloor >= floorCount) destFloor = 0;
                            var slot = arrivedCountsPerFloor[destFloor]++;
                            var arrivedX = -0.8f + (slot % 14) * 0.42f;
                            var arrivedY = TowerStructurePresenter.FloorY(destFloor) - 0.58f;
                            residentTransform.position = new Vector3(arrivedX, arrivedY, -0.1f);
                            skeletal?.SetTransitStatus(TransitResidentStatus.InRoom);
                            skeletal?.SetAnimationClip(NpcAnimationClip.Idle);
                        }
                        break;
                    }

                    case TransitResidentStatus.Walking:
                    {
                        var walkX = -2.4f + resident.CellX * 0.5f + 0.25f;
                        var walkFloor = resident.Floor;
                        if (walkFloor < 0 || walkFloor >= floorCount) walkFloor = 0;
                        var walkY = TowerStructurePresenter.FloorY(walkFloor) - 0.58f;
                        residentTransform.position = new Vector3(walkX, walkY, -0.2f);
                        // Face travel direction, latched per resident so pauses keep heading.
                        EnsureWalkMemory(i, walkX);
                        var heading = _walkFacing[i];
                        if (walkX > _lastWalkX[i] + 0.001f) heading = 1f;
                        else if (walkX < _lastWalkX[i] - 0.001f) heading = -1f;
                        _walkFacing[i] = heading;
                        _lastWalkX[i] = walkX;
                        if (skeletal != null) skeletal.SetFacing(heading);
                        else residentTransform.localScale = new Vector3(heading, 1f, 1f);
                        skeletal?.SetTransitStatus(TransitResidentStatus.Walking);
                        break;
                    }

                    case TransitResidentStatus.Queued:
                    {
                        var queueFloor = resident.Floor;
                        if (queueFloor < 0 || queueFloor >= floorCount) queueFloor = 0;
                        var slot = queuedCountsPerFloor[queueFloor]++;
                        // Compact landing formation inside the shaft reserve
                        // (world x in [-2.4, -1.4]). Four columns spill into a
                        // second row instead of marching single-file into the
                        // neighbouring apartment with their agitation auras.
                        var col = slot % 4;
                        var row = (slot / 4) % 2;
                        var crush = (slot / 8) % 2;
                        var queueX = -1.62f - col * 0.24f + crush * 0.09f;
                        var queueY = TowerStructurePresenter.FloorY(queueFloor) - 0.58f + row * 0.34f - crush * 0.05f;
                        residentTransform.position = new Vector3(queueX, queueY, -0.2f);
                        // Reset heading without touching stature; queues face the car (+x).
                        if (skeletal != null) skeletal.SetFacing(1f);
                        else residentTransform.localScale = new Vector3(1f, 1f, 1f);
                        skeletal?.SetTransitStatus(TransitResidentStatus.Queued);
                        break;
                    }

                    case TransitResidentStatus.Riding:
                    {
                        var elevatorIndex = FindPassengerElevator(snapshot, resident.ResidentId);
                        if (snapshot.Elevators.Count == 0 || elevatorIndex < 0 || elevatorIndex >= snapshot.Elevators.Count)
                        {
                            break;
                        }
                        // Share the car's layout math so riders stand inside the
                        // rendered car at any bank size instead of a hardcoded column.
                        ElevatorBankPresenter.CalculateCarLayout(elevatorIndex, snapshot.Elevators.Count, out var layoutX, out var carWidth);
                        var jitterX = ((i % 2) - 0.5f) * Mathf.Min(0.12f, carWidth * 0.3f);
                        Vector3 carAnchor;
                        if (elevatorPresenter != null && elevatorIndex < elevatorPresenter.ElevatorViews.Count && elevatorPresenter.ElevatorViews[elevatorIndex] != null)
                        {
                            // Ride the rendered car: it lerps between floors, so
                            // anchoring to it keeps residents inside mid-travel
                            // instead of snapping to the logical floor ahead of it.
                            var carPos = elevatorPresenter.ElevatorViews[elevatorIndex].transform.position;
                            carAnchor = new Vector3(carPos.x, carPos.y - 0.25f, -0.3f);
                        }
                        else
                        {
                            carAnchor = new Vector3(layoutX, TowerStructurePresenter.FloorY(snapshot.Elevators[elevatorIndex].Floor) - 0.25f, -0.3f);
                        }
                        residentTransform.position = new Vector3(carAnchor.x + jitterX, carAnchor.y, carAnchor.z);
                        skeletal?.SetTransitStatus(TransitResidentStatus.Riding);
                        break;
                    }
                }

                // Emotional emote expression
                if (resident.Status == TransitResidentStatus.Queued)
                {
                    if (resident.WaitTicks >= 30) skeletal?.SetEmote(NpcEmoteKind.Anger);
                    else if (resident.WaitTicks >= 15) skeletal?.SetEmote(NpcEmoteKind.Sweat);
                    else if (resident.WaitTicks >= 5) skeletal?.SetEmote(NpcEmoteKind.Ellipsis);
                    else skeletal?.SetEmote(NpcEmoteKind.None);
                }
                else if (resident.Status == TransitResidentStatus.InRoom)
                {
                    switch (resident.Activity)
                    {
                        case ActivityKind.Sleeping:
                            skeletal?.SetEmote(NpcEmoteKind.Sleeping);
                            break;
                        case ActivityKind.Working:
                            skeletal?.SetEmote(NpcEmoteKind.Lightbulb);
                            break;
                        case ActivityKind.Leisure:
                            skeletal?.SetEmote(NpcEmoteKind.MusicNote);
                            break;
                        default:
                            skeletal?.SetEmote(NpcEmoteKind.None);
                            break;
                    }
                }
                else
                {
                    skeletal?.SetEmote(NpcEmoteKind.None);
                }

                // Waiting is the person-scale half of the congestion explanation chain.
                // The aura starts only after a legible delay and reaches full intensity at 30 ticks.
                var agitation = resident.Status == TransitResidentStatus.Queued
                    ? Mathf.Clamp01((resident.WaitTicks - 10f) / 20f)
                    : 0f;
                var effects = skeletal != null
                    ? skeletal.GetComponent<VisualEffectsPresenter>() ?? skeletal.gameObject.AddComponent<VisualEffectsPresenter>()
                    : null;
                effects?.SetAgitation(agitation);

                skeletal?.ApplyProceduralAnimation(time);
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _residentObjects.Count; i++)
            {
                var go = _residentObjects[i];
                if (go != null && !_authoredObjects.Contains(go))
                {
                    if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(go);
                    else UnityEngine.Object.DestroyImmediate(go);
                }
            }

            _residentObjects.Clear();
            _residentViews.Clear();
            _residentSkeletons.Clear();
            _authoredObjects.Clear();
            _lastWalkX.Clear();
            _walkFacing.Clear();
        }

        private void EnsureWalkMemory(int index, float currentX)
        {
            // Authored residents discovered via Initialize never pass through
            // EnsureResidentViews, so grow the parallel facing memory on demand.
            while (_lastWalkX.Count <= index)
            {
                _lastWalkX.Add(currentX);
                _walkFacing.Add(1f);
            }
        }

        private static int FindPassengerElevator(TowerProjection snapshot, int residentId)
        {
            for (var i = 0; i < snapshot.Elevators.Count; i++)
            {
                var ids = snapshot.Elevators[i].PassengerIds;
                for (var j = 0; j < ids.Count; j++)
                {
                    if (ids[j] == residentId)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }
    }
}
