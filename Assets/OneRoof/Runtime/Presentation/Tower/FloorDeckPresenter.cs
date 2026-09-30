using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Architecture;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OneRoof.Presentation.Tower
{
    /// <summary>
    /// Deep presenter responsible for the lifecycle, spatial updates, and theme assignments
    /// of physical floor deck views across the tower.
    /// Eliminates see-through gaps between stacked levels while respecting elevator shaft chutes.
    /// </summary>
    public sealed class FloorDeckPresenter
    {
        public const int DefaultInitialFloorCount = 5;

        private Transform _parent;
        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;

        private readonly Dictionary<int, FloorDeckView> _decksByFloor = new Dictionary<int, FloorDeckView>();
        private readonly Dictionary<int, CellBounds> _appliedBounds = new Dictionary<int, CellBounds>();
        private readonly Dictionary<int, string> _customThemeOverrides = new Dictionary<int, string>();
        private readonly HashSet<GameObject> _authoredObjects = new HashSet<GameObject>();
        private string _globalThemeId = FloorThemeCatalog.ConcreteSlab;

        public int RenderedDeckCount => _decksByFloor.Count;
        public IReadOnlyDictionary<int, FloorDeckView> Decks => _decksByFloor;
        public string GlobalThemeId => _globalThemeId;

        // The combined facade spandrels replace upper interior deck trim in exterior mode.
        // Keep the concourse deck because storefront interiors remain visible.
        public void SetInteriorDecksVisible(bool visible)
        {
            foreach (var pair in _decksByFloor)
            {
                var active = visible || pair.Key == 0;
                if (pair.Value != null && pair.Value.gameObject.activeSelf != active)
                    pair.Value.gameObject.SetActive(active);
            }
        }

        public void Initialize(Transform parent, Material worldMaterial, MaterialPropertyBlock colorBlock)
        {
            _parent = parent;
            _worldMaterial = worldMaterial;
            _colorBlock = colorBlock;
            _decksByFloor.Clear();
            _appliedBounds.Clear();
            _authoredObjects.Clear();
            _customThemeOverrides.Clear();

            // Adopt any pre-existing authored Floor Deck objects
            if (_parent != null)
            {
                var floor = 0;
                while (_parent.Find($"Floor Deck {floor}") != null)
                {
                    AdoptFloorDeck(floor);
                    floor++;
                }
            }
        }

        public void EnsureFloorDecks(TowerTopologyProjection topology)
        {
            var targetCount = topology != null ? topology.FloorCount : DefaultInitialFloorCount;

            // Remove decks above target count
            var floorsToRemove = new List<int>();
            foreach (var floor in _decksByFloor.Keys)
            {
                if (floor >= targetCount)
                {
                    floorsToRemove.Add(floor);
                }
            }

            for (var i = 0; i < floorsToRemove.Count; i++)
            {
                DestroyDeck(floorsToRemove[i]);
            }

            // Ensure deck exists for each floor 0..targetCount - 1
            for (var floor = 0; floor < targetCount; floor++)
            {
                CellBounds slab;
                if (topology != null && topology.TryGetFloorSlab(floor, out var topoSlab))
                {
                    slab = topoSlab;
                }
                else
                {
                    slab = new CellBounds(floor, -14, 16);
                }

                if (!_decksByFloor.TryGetValue(floor, out var deckView) || deckView == null)
                {
                    deckView = CreateOrAdoptDeckView(floor);
                    _decksByFloor[floor] = deckView;
                }

                // Determine appropriate theme
                var themeId = ResolveThemeForFloor(floor, topology);
                var theme = FloorThemeCatalog.GetTheme(themeId);
                deckView.ApplyTheme(theme);

                // Update geometry if bounds changed or first build
                if (!_appliedBounds.TryGetValue(floor, out var applied) || !applied.Equals(slab))
                {
                    deckView.UpdateGeometry(slab, hasShaftCutout: true);
                    _appliedBounds[floor] = slab;
                }
            }
        }

        public void SetFloorTheme(int floor, string themeId)
        {
            _customThemeOverrides[floor] = themeId;
            if (_decksByFloor.TryGetValue(floor, out var deckView) && deckView != null)
            {
                deckView.ApplyTheme(FloorThemeCatalog.GetTheme(themeId));
            }
        }

        public void SetGlobalTheme(string themeId)
        {
            _globalThemeId = themeId;
            foreach (var kvp in _decksByFloor)
            {
                if (!_customThemeOverrides.ContainsKey(kvp.Key) && kvp.Value != null)
                {
                    kvp.Value.ApplyTheme(FloorThemeCatalog.GetTheme(themeId));
                }
            }
        }

        public FloorDeckView GetFloorDeck(int floor)
        {
            _decksByFloor.TryGetValue(floor, out var view);
            return view;
        }

        public bool TryGetFloorDeck(int floor, out FloorDeckView view)
        {
            return _decksByFloor.TryGetValue(floor, out view) && view != null;
        }

        public void Clear()
        {
            foreach (var kvp in _decksByFloor)
            {
                var go = kvp.Value != null ? kvp.Value.gameObject : null;
                if (go != null && !_authoredObjects.Contains(go))
                {
                    if (UnityEngine.Application.isPlaying) Object.Destroy(go);
                    else Object.DestroyImmediate(go);
                }
            }

            _decksByFloor.Clear();
            _appliedBounds.Clear();
            _authoredObjects.Clear();
            _customThemeOverrides.Clear();
        }

        private string ResolveThemeForFloor(int floor, TowerTopologyProjection topology)
        {
            if (_customThemeOverrides.TryGetValue(floor, out var customId) && !string.IsNullOrEmpty(customId))
            {
                return customId;
            }

            if (topology != null)
            {
                var roomsOnFloor = topology.GetRoomsOnFloor(floor);
                if (roomsOnFloor.Count > 0)
                {
                    return FloorThemeCatalog.ResolveDominantTheme(roomsOnFloor);
                }
            }

            return _globalThemeId;
        }

        private FloorDeckView CreateOrAdoptDeckView(int floor)
        {
            var deckName = $"Floor Deck {floor}";
            var child = _parent != null ? _parent.Find(deckName) : null;
            GameObject deckObj;

            if (child != null)
            {
                deckObj = child.gameObject;
                _authoredObjects.Add(deckObj);
            }
            else
            {
                deckObj = new GameObject(deckName);
                if (_parent != null) deckObj.transform.SetParent(_parent, false);
            }

            var view = deckObj.GetComponent<FloorDeckView>() ?? deckObj.AddComponent<FloorDeckView>();
            view.Initialize(floor, _worldMaterial, _colorBlock, FloorThemeCatalog.GetTheme(_globalThemeId));
            return view;
        }

        private void AdoptFloorDeck(int floor)
        {
            var deckName = $"Floor Deck {floor}";
            var child = _parent != null ? _parent.Find(deckName) : null;
            if (child == null) return;

            var go = child.gameObject;
            _authoredObjects.Add(go);
            var view = go.GetComponent<FloorDeckView>() ?? go.AddComponent<FloorDeckView>();
            view.Initialize(floor, _worldMaterial, _colorBlock, FloorThemeCatalog.GetTheme(_globalThemeId));
            _decksByFloor[floor] = view;
        }

        private void DestroyDeck(int floor)
        {
            if (_decksByFloor.TryGetValue(floor, out var view) && view != null)
            {
                var go = view.gameObject;
                _authoredObjects.Remove(go);
                if (UnityEngine.Application.isPlaying) Object.Destroy(go);
                else Object.DestroyImmediate(go);
            }

            _decksByFloor.Remove(floor);
            _appliedBounds.Remove(floor);
            _customThemeOverrides.Remove(floor);
        }
    }
}
