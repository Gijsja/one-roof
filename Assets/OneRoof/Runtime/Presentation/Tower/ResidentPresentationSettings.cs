using System;
using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Authoring settings applied once when resident presentation initializes.</summary>
    [Serializable]
    public sealed class ResidentPresentationSettings
    {
        [SerializeField, Min(1)] private int _skeletalCapacity = 60;
        [SerializeField, Min(0)] private int _prewarmCount = 60;
        [SerializeField] private float _rigFadeStart = 6.5f;
        [SerializeField] private float _rigFadeEnd = 8f;
        [SerializeField] private float _macroFadeStart = 16f;
        [SerializeField] private float _macroFadeEnd = 18f;
        [SerializeField, Min(0f)] private float _viewportMargin = 0.05f;
        [SerializeField, Range(0.01f, 1f)] private float _retainedRigPreference = 0.8f;

        public int SkeletalCapacity { get => _skeletalCapacity; set => _skeletalCapacity = value; }
        public int PrewarmCount { get => _prewarmCount; set => _prewarmCount = value; }
        public float RigFadeStart { get => _rigFadeStart; set => _rigFadeStart = value; }
        public float RigFadeEnd { get => _rigFadeEnd; set => _rigFadeEnd = value; }
        public float MacroFadeStart { get => _macroFadeStart; set => _macroFadeStart = value; }
        public float MacroFadeEnd { get => _macroFadeEnd; set => _macroFadeEnd = value; }
        public float ViewportMargin { get => _viewportMargin; set => _viewportMargin = value; }
        public float RetainedRigPreference { get => _retainedRigPreference; set => _retainedRigPreference = value; }

        public void Validate()
        {
            if (_skeletalCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(SkeletalCapacity));
            if (_prewarmCount < 0) throw new ArgumentOutOfRangeException(nameof(PrewarmCount));
            if (!Finite(_rigFadeStart) || !Finite(_rigFadeEnd) || !Finite(_macroFadeStart) || !Finite(_macroFadeEnd) ||
                _rigFadeStart < 0f || _rigFadeStart >= _rigFadeEnd || _rigFadeEnd > _macroFadeStart || _macroFadeStart >= _macroFadeEnd)
                throw new ArgumentException("Resident LOD fade ranges must be finite, ordered, and nonoverlapping.");
            if (!Finite(_viewportMargin) || _viewportMargin < 0f) throw new ArgumentOutOfRangeException(nameof(ViewportMargin));
            if (!Finite(_retainedRigPreference) || _retainedRigPreference <= 0f || _retainedRigPreference > 1f)
                throw new ArgumentOutOfRangeException(nameof(RetainedRigPreference));
        }

        internal ResidentPresentationSettings ValidatedCopy()
        {
            Validate();
            return (ResidentPresentationSettings)MemberwiseClone();
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
