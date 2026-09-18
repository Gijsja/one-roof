using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    /// <summary>
    /// Fixed-capacity object pool for <see cref="NpcView"/> instances.
    /// Enforces the strict performance budget of at most 40 visible views
    /// while 50 or more simulation residents persist in Domain state.
    /// </summary>
    public sealed class NpcViewPool : MonoBehaviour
    {
        public const int DefaultMaxCapacity = 40;

        [SerializeField]
        private int _maxCapacity = DefaultMaxCapacity;

        private readonly List<NpcView> _allViews = new List<NpcView>(DefaultMaxCapacity);
        private readonly Dictionary<int, NpcView> _activeViews = new Dictionary<int, NpcView>(DefaultMaxCapacity);
        private readonly Queue<NpcView> _availableViews = new Queue<NpcView>(DefaultMaxCapacity);

        private Material _sharedMaterial;

        public int MaxCapacity
        {
            get => _maxCapacity;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Max capacity must be positive.");
                }

                _maxCapacity = value;
            }
        }

        public int ActiveCount => _activeViews.Count;

        public int TotalInstantiatedCount => _allViews.Count;

        public IReadOnlyCollection<NpcView> ActiveViews => _activeViews.Values;

        public IReadOnlyList<NpcView> AllViews => _allViews;

        private void OnDestroy()
        {
            ReleaseAll();

            if (_sharedMaterial != null)
            {
                DestroyImmediate(_sharedMaterial);
                _sharedMaterial = null;
            }
        }

        /// <summary>
        /// Prewarms the pool by instantiating <paramref name="count"/> inactive views ahead of time.
        /// </summary>
        public void Prewarm(int count)
        {
            var target = Mathf.Min(count, _maxCapacity);
            while (_allViews.Count < target)
            {
                var view = CreateNewViewInstance();
                _availableViews.Enqueue(view);
            }
        }

        /// <summary>
        /// Acquires a pooled view bound to <paramref name="entityId"/>.
        /// Throws if the pool capacity would be exceeded.
        /// </summary>
        public NpcView Acquire(int entityId)
        {
            if (_activeViews.TryGetValue(entityId, out var existing))
            {
                return existing;
            }

            if (_activeViews.Count >= _maxCapacity)
            {
                throw new InvalidOperationException(
                    $"Cannot acquire NPC view for entity {entityId}: pool has reached its maximum capacity of {_maxCapacity}.");
            }

            NpcView view;
            if (_availableViews.Count > 0)
            {
                view = _availableViews.Dequeue();
            }
            else if (_allViews.Count < _maxCapacity)
            {
                view = CreateNewViewInstance();
            }
            else
            {
                throw new InvalidOperationException(
                    $"No pooled NPC views available and capacity ({_maxCapacity}) reached.");
            }

            _activeViews.Add(entityId, view);
            return view;
        }

        /// <summary>
        /// Releases the view bound to <paramref name="entityId"/> back to the available pool.
        /// </summary>
        public bool Release(int entityId)
        {
            if (!_activeViews.TryGetValue(entityId, out var view))
            {
                return false;
            }

            _activeViews.Remove(entityId);
            view.Unbind();
            _availableViews.Enqueue(view);
            return true;
        }

        /// <summary>
        /// Releases all currently active views back to the pool.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var view in _activeViews.Values)
            {
                view.Unbind();
                _availableViews.Enqueue(view);
            }

            _activeViews.Clear();
        }

        /// <summary>
        /// Looks up an active view by entity ID.
        /// </summary>
        public bool TryGetView(int entityId, out NpcView view) =>
            _activeViews.TryGetValue(entityId, out view);

        private NpcView CreateNewViewInstance()
        {
            var index = _allViews.Count + 1;
            var go = new GameObject($"NpcView_{index}");
            go.transform.SetParent(transform, false);

            var skeletal = go.AddComponent<NpcSkeletalHierarchy>();
            skeletal.Initialize(index - 1);

            var view = go.AddComponent<NpcView>();
            go.SetActive(false);

            _allViews.Add(view);
            return view;
        }

        private Material GetOrCreateSharedMaterial()
        {
            if (_sharedMaterial != null)
            {
                return _sharedMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                _sharedMaterial = new Material(shader) { name = "PooledNpc_SharedMaterial" };
            }

            return _sharedMaterial;
        }
    }
}
