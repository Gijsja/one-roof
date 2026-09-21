using System.Collections.Generic;
using OneRoof.Application.Population;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    /// <summary>
    /// Presentation coordinator that binds pooled <see cref="NpcView"/> instances to
    /// read-only <see cref="NpcProjection"/> records, strictly enforcing the 40-view cap
    /// across 50 persistent simulation residents.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcPopulationPresenter : MonoBehaviour
    {
        [SerializeField]
        private NpcViewPool _viewPool;

        private NpcVisibilityPolicy _visibilityPolicy;
        private VisibleFloorRange _visibleFloors = VisibleFloorRange.All(4);

        // Pre-allocated buffers to prevent steady-state allocations during updates
        private readonly HashSet<int> _targetEntities = new HashSet<int>();
        private readonly List<int> _releaseBuffer = new List<int>(NpcViewPool.DefaultMaxCapacity);

        public NpcViewPool ViewPool
        {
            get => EnsureViewPool();
            set => _viewPool = value;
        }

        public VisibleFloorRange VisibleFloors
        {
            get => _visibleFloors;
            set => _visibleFloors = value;
        }

        public NpcVisibilityPolicy VisibilityPolicy
        {
            get => _visibilityPolicy ?? (_visibilityPolicy = new NpcVisibilityPolicy(EnsureViewPool().MaxCapacity));
            set => _visibilityPolicy = value;
        }

        private void Awake()
        {
            EnsureDependencies();
        }

        public void EnsureDependencies()
        {
            EnsureViewPool();
            if (_visibilityPolicy == null)
            {
                _visibilityPolicy = new NpcVisibilityPolicy(_viewPool.MaxCapacity);
            }
        }

        /// <summary>
        /// Updates visible pooled views from the current population projections.
        /// Unbinds culled residents and binds newly visible residents without exceeding
        /// the 40-view cap.
        /// </summary>
        public void UpdatePresentation(IReadOnlyList<NpcProjection> allProjections)
        {
            if (allProjections == null)
            {
                return;
            }

            EnsureDependencies();

            // 1. Select prioritized projections (capped at MaxCapacity = 40)
            var visibleProjections = _visibilityPolicy.SelectVisibleNpcs(allProjections, _visibleFloors);

            _targetEntities.Clear();
            for (var i = 0; i < visibleProjections.Count; i++)
            {
                _targetEntities.Add(visibleProjections[i].PersonId);
            }

            // 2. Identify and release views no longer in the prioritized visible set
            _releaseBuffer.Clear();
            foreach (var view in _viewPool.ActiveViews)
            {
                if (view.BoundEntityId.HasValue && !_targetEntities.Contains(view.BoundEntityId.Value))
                {
                    _releaseBuffer.Add(view.BoundEntityId.Value);
                }
            }

            for (var i = 0; i < _releaseBuffer.Count; i++)
            {
                _viewPool.Release(_releaseBuffer[i]);
            }

            // 3. Bind or update views for all prioritized projections
            for (var i = 0; i < visibleProjections.Count; i++)
            {
                var projection = visibleProjections[i];
                var worldPos = CalculateWorldPosition(projection);

                if (_viewPool.TryGetView(projection.PersonId, out var existingView))
                {
                    existingView.UpdateView(projection, worldPos);
                }
                else
                {
                    var newView = _viewPool.Acquire(projection.PersonId);
                    newView.Bind(projection, worldPos);
                }
            }
        }

        /// <summary>
        /// Calculates world coordinates for a given projection.
        /// </summary>
        public static Vector3 CalculateWorldPosition(in NpcProjection projection)
        {
            var y = TowerStructurePresenter.FloorY(projection.Floor);
            var x = projection.HorizontalPosition;
            return new Vector3(x, y, -2f);
        }

        /// <summary>Compatibility entry point for NPC-only consumers; all tower views share this coordinate system.</summary>
        public static float FloorY(int floor) => TowerStructurePresenter.FloorY(floor);

        private NpcViewPool EnsureViewPool()
        {
            if (_viewPool == null)
            {
                _viewPool = GetComponentInChildren<NpcViewPool>();
                if (_viewPool == null)
                {
                    var poolGo = new GameObject("NpcViewPool");
                    poolGo.transform.SetParent(transform, false);
                    _viewPool = poolGo.AddComponent<NpcViewPool>();
                }
            }

            return _viewPool;
        }
    }
}
