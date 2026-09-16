using System.Collections;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class GoldenExpansionPlayModeTests
    {
        [UnityTest]
        public IEnumerator GoldenExpansionPlayMode_FullLifecycle_ExpandsTowerAndIntervenes()
        {
            var holder = new GameObject("Test_PlayMode_Tower");
            var controller = holder.AddComponent<TowerPlayableController>();

            Assert.That(controller.SimulationSession, Is.Not.Null);
            Assert.That(controller.SimulationSession.FloorCount, Is.EqualTo(5));
            Assert.That(controller.SimulationSession.ResidentCount, Is.EqualTo(50));

            // Run 5 ticks
            for (var i = 0; i < 5; i++)
            {
                controller.SimulationSession.AdvanceOneTick();
                yield return null;
            }

            // Interactive expansion: Build floor 5 slab
            var slabCmd = new BuildFloorSlabCommand(5, -14, 16);
            var slabResult = controller.SimulationSession.BuildFloorSlab(slabCmd);
            Assert.That(slabResult.Accepted, Is.True);
            Assert.That(controller.SimulationSession.FloorCount, Is.EqualTo(6));

            // Build apartment on floor 5
            var aptCmd = new BuildRoomCommand(5, -10, -5, new ContentId("residential:studio"), capacity: 5);
            var aptResult = controller.SimulationSession.BuildRoom(aptCmd);
            Assert.That(aptResult.Accepted, Is.True);

            // Extend shaft to floor 5
            var shaftCmd = new AddElevatorShaftCommand(0, 5, 0, 1);
            var shaftResult = controller.SimulationSession.AddElevatorShaft(shaftCmd);
            Assert.That(shaftResult.Accepted, Is.True);

            // Advance 40 ticks to allow leasing to move in residents
            for (var i = 0; i < 40; i++)
            {
                controller.SimulationSession.AdvanceOneTick();
                yield return null;
            }

            Assert.That(controller.SimulationSession.ResidentCount, Is.GreaterThan(50));

            // Player intervention: Add elevator capacity
            var initialCars = controller.SimulationSession.ElevatorBank.Cars.Count;
            controller.OnConfirmElevatorPlacement();
            Assert.That(controller.SimulationSession.ElevatorBank.Cars.Count, Is.EqualTo(initialCars + 1));

            // Advance through commute
            for (var i = 0; i < 30; i++)
            {
                controller.SimulationSession.AdvanceOneTick();
                yield return null;
            }

            Assert.That(controller.SimulationSession.ElevatorBank.DeliveredCount, Is.GreaterThan(0));

            Object.DestroyImmediate(holder);
        }
    }
}
