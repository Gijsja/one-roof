using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class ElevatorTierPersistenceTests
    {
        [Test]
        public void UpgradedTier_RoundTripPreservesTierAndTravelTiming()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var upgrade = sim.ExecuteCommand(new UpgradeElevatorTierCommand(2));
            Assert.That(upgrade.Accepted, Is.True, upgrade.Reason);
            var restored = ElevatorBank.FromSaveData(sim.ElevatorBank.ToSaveData());
            Assert.That(restored.Tier, Is.EqualTo(2));
            Assert.That(restored.Cars[0].Timing.FloorTravelSeconds, Is.EqualTo(10));
        }

        [Test]
        public void LegacySaveWithoutTier_DefaultsToTierOne()
        {
            var restored = ElevatorBank.FromSaveData(new ElevatorBankSaveData { minFloor = 0, maxFloor = 0 });
            Assert.That(restored.Tier, Is.EqualTo(1));
        }
    }
}
