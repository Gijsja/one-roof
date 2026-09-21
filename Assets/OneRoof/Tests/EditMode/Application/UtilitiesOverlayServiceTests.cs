using NUnit.Framework;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Overlays;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class UtilitiesOverlayServiceTests
    {
        [Test]
        public void CreateOverlay_StandardTower_ProjectsEveryFloorWithNonColourEvidence()
        {
            var overlay = new UtilitiesOverlayService().CreateOverlay(new TowerSimulationSession());
            Assert.That(overlay.Floors, Has.Count.EqualTo(5));
            Assert.That(overlay.Floors[0].AccessibilityLabel, Does.Contain("power"));
            Assert.That(overlay.Floors[0].AccessibilityLabel, Does.Contain("water"));
            Assert.That(overlay.Floors[0].AccessibilityLabel, Does.Contain("waste"));
        }

        [Test]
        public void InspectUtilities_ExplainsFailureAndSystemsLevelResponse()
        {
            var details = new TowerInspectionService(new TowerSimulationSession()).InspectUtilities();
            Assert.That(details.Title, Is.EqualTo("Tower Utilities"));
            Assert.That(details.SuggestedResponse, Does.Contain("maintenance training"));
        }
    }
}
