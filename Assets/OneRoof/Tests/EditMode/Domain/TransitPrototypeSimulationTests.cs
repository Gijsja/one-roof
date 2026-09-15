using NUnit.Framework;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class TransitPrototypeSimulationTests
    {
        [Test]
        public void MorningCommuteStartsWithAllResidentsQueuedAtTheLobby()
        {
            var simulation = new TransitPrototypeSimulation();

            var snapshot = simulation.Snapshot();

            Assert.That(snapshot.QueueLength, Is.EqualTo(TransitPrototypeSimulation.ResidentCount));
            Assert.That(snapshot.Elevators.Count, Is.EqualTo(1));
            Assert.That(snapshot.ArrivedCount, Is.Zero);
        }

        [Test]
        public void AddedElevatorCapacityImprovesTheCompletedCommuteWait()
        {
            var constrained = new TransitPrototypeSimulation();
            RunToCompletion(constrained);

            var expanded = new TransitPrototypeSimulation();
            expanded.AddElevator();
            RunToCompletion(expanded);

            Assert.That(expanded.Snapshot().AverageWaitTicks, Is.LessThan(constrained.Snapshot().AverageWaitTicks));
            Assert.That(expanded.Snapshot().ArrivedCount, Is.EqualTo(TransitPrototypeSimulation.ResidentCount));
        }

        private static void RunToCompletion(TransitPrototypeSimulation simulation)
        {
            for (var tick = 0; tick < 500 && !simulation.IsComplete; tick++)
            {
                simulation.AdvanceOneTick();
            }

            Assert.That(simulation.IsComplete, Is.True);
        }
    }
}
