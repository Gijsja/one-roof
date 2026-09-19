using NUnit.Framework;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Overlays;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class PopulationOverlayServiceTests
    {
        [Test]
        public void CreateOverlay_StandardTower_ProjectsEveryFloorWithAccessibleDemographics()
        {
            var overlay = new PopulationOverlayService().CreateOverlay(new TowerSimulationSession());

            Assert.That(overlay.ResidentCount, Is.EqualTo(50));
            Assert.That(overlay.Floors, Has.Count.EqualTo(5));
            Assert.That(overlay.TryGetFloor(1, out var floorOne), Is.True);
            Assert.That(floorOne.ResidentCount, Is.GreaterThan(0));
            Assert.That(floorOne.YoungAdultCount + floorOne.AdultCount + floorOne.OlderAdultCount, Is.EqualTo(floorOne.ResidentCount));
            Assert.That(floorOne.LimitedResourceCount + floorOne.StableResourceCount + floorOne.ComfortableResourceCount, Is.EqualTo(floorOne.ResidentCount));
            Assert.That(floorOne.AccessibilityLabel, Does.Contain("Age 18–29"));
            Assert.That(floorOne.AccessibilityLabel, Does.Contain("Resources"));
        }

        [Test]
        public void InspectPopulationFloor_ExposesOverlayEvidenceAndSystemsLevelResponse()
        {
            var details = new TowerInspectionService(new TowerSimulationSession()).InspectPopulationFloor(1);

            Assert.That(details, Is.Not.Null);
            Assert.That(details.Title, Is.EqualTo("Floor 1 Population"));
            Assert.That(details.Details, Has.Some.Contains("Age 18–29"));
            Assert.That(details.Details, Has.Some.Contains("Household resources"));
            Assert.That(details.SuggestedResponse, Does.Contain("Build, leasing, and service"));
        }
    }
}
