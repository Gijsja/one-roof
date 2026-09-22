using System;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Overlays
{
    /// <summary>Maps authoritative Scrutiny state into a read-only Data-mode projection.</summary>
    public sealed class ScrutinyOverlayService
    {
        public ScrutinyOverlayProjection CreateOverlay(TowerSimulationSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var scrutiny = session.Scrutiny;
            return new ScrutinyOverlayProjection(scrutiny.Value, scrutiny.Trend, scrutiny.ExternalEventPressure, scrutiny.ContributingFactors);
        }
    }
}
