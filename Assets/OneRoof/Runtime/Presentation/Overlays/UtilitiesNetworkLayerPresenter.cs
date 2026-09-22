using System.Collections.Generic;
using OneRoof.Application.Overlays;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Overlays
{
    /// <summary>Draws utility risers in their own toggleable world-space view layer.</summary>
    [DisallowMultipleComponent]
    public sealed class UtilitiesNetworkLayerPresenter : MonoBehaviour
    {
        public enum NetworkKind { Power, Water, Waste }

        private readonly List<LineRenderer> _segments = new List<LineRenderer>();
        private UtilitiesOverlayProjection _overlay;
        private Material _lineMaterial;
        private Transform _layerRoot;
        private bool _visible;

        public NetworkKind SelectedNetwork { get; private set; }
        public bool IsVisible => _visible;

        public void SetVisible(bool visible)
        {
            _visible = visible;
            EnsureLayerRoot();
            _layerRoot.gameObject.SetActive(visible);
        }

        public void Select(NetworkKind network)
        {
            if (SelectedNetwork == network) return;
            SelectedNetwork = network;
            Rebuild();
        }

        public void UpdateOverlay(UtilitiesOverlayProjection overlay)
        {
            _overlay = overlay;
            Rebuild();
        }

        private void Rebuild()
        {
            if (_overlay == null) { ClearSegments(); return; }
            EnsureLayerRoot();
            EnsureMaterial();
            var segmentIndex = 0;

            for (var i = 0; i < _overlay.Floors.Count; i++)
            {
                var floor = _overlay.Floors[i];
                var connected = SelectedNetwork == NetworkKind.Power ? floor.PowerConnected :
                    SelectedNetwork == NetworkKind.Water ? floor.WaterConnected : floor.WasteConnected;
                if (!connected) continue;

                var column = SelectedNetwork == NetworkKind.Power ? floor.PowerColumn :
                    SelectedNetwork == NetworkKind.Water ? floor.WaterColumn : floor.WasteColumn;
                var color = SelectedNetwork == NetworkKind.Power ? new Color(1f, .72f, .18f) :
                    SelectedNetwork == NetworkKind.Water ? new Color(.18f, .72f, 1f) : new Color(.74f, .48f, .9f);
                var x = -2.4f + column * .5f;
                var bottom = TowerStructurePresenter.FloorY(floor.Floor) + .08f;
                var top = bottom + TowerStructurePresenter.DefaultFloorHeight - .16f;
                var line = GetSegment(segmentIndex++);
                line.gameObject.name = $"Utility {SelectedNetwork} Floor {floor.Floor}";
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.SetPosition(0, new Vector3(x, bottom, -.35f));
                line.SetPosition(1, new Vector3(x, top, -.35f));
                line.startWidth = .10f;
                line.endWidth = .10f;
                line.sharedMaterial = _lineMaterial;
                line.startColor = color;
                line.endColor = color;
                line.numCapVertices = 3;
                line.sortingOrder = 40;
                line.enabled = _visible;
            }

            while (_segments.Count > segmentIndex)
            {
                var last = _segments[_segments.Count - 1];
                if (UnityEngine.Application.isPlaying) Destroy(last.gameObject); else DestroyImmediate(last.gameObject);
                _segments.RemoveAt(_segments.Count - 1);
            }
        }

        private LineRenderer GetSegment(int index)
        {
            if (index < _segments.Count) return _segments[index];
            var lineObject = new GameObject("Utility Network Segment");
            lineObject.transform.SetParent(_layerRoot, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = .10f;
            line.endWidth = .10f;
            line.sharedMaterial = _lineMaterial;
            line.numCapVertices = 3;
            line.sortingOrder = 40;
            _segments.Add(line);
            return line;
        }

        private void EnsureLayerRoot()
        {
            if (_layerRoot != null) return;
            var layer = new GameObject("Utility Network View Layer");
            _layerRoot = layer.transform;
            _layerRoot.SetParent(transform, false);
            layer.SetActive(_visible);
        }

        private void EnsureMaterial()
        {
            if (_lineMaterial != null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader != null) _lineMaterial = new Material(shader);
        }

        private void ClearSegments()
        {
            for (var i = 0; i < _segments.Count; i++)
            {
                var item = _segments[i];
                if (item == null) continue;
                if (UnityEngine.Application.isPlaying) Destroy(item.gameObject); else DestroyImmediate(item.gameObject);
            }
            _segments.Clear();
        }

        private void OnDestroy()
        {
            ClearSegments();
            if (_lineMaterial == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(_lineMaterial); else DestroyImmediate(_lineMaterial);
        }
    }
}
