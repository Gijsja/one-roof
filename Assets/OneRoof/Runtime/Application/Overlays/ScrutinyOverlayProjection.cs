using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Scrutiny;

namespace OneRoof.Application.Overlays
{
    /// <summary>Immutable explanation-ready view of tower-level external pressure.</summary>
    public sealed class ScrutinyOverlayProjection
    {
        public ScrutinyOverlayProjection(float value, ScrutinyTrend trend, float externalEventPressure, bool isExpansionConstrained, IReadOnlyList<string> contributingFactors)
        {
            Value = value;
            Trend = trend;
            ExternalEventPressure = externalEventPressure;
            IsExpansionConstrained = isExpansionConstrained;
            ContributingFactors = new ReadOnlyCollection<string>(new List<string>(contributingFactors ?? Array.Empty<string>()));
        }

        public float Value { get; }
        public ScrutinyTrend Trend { get; }
        public float ExternalEventPressure { get; }
        public bool IsExpansionConstrained { get; }
        public IReadOnlyList<string> ContributingFactors { get; }
        public string AccessibilityLabel => $"Scrutiny: {Value:P0} [{Trend.ToString().ToUpperInvariant()}] | External-event pressure: {ExternalEventPressure:P0} | Expansion: {(IsExpansionConstrained ? "TEMPORARILY CONSTRAINED" : "AVAILABLE")}";
    }
}
