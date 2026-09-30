using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    public readonly struct ResidentBubbleLayoutCandidate
    {
        public ResidentBubbleLayoutCandidate(int residentId, Rect screenRect, bool inspected, int emotionalPriority)
        {
            ResidentId = residentId;
            ScreenRect = screenRect;
            Inspected = inspected;
            EmotionalPriority = emotionalPriority;
        }

        public int ResidentId { get; }
        public Rect ScreenRect { get; }
        public bool Inspected { get; }
        public int EmotionalPriority { get; }
    }

    /// <summary>
    /// Chooses readable bubbles in screen pixels. The caller owns and reuses candidates and output;
    /// candidates are sorted in place. After buffers reach capacity, selection creates no collections.
    /// </summary>
    public sealed class ResidentBubbleLayout
    {
        private readonly List<Rect> _acceptedRects = new List<Rect>(16);
        private static readonly CandidateComparer PriorityComparer = new CandidateComparer();

        public void SelectVisible(List<ResidentBubbleLayoutCandidate> candidates, int maximumVisible,
            List<int> visibleResidentIds, float spacing = 6f, Rect? viewport = null)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (visibleResidentIds == null) throw new ArgumentNullException(nameof(visibleResidentIds));
            visibleResidentIds.Clear();
            _acceptedRects.Clear();
            if (maximumVisible <= 0) return;
            spacing = float.IsNaN(spacing) || float.IsInfinity(spacing) ? 0f : Mathf.Max(0f, spacing);
            candidates.Sort(PriorityComparer);
            for (var i = 0; i < candidates.Count && visibleResidentIds.Count < maximumVisible; i++)
            {
                var candidate = candidates[i];
                var rect = candidate.ScreenRect;
                if (!IsFinite(rect.xMin) || !IsFinite(rect.yMin) || !IsFinite(rect.xMax) ||
                    !IsFinite(rect.yMax) || rect.width <= 0f || rect.height <= 0f) continue;
                if (viewport.HasValue)
                {
                    var bounds = viewport.Value;
                    if (rect.xMin < bounds.xMin || rect.xMax > bounds.xMax ||
                        rect.yMin < bounds.yMin || rect.yMax > bounds.yMax) continue;
                }
                if (visibleResidentIds.Contains(candidate.ResidentId)) continue;
                var spacedRect = Rect.MinMaxRect(rect.xMin - spacing * 0.5f, rect.yMin - spacing * 0.5f,
                    rect.xMax + spacing * 0.5f, rect.yMax + spacing * 0.5f);
                var overlaps = false;
                for (var j = 0; j < _acceptedRects.Count; j++)
                {
                    if (!spacedRect.Overlaps(_acceptedRects[j])) continue;
                    overlaps = true;
                    break;
                }
                if (overlaps) continue;
                _acceptedRects.Add(spacedRect);
                visibleResidentIds.Add(candidate.ResidentId);
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private sealed class CandidateComparer : IComparer<ResidentBubbleLayoutCandidate>
        {
            public int Compare(ResidentBubbleLayoutCandidate a, ResidentBubbleLayoutCandidate b)
            {
                var inspected = b.Inspected.CompareTo(a.Inspected);
                if (inspected != 0) return inspected;
                var priority = b.EmotionalPriority.CompareTo(a.EmotionalPriority);
                return priority != 0 ? priority : a.ResidentId.CompareTo(b.ResidentId);
            }
        }
    }
}
