using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Scrutiny;

namespace OneRoof.Application.Overlays
{
    /// <summary>Immutable explanation-ready view of tower-level external pressure.</summary>
    public sealed class ScrutinyOverlayProjection
    {
        public ScrutinyOverlayProjection(float value, ScrutinyTrend trend, float externalEventPressure, IReadOnlyList<string> contributingFactors)
        {
            Value = value;
            Trend = trend;
            ExternalEventPressure = externalEventPressure;
            ContributingFactors = new ReadOnlyCollection<string>(new List<string>(contributingFactors ?? Array.Empty<string>()));
        }

        public float Value { get; }
        public ScrutinyTrend Trend { get; }
        public float ExternalEventPressure { get; }
        public IReadOnlyList<string> ContributingFactors { get; }
        public string EventPressureBand => ExternalEventPressure >= .80f ? "ELEVATED" : ExternalEventPressure >= .50f ? "GUARDED" : "LOW";
        public string AccessibilityLabel => $"Scrutiny: {Value:P0} [{Trend.ToString().ToUpperInvariant()}] | Inspection-event pressure: {ExternalEventPressure:P0} [{EventPressureBand}] | Expansion remains available";
    }
}
