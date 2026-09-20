using NUnit.Framework;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class ElectricalGridStateTests
    {
        [Test]
        public void Evaluate_HigherFloorsHaveLowerVoltageFromVerticalRiserLoss()
        {
            var grid = new ElectricalGridState(substationCapacity: 500f, riserLossPerFloor: .1f);
            var snapshot = grid.Evaluate(BuildingTopologyState.CreateWithFixture());

            Assert.That(snapshot.Floors[4].Voltage, Is.LessThan(snapshot.Floors[0].Voltage));
            Assert.That(snapshot.Floors[0].IsBrownout, Is.False);
        }

        [Test]
        public void Evaluate_OverloadedSubstationMarksBrownout()
        {
            var grid = new ElectricalGridState(substationCapacity: 1f, riserLossPerFloor: 0f);
            var snapshot = grid.Evaluate(BuildingTopologyState.CreateWithFixture());

            Assert.That(snapshot.TotalDemand, Is.GreaterThan(snapshot.SubstationCapacity));
            Assert.That(snapshot.Floors[0].IsBrownout, Is.True);
        }
    }
}
