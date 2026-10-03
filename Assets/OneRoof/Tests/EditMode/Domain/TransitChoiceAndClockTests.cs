using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class TransitChoiceAndClockTests
    {
        [TestCase(1, 3)]
        [TestCase(3, 2)]
        [TestCase(5, 2)]
        public void ElevenFloorJourneyUsesMinutesRatherThanMinutesPerFloor(int tier, int latestArrival)
        {
            var bank = new ElevatorBank(0, 11, new[] { new ElevatorCar(new EntityId(1), 0, 8) });
            bank.SetTier(tier);
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(10), 0, 11));
            for (var tick = 1; tick <= latestArrival; tick++) bank.Advance(new Tick(tick));
            Assert.That(bank.DeliveredCount, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(20)]
        public void LongOrdinaryDescentKeepsUsableElevatorEvenAfterFortyQueueMinutes(int ahead)
        {
            var topology = CreateTopology(29);
            var route = new TransitRoutePlanner(topology.TransitGraph).FindRoute(new EntityId(1291), new EntityId(1001));
            var trip = new TripRecord(new EntityId(9000), new EntityId(9001),
                WorldLocation.InRoom(new EntityId(1290)), WorldLocation.InRoom(new EntityId(1000)),
                TripPurpose.Work, new Tick(0), route);
            trip.Begin();
            var transit = new TransitExecutionSystem();
            transit.RestoreTripExecution(trip, 0, 1, true, false, 29, 0f);
            var car = new ElevatorCar(new EntityId(1), 0, 8, new ElevatorTimingConfig(20, 2, 2));
            var bank = new ElevatorBank(0, 29, new[] { car });
            for (var i = 0; i < ahead; i++) bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(8000 + i), 29, 0));
            var resident = new ElevatorPassenger(trip.PersonId, 29, 0) { WaitTicks = 40 };
            bank.EnqueuePassenger(resident);
            transit.Advance(new Tick(41), topology, bank, null);
            Assert.That(transit.ActiveTrips[0].Route.Legs, Has.Some.Matches<TransitEdge>(e => e.Mode == TransitMode.Elevator));
            Assert.That(bank.IsPassengerInQueue(trip.PersonId, out _), Is.True);
        }

        [Test]
        public void HallwayTraversalConsumesOneSecondPerCellAcrossSeveralLegs()
        {
            var topology = CreateTopology(0);
            var route = new TransitRoutePlanner(topology.TransitGraph).FindRoute(new EntityId(1001), new EntityId(1003));
            var trip = new TripRecord(new EntityId(9000), new EntityId(9001),
                WorldLocation.InRoom(new EntityId(1000)), WorldLocation.InRoom(new EntityId(2000)),
                TripPurpose.Work, new Tick(0), route);
            var transit = new TransitExecutionSystem();
            transit.SubmitTrip(trip, topology, new Tick(0), null);
            transit.Advance(new Tick(1), topology, new ElevatorBank(0, 0, null), null);
            Assert.That(trip.State, Is.EqualTo(TripState.Completed));
        }

        [TestCase(3, 7, 4)]
        [TestCase(5, 10, 6)]
        public void FloorTravelRetainsSecondsAcrossFiveSecondBoundaries(int tier, int floor, int remaining)
        {
            var car = new ElevatorCar(new EntityId(1), 0, 8);
            var bank = new ElevatorBank(0, 20, new[] { car });
            bank.SetTier(tier);
            // Rider destination is a real committed stop, not a hall call that dispatch can remove.
            car.RestoreState(0, ElevatorCarPhase.OpenLoading, ElevatorDirection.Up, 0, null);
            car.Board(new ElevatorPassenger(new EntityId(10), 0, 20));
            car.RestoreState(0, ElevatorCarPhase.Idle, ElevatorDirection.Up, 0, new List<ElevatorPassenger>(car.Passengers));
            bank.Advance(new Tick(1));
            Assert.That(car.CurrentFloor, Is.EqualTo(floor));
            Assert.That(car.TimerSecondsRemaining, Is.EqualTo(remaining));
        }

        [Test]
        public void CustomMinuteTimingSurvivesBankSaveWithoutSpeedingUp()
        {
            var bank = new ElevatorBank(0, 5, new[] { new ElevatorCar(new EntityId(1), 0, 8, new ElevatorTimingConfig(3, 2, 2)) });
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(10), 0, 5));
            bank.Advance(new Tick(1));
            var restored = ElevatorBank.FromSaveData(bank.ToSaveData());
            Assert.That(restored.Cars[0].Timing.FloorTravelSeconds, Is.EqualTo(180));
            Assert.That(restored.Cars[0].Timing.UsesSeconds, Is.False);
            for (var tick = 2; tick <= 40; tick++)
            {
                bank.Advance(new Tick(tick)); restored.Advance(new Tick(tick));
                Assert.That(restored.Cars[0].Phase, Is.EqualTo(bank.Cars[0].Phase));
                Assert.That(restored.Cars[0].TimerTicksRemaining, Is.EqualTo(bank.Cars[0].TimerTicksRemaining));
                Assert.That(restored.DeliveredCount, Is.EqualTo(bank.DeliveredCount));
            }
        }

        [Test]
        public void UpgradeRetainsTheUnfinishedMinutePhaseBeforeUsingNewSpeed()
        {
            var car = new ElevatorCar(new EntityId(1), 0, 8, new ElevatorTimingConfig(3, 2, 2));
            var bank = new ElevatorBank(0, 5, new[] { car });
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(10), 0, 5));
            bank.Advance(new Tick(1));
            Assert.That(car.TimerTicksRemaining, Is.EqualTo(1));
            bank.SetTier(5);
            Assert.That(car.TimerSecondsRemaining, Is.EqualTo(60));
        }

        [TestCase(1, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 0)]
        [TestCase(4, 0)]
        [TestCase(0, 2)]
        [TestCase(0, 1)]
        public void EligibleShortFlightUsesStairsWhenFullArrivalAndEffortAreBetter(int origin, int destination)
        {
            var topology = CreateTopology(29);
            var transit = new TransitExecutionSystem();
            var trip = Trip(topology, origin, destination);
            transit.SubmitTrip(trip, topology, new Tick(0), null);
            // A car at F29 has a substantial pickup journey, even without a hall queue.
            var bank = new ElevatorBank(0, 29, new[] { new ElevatorCar(new EntityId(1), 29, 8) });
            transit.Advance(new Tick(1), topology, bank, null);
            Assert.That(bank.IsPassengerInQueue(trip.PersonId, out _), Is.False);
            if (transit.ActiveTripCount > 0)
                Assert.That(transit.ActiveTrips[0].Route.Legs, Has.None.Matches<TransitEdge>(e => e.Mode == TransitMode.Elevator));
        }

        [TestCase(3, 1)]
        [TestCase(4, 2)]
        public void ThreeOrFourFloorDescentKeepsElevatorWhenStairsHaveNoCostAdvantage(int origin, int arrivalTick)
        {
            var topology = CreateTopology(4);
            var transit = new TransitExecutionSystem();
            var trip = Trip(topology, origin, 0);
            transit.SubmitTrip(trip, topology, new Tick(0), null);
            var bank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(1), origin, 8) });
            for (var tick = 1; tick <= arrivalTick; tick++) transit.Advance(new Tick(tick), topology, bank, null);
            Assert.That(bank.DeliveredCount, Is.EqualTo(1));
            Assert.That(trip.CompletionTick.Value.Value, Is.EqualTo(arrivalTick));
        }

        [TestCase(0, 3)]
        [TestCase(5, 0)]
        public void OrdinaryStairEligibilityIsDirectionalAndBounded(int origin, int destination)
        {
            var topology = CreateTopology(29);
            var transit = new TransitExecutionSystem();
            var trip = Trip(topology, origin, destination); trip.Begin();
            transit.RestoreTripExecution(trip, 0, 1, true, false, origin, 0);
            var bank = new ElevatorBank(0, 29, new[] { new ElevatorCar(new EntityId(1), 29, 8, new ElevatorTimingConfig(20, 2, 2)) });
            bank.EnqueuePassenger(new ElevatorPassenger(trip.PersonId, origin, destination) { WaitTicks = 40 });
            transit.Advance(new Tick(41), topology, bank, null);
            Assert.That(bank.IsPassengerInQueue(trip.PersonId, out _), Is.True);
            Assert.That(transit.ActiveTrips[0].Route.Legs, Has.Some.Matches<TransitEdge>(e => e.Mode == TransitMode.Elevator));
        }

        [Test]
        public void LongStairsAreAvailableWhenThereAreNoCars()
        {
            var topology = CreateTopology(29);
            var transit = new TransitExecutionSystem();
            var trip = Trip(topology, 29, 0);
            transit.SubmitTrip(trip, topology, new Tick(0), null);
            var bank = new ElevatorBank(0, 29, null);
            for (var tick = 1; tick <= 16; tick++) transit.Advance(new Tick(tick), topology, bank, null);
            Assert.That(trip.State, Is.EqualTo(TripState.Completed));
            Assert.That(trip.CompletionTick.Value.Value, Is.EqualTo(15));
        }

        [Test]
        public void FailedReconsiderationRetainsQueueOrderAndCountsWaitOnlyOnce()
        {
            var topology = CreateTopology(29);
            var transit = new TransitExecutionSystem();
            var trip = Trip(topology, 29, 0); trip.Begin();
            transit.RestoreTripExecution(trip, 0, 1, true, false, 29, 0);
            var bank = new ElevatorBank(0, 29, new[] { new ElevatorCar(new EntityId(1), 0, 1, new ElevatorTimingConfig(20, 2, 2)) });
            var first = new ElevatorPassenger(new EntityId(9002), 29, 0) { WaitTicks = 15 };
            var second = new ElevatorPassenger(trip.PersonId, 29, 0) { WaitTicks = 15 };
            bank.EnqueuePassenger(first); bank.EnqueuePassenger(second);
            transit.Advance(new Tick(16), topology, bank, null);
            var queue = bank.Snapshot().GetFloorQueue(29);
            Assert.That(queue[0].PersonId, Is.EqualTo(first.PersonId));
            Assert.That(queue[1].PersonId, Is.EqualTo(second.PersonId));
            Assert.That(second.WaitTicks, Is.EqualTo(16));
            Assert.That(trip.WaitTicks, Is.Zero);
            Assert.That(transit.ActiveTrips[0].LastReconsiderationTick, Is.EqualTo(16));
            var restored = new TransitExecutionSystem();
            restored.RestoreFromSaveData(transit.ToSaveData(), topology, null);
            Assert.That(restored.ActiveTrips[0].LastReconsiderationTick, Is.EqualTo(16));
        }

        [Test]
        public void PartialStairSecondsReplayToTheSameArrivalWithoutWholeMinuteRounding()
        {
            var topology = CreateTopology(2);
            var original = new TransitExecutionSystem();
            var trip = Trip(topology, 0, 2);
            original.SubmitTrip(trip, topology, new Tick(0), null);
            var bank = new ElevatorBank(0, 2, null);
            original.Advance(new Tick(1), topology, bank, null);
            Assert.That(original.ActiveTripCount, Is.EqualTo(1));
            Assert.That(original.ActiveTrips[0].LegRemainingSeconds, Is.EqualTo(33));
            var restored = new TransitExecutionSystem();
            restored.RestoreFromSaveData(original.ToSaveData(), topology, null);
            var restoredTrip = restored.ActiveTrips[0].Trip;
            original.Advance(new Tick(2), topology, bank, null);
            restored.Advance(new Tick(2), topology, new ElevatorBank(0, 2, null), null);
            Assert.That(trip.CompletionTick.Value.Value, Is.EqualTo(2));
            Assert.That(restoredTrip.CompletionTick, Is.EqualTo(trip.CompletionTick));
        }

        [Test]
        public void PartialEightSecondMovementReplaysCarAndDirectionalPickupsExactly()
        {
            var bank = new ElevatorBank(-5, 39, new[] { new ElevatorCar(new EntityId(1), -5, 8) });
            bank.SetTier(3);
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(10), 20, -5));
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(11), 3, 30));
            bank.Advance(new Tick(1));
            Assert.That(bank.Cars[0].TimerSecondsRemaining, Is.EqualTo(4));
            var restored = ElevatorBank.FromSaveData(bank.ToSaveData());
            for (var tick = 2; tick <= 40; tick++)
            {
                bank.Advance(new Tick(tick)); restored.Advance(new Tick(tick));
                var first = bank.Cars[0]; var second = restored.Cars[0];
                Assert.That(second.CurrentFloor, Is.EqualTo(first.CurrentFloor));
                Assert.That(second.Phase, Is.EqualTo(first.Phase));
                Assert.That(second.Direction, Is.EqualTo(first.Direction));
                Assert.That(second.TimerSecondsRemaining, Is.EqualTo(first.TimerSecondsRemaining));
                Assert.That(second.TargetFloors, Is.EqualTo(first.TargetFloors));
                Assert.That(restored.TotalQueuedCount, Is.EqualTo(bank.TotalQueuedCount));
                Assert.That(restored.DeliveredCount, Is.EqualTo(bank.DeliveredCount));
            }
        }

        [Test]
        public void OlderSavedElevatorUsesItsMinuteTimerAndTierSpeed()
        {
            var bank = ElevatorBank.FromSaveData(new ElevatorBankSaveData
            {
                minFloor = 0, maxFloor = 10, tier = 5,
                cars = new[] { new ElevatorCarSaveData { id = 1, currentFloor = 3, capacity = 8,
                    phase = (int)ElevatorCarPhase.Moving, direction = (int)ElevatorDirection.Up,
                    timerTicksRemaining = 2, targetFloors = new[] { 10 },
                    passengers = new[] { new ElevatorPassengerSaveData { personId = 10, originFloor = 0, destinationFloor = 10 } } } }
            });
            bank.Advance(new Tick(1));
            Assert.That(bank.Cars[0].CurrentFloor, Is.EqualTo(3));
            Assert.That(bank.Cars[0].TimerTicksRemaining, Is.EqualTo(1));
            Assert.That(bank.Cars[0].Timing.FloorTravelSeconds, Is.EqualTo(180));
        }

        [Test]
        public void ReconsiderationUsesLiveFirstLegWaitAndCreditsAcceptedDiversionOnce()
        {
            var topology = CreateTopology(29);
            var transit = new TransitExecutionSystem();
            var trip = Trip(topology, 2, 0); trip.Begin();
            transit.RestoreTripExecution(trip, 0, 1, true, false, 2, 0);
            var bank = new ElevatorBank(0, 29, new[] { new ElevatorCar(new EntityId(1), 29, 8, new ElevatorTimingConfig(20, 2, 2)) });
            var passenger = new ElevatorPassenger(trip.PersonId, 2, 0) { WaitTicks = 14 };
            bank.EnqueuePassenger(passenger);
            transit.Advance(new Tick(15), topology, bank, null);
            Assert.That(bank.IsPassengerInQueue(trip.PersonId, out _), Is.False);
            Assert.That(trip.WaitTicks, Is.EqualTo(15));
            Assert.That(transit.ActiveTrips[0].Route.Legs, Has.None.Matches<TransitEdge>(e => e.Mode == TransitMode.Elevator));
            transit.Advance(new Tick(16), topology, bank, null);
            Assert.That(trip.WaitTicks, Is.EqualTo(15));
            Assert.That(trip.State, Is.EqualTo(TripState.Completed));
        }

        [Test]
        public void ReconsiderationRequiresThreeMinutesOfBenefitAndFiveMinuteCadence()
        {
            var topology = CreateTopology(4);
            var transit = new TransitExecutionSystem();
            var trip = Trip(topology, 4, 0); trip.Begin();
            transit.RestoreTripExecution(trip, 0, 1, true, false, 4, 0);
            var bank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(1), 0, 8, ElevatorTimingConfig.FromSeconds(40, 5, 10)) });
            var passenger = new ElevatorPassenger(trip.PersonId, 4, 0) { WaitTicks = 14 };
            bank.EnqueuePassenger(passenger);
            transit.Advance(new Tick(15), topology, bank, null);
            Assert.That(bank.IsPassengerInQueue(trip.PersonId, out _), Is.True,
                "An improvement below 180 seconds cannot discard a held queue position.");
            Assert.That(transit.ActiveTrips[0].LastReconsiderationTick, Is.EqualTo(15));
            var restored = new TransitExecutionSystem();
            restored.RestoreFromSaveData(transit.ToSaveData(), topology, null);
            restored.Advance(new Tick(16), topology, ElevatorBank.FromSaveData(bank.ToSaveData()), null);
            Assert.That(restored.ActiveTrips[0].LastReconsiderationTick, Is.EqualTo(15));
            Assert.That(restored.ActiveTrips[0].Trip.WaitTicks, Is.Zero);
        }

        [Test]
        public void InfeasibleLateDiversionKeepsTheHeldQueuePosition()
        {
            var topology = CreateTopology(29);
            var transit = new TransitExecutionSystem();
            var trip = Trip(topology, 4, 0); trip.Begin();
            transit.RestoreTripExecution(trip, 0, 1, true, false, 4, 0);
            var bank = new ElevatorBank(0, 29, new[] { new ElevatorCar(new EntityId(1), 29, 8, new ElevatorTimingConfig(20, 2, 2)) });
            bank.EnqueuePassenger(new ElevatorPassenger(trip.PersonId, 4, 0) { WaitTicks = 14 });
            transit.Advance(new Tick(359), topology, bank, null);
            Assert.That(bank.IsPassengerInQueue(trip.PersonId, out _), Is.True);
            Assert.That(trip.WaitTicks, Is.Zero);
        }

        [TestCase("remaining")]
        [TestCase("duration")]
        [TestCase("tick_projection")]
        [TestCase("legacy_mode")]
        [TestCase("future_reconsideration")]
        [TestCase("state")]
        [TestCase("purpose")]
        [TestCase("location_kind")]
        public void ImpossibleMovementCheckpointIsRejected(string corruption)
        {
            var topology = CreateTopology(2);
            var transit = new TransitExecutionSystem();
            transit.SubmitTrip(Trip(topology, 0, 2), topology, new Tick(0), null);
            transit.Advance(new Tick(1), topology, new ElevatorBank(0, 2, null), null);
            var save = transit.ToSaveData();
            Assert.That(save[0].legDurationSeconds, Is.EqualTo(45));
            Assert.That(save[0].legRemainingSeconds, Is.EqualTo(33));
            if (corruption == "remaining") save[0].legRemainingSeconds = 6000;
            if (corruption == "duration") save[0].legDurationSeconds = 46;
            if (corruption == "tick_projection") save[0].legRemainingTicks = 100;
            if (corruption == "legacy_mode") save[0].legacyLegProgress = true;
            if (corruption == "future_reconsideration") save[0].lastReconsiderationTick = long.MaxValue;
            if (corruption == "state") save[0].state = 999;
            if (corruption == "purpose") save[0].purpose = 999;
            if (corruption == "location_kind") save[0].originLocationKind = 999;
            Assert.Throws<System.ArgumentException>(() => new TransitExecutionSystem().RestoreFromSaveData(save, topology, null));
        }

        [TestCase("minute_travel")]
        [TestCase("minute_door")]
        [TestCase("minute_dwell")]
        [TestCase("phase")]
        [TestCase("direction")]
        [TestCase("moving_without_direction")]
        [TestCase("negative_timer")]
        [TestCase("timer_projection")]
        [TestCase("bank_version")]
        public void ImpossibleCarTemporalCheckpointIsRejected(string corruption)
        {
            var bank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(1), 0, 8, new ElevatorTimingConfig(3, 2, 2)) });
            var save = bank.ToSaveData();
            if (corruption == "minute_travel") save.cars[0].floorTravelSeconds = 179;
            if (corruption == "minute_door") save.cars[0].doorCycleSeconds = 119;
            if (corruption == "minute_dwell") save.cars[0].dwellSeconds = 119;
            if (corruption == "phase") save.cars[0].phase = 999;
            if (corruption == "direction") save.cars[0].direction = 999;
            if (corruption == "moving_without_direction") { save.cars[0].phase = (int)ElevatorCarPhase.Moving; save.cars[0].timerTicksRemaining = 1; }
            if (corruption == "negative_timer") save.cars[0].timerTicksRemaining = -1;
            if (corruption == "timer_projection") save.cars[0].timerSecondsRemaining = 100;
            if (corruption == "bank_version") save.timingDataVersion = 999;
            Assert.Throws<System.ArgumentException>(() => ElevatorBank.FromSaveData(save));
        }

        [Test]
        public void UpgradeRemainderBeyondNewSpeedRetainsOriginalPhaseAndExactReplay()
        {
            var bank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(1), 0, 8, new ElevatorTimingConfig(3, 2, 2)) });
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(10), 1, 4));
            bank.Advance(new Tick(1));
            bank.SetTier(5);
            var save = bank.ToSaveData();
            Assert.That(save.cars[0].timerSecondsRemaining, Is.EqualTo(120));
            Assert.That(save.cars[0].phaseDurationSeconds, Is.EqualTo(180));
            Assert.That(save.cars[0].floorTravelSeconds, Is.EqualTo(6));
            var restored = ElevatorBank.FromSaveData(save);
            for (var tick = 2; tick <= 5; tick++)
            {
                bank.Advance(new Tick(tick)); restored.Advance(new Tick(tick));
                Assert.That(restored.Cars[0].CurrentFloor, Is.EqualTo(bank.Cars[0].CurrentFloor));
                Assert.That(restored.Cars[0].TimerSecondsRemaining, Is.EqualTo(bank.Cars[0].TimerSecondsRemaining));
                Assert.That(restored.Cars[0].Phase, Is.EqualTo(bank.Cars[0].Phase));
                Assert.That(restored.DeliveredCount, Is.EqualTo(bank.DeliveredCount));
            }
        }

        [TestCase("checkpoint_future")]
        [TestCase("reconsideration_future")]
        [TestCase("reconsideration_before_departure")]
        [TestCase("reconsideration_after_recorded_progress")]
        public void MovementChronologyUsesAuthoritativeAggregateClock(string corruption)
        {
            var topology = CreateTopology(2);
            var transit = new TransitExecutionSystem();
            transit.SubmitTrip(Trip(topology, 0, 2), topology, new Tick(0), null);
            transit.Advance(new Tick(1), topology, new ElevatorBank(0, 2, null), null);
            var save = transit.ToSaveData();
            if (corruption == "checkpoint_future") save[0].checkpointTick = 2;
            if (corruption == "reconsideration_future") save[0].lastReconsiderationTick = 2;
            if (corruption == "reconsideration_before_departure") { save[0].departureTick = 1; save[0].lastReconsiderationTick = 0; }
            if (corruption == "reconsideration_after_recorded_progress") save[0].lastReconsiderationTick = 2;
            var clock = new Tick(corruption == "reconsideration_after_recorded_progress" ? 2 : 1);
            Assert.Throws<System.ArgumentException>(() => new TransitExecutionSystem().RestoreFromSaveData(save, topology, null, clock));
        }

        [Test]
        public void EarlierPhysicalCheckpointVersionReplaysWithAuthoritativeClock()
        {
            var topology = CreateTopology(2);
            var transit = new TransitExecutionSystem();
            transit.SubmitTrip(Trip(topology, 0, 2), topology, new Tick(0), null);
            transit.Advance(new Tick(1), topology, new ElevatorBank(0, 2, null), null);
            var save = transit.ToSaveData(); save[0].movementDataVersion = 1;
            var restored = new TransitExecutionSystem();
            restored.RestoreFromSaveData(save, topology, null, new Tick(1));
            var trip = restored.ActiveTrips[0].Trip;
            restored.Advance(new Tick(2), topology, new ElevatorBank(0, 2, null), null);
            Assert.That(trip.CompletionTick.Value.Value, Is.EqualTo(2));
            save[0].lastReconsiderationTick = 2;
            Assert.Throws<System.ArgumentException>(() => new TransitExecutionSystem().RestoreFromSaveData(save, topology, null, new Tick(1)));
        }

        [TestCase("remaining")]
        [TestCase("phase_version")]
        [TestCase("phase_units")]
        public void PhaseDurationCheckpointRejectsImpossibleRemainderAndVersion(string corruption)
        {
            var bank = new ElevatorBank(0, 4, new[] { new ElevatorCar(new EntityId(1), 0, 8) });
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(10), 4, 0));
            bank.Advance(new Tick(1));
            var save = bank.ToSaveData();
            // At 60 seconds the car has arrived, opened and is partly loading.
            if (corruption == "remaining") { save.cars[0].timerSecondsRemaining = 6000; save.cars[0].timerTicksRemaining = 100; }
            if (corruption == "phase_version") save.cars[0].phaseTimingDataVersion = 999;
            if (corruption == "phase_units") { save.cars[0].usesSeconds = false; save.cars[0].floorTravelSeconds = 60; save.cars[0].doorCycleSeconds = 60; save.cars[0].dwellSeconds = 60; save.cars[0].phaseDurationSeconds = 179; }
            Assert.Throws<System.ArgumentException>(() => ElevatorBank.FromSaveData(save));
        }

        [Test]
        public void AuthoredZeroDoorAndLoadingTimesStillProduceReplayableIdleCheckpoint()
        {
            var bank = new ElevatorBank(0, 1, new[] { new ElevatorCar(new EntityId(1), 0, 8, new ElevatorTimingConfig(1, 0, 0)) });
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(10), 0, 1));
            for (var tick = 1; tick <= 7; tick++) bank.Advance(new Tick(tick));
            Assert.That(bank.Cars[0].Phase, Is.EqualTo(ElevatorCarPhase.Idle));
            var restored = ElevatorBank.FromSaveData(bank.ToSaveData());
            Assert.That(restored.DeliveredCount, Is.EqualTo(1));
            Assert.That(restored.Cars[0].TimerTicksRemaining, Is.Zero);
        }

        [Test]
        public void PreviousWriterIdleBoundaryMigratesWithoutChangingFutureCustomTiming()
        {
            var payload = PreviousWriterIdleCheckpoint();
            var bank = ElevatorBank.FromSaveData(payload);
            Assert.That(bank.Cars[0].TimerTicksRemaining, Is.Zero);
            Assert.That(bank.Cars[0].Timing.UsesSeconds, Is.False);
            Assert.That(bank.Cars[0].Timing.FloorTravelSeconds, Is.EqualTo(60));
            Assert.That(bank.DeliveredCount, Is.EqualTo(1));
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(11), 1, 0));
            var restored = ElevatorBank.FromSaveData(bank.ToSaveData());
            for (var tick = 8; tick <= 14; tick++)
            {
                bank.Advance(new Tick(tick)); restored.Advance(new Tick(tick));
                Assert.That(restored.Cars[0].Phase, Is.EqualTo(bank.Cars[0].Phase));
                Assert.That(restored.Cars[0].TimerTicksRemaining, Is.EqualTo(bank.Cars[0].TimerTicksRemaining));
                Assert.That(restored.DeliveredCount, Is.EqualTo(bank.DeliveredCount));
            }
            Assert.That(bank.DeliveredCount, Is.EqualTo(2));
        }

        [TestCase("new_provenance")]
        [TestCase("different_negative")]
        [TestCase("physical_clock")]
        [TestCase("nonzero_closing")]
        [TestCase("moving")]
        public void OldIdleBoundaryMigrationDoesNotRelaxNewOrUnrelatedNegativeTimers(string corruption)
        {
            var payload = PreviousWriterIdleCheckpoint();
            var car = payload.cars[0];
            if (corruption == "new_provenance") car.phaseTimingDataVersion = 1;
            if (corruption == "different_negative") car.timerTicksRemaining = -2;
            if (corruption == "physical_clock") car.usesSeconds = true;
            if (corruption == "nonzero_closing") car.doorCycleSeconds = 60;
            if (corruption == "moving") { car.phase = (int)ElevatorCarPhase.Moving; car.direction = (int)ElevatorDirection.Down; }
            Assert.Throws<System.ArgumentException>(() => ElevatorBank.FromSaveData(payload));
        }

        [Test]
        public void PreviousWriterLegacyDurationMigratesAndReplaysTheSameHeldElevatorTrip()
        {
            var topology = CreateTopology(1);
            var transit = new TransitExecutionSystem();
            transit.RestoreFromSaveData(PreviousWriterLegacyTripCheckpoint(), topology, null, new Tick(0));
            var execution = transit.ActiveTrips[0];
            Assert.That(execution.LegacyLegProgress, Is.True);
            Assert.That(execution.LegRemainingTicks, Is.EqualTo(1));
            Assert.That(execution.LegRemainingSeconds, Is.EqualTo(60));
            Assert.That(execution.LegDurationSeconds, Is.EqualTo(600));
            var restored = new TransitExecutionSystem();
            restored.RestoreFromSaveData(transit.ToSaveData(), topology, null, new Tick(0));
            var bank = new ElevatorBank(0, 1, new[] { new ElevatorCar(new EntityId(1), 0, 8) });
            bank.EnqueuePassenger(new ElevatorPassenger(execution.Trip.PersonId, 0, 1));
            var secondBank = ElevatorBank.FromSaveData(bank.ToSaveData());
            var firstTrip = execution.Trip; var secondTrip = restored.ActiveTrips[0].Trip;
            transit.Advance(new Tick(1), topology, bank, null);
            restored.Advance(new Tick(1), topology, secondBank, null);
            Assert.That(firstTrip.CompletionTick.Value.Value, Is.EqualTo(1));
            Assert.That(secondTrip.CompletionTick, Is.EqualTo(firstTrip.CompletionTick));
            Assert.That(secondTrip.WaitTicks, Is.EqualTo(firstTrip.WaitTicks));
        }

        [TestCase("new_version")]
        [TestCase("physical_progress")]
        [TestCase("different_duration")]
        [TestCase("tick_projection")]
        [TestCase("overrun")]
        [TestCase("future_reconsideration")]
        public void PreviousDurationMigrationDoesNotRelaxStrictTemporalRejection(string corruption)
        {
            var payload = PreviousWriterLegacyTripCheckpoint();
            var trip = payload[0];
            if (corruption == "new_version") trip.movementDataVersion = 2;
            if (corruption == "physical_progress") trip.legacyLegProgress = false;
            if (corruption == "different_duration") trip.legDurationSeconds = 61;
            if (corruption == "tick_projection") trip.legRemainingTicks = 2;
            if (corruption == "overrun") { trip.legRemainingSeconds = 660; trip.legRemainingTicks = 11; }
            if (corruption == "future_reconsideration") trip.lastReconsiderationTick = 1;
            Assert.Throws<System.ArgumentException>(() => new TransitExecutionSystem().RestoreFromSaveData(payload, CreateTopology(1), null, new Tick(0)));
        }

        // Exact semantic reconstruction of unmodified payloads emitted AND reloaded by the prior frozen assembly.
        // Provenance: /tmp/task2-fix1-compat-probe/bank-v1.json, trip-v1.json and old/Program.cs;
        // copies are retained beside task-2-report.md in task-2-evidence/fix2-old-writer/.
        private static ElevatorBankSaveData PreviousWriterIdleCheckpoint() => new ElevatorBankSaveData
        {
            timingDataVersion = 1, minFloor = 0, maxFloor = 1, tier = 1, pickups = new ElevatorPickupSaveData[0],
            cars = new[] { new ElevatorCarSaveData { timingDataVersion = 1, usesSeconds = false,
                floorTravelSeconds = 60, doorCycleSeconds = 0, dwellSeconds = 0, timerSecondsRemaining = 0,
                targetFloors = new int[0], id = 1, currentFloor = 1, capacity = 8,
                phase = (int)ElevatorCarPhase.Idle, direction = (int)ElevatorDirection.None, timerTicksRemaining = -1,
                passengers = new ElevatorPassengerSaveData[0] } },
            queuedPassengers = new ElevatorPassengerSaveData[0],
            deliveredPassengers = new[] { new ElevatorPassengerSaveData { personId = 10, originFloor = 0, destinationFloor = 1, waitTicks = 1, rideTicks = 4 } }
        };

        private static ActiveTripSaveData[] PreviousWriterLegacyTripCheckpoint()
        {
            var route = new TransitRouteSaveData { originNodeId = 1001, destinationNodeId = 1011,
                originX = 0, originFloor = 0, destinationX = 0, destinationFloor = 1,
                legs = new[] { new TransitEdgeSaveData { fromNodeId = 1001, toNodeId = 1011, cost = 10,
                    mode = (int)TransitMode.Elevator, durationDataVersion = 1, durationSeconds = 10, hasPhysicalDuration = true } } };
            return new[] { new ActiveTripSaveData { movementDataVersion = 1, legRemainingSeconds = 60, legDurationSeconds = 60,
                legacyLegProgress = true, lastReconsiderationTick = long.MinValue, routeDataVersion = 1,
                plannedRoute = route, executionRoute = route, tripId = 9000, personId = 9001, originRoomId = 1000,
                destinationRoomId = 1010, originLocationKind = (int)WorldLocationKind.Room,
                destinationLocationKind = (int)WorldLocationKind.Room, purpose = (int)TripPurpose.Work,
                departureTick = 0, state = (int)TripState.InProgress, waitTicks = 0, currentLegIndex = 0,
                legRemainingTicks = 1, isQueuedInElevator = true, isRidingElevator = false, currentFloor = 0, currentX = 0 } };
        }

        internal static TripRecord Trip(BuildingTopologyState topology, int origin, int destination)
        {
            var route = new TransitRoutePlanner(topology.TransitGraph).FindRoute(new EntityId(1001 + origin * 10), new EntityId(1001 + destination * 10));
            return new TripRecord(new EntityId(9000), new EntityId(9001),
                WorldLocation.InRoom(new EntityId(1000 + origin * 10)), WorldLocation.InRoom(new EntityId(1000 + destination * 10)),
                TripPurpose.Work, new Tick(0), route);
        }

        internal static BuildingTopologyState CreateTopology(int highest)
        {
            var floors = new List<FloorTopology>();
            for (var f = 0; f <= highest; f++)
            {
                var id = 1000 + f * 10;
                var room = new Room(new EntityId(id), new ContentId("residential:room"), new CellBounds(f, 0, 8),
                    new[] { new EntityId(id + 1), new EntityId(id + 2), new EntityId(id + 3) }, 8);
                floors.Add(new FloorTopology(f, new[] { room }, new[]
                {
                    new Portal(new EntityId(id + 1), PortalType.ElevatorShaftDoor, new CellCoordinate(0, f), room.Id),
                    new Portal(new EntityId(id + 2), PortalType.StairwellDoor, new CellCoordinate(3, f), room.Id),
                    new Portal(new EntityId(id + 3), PortalType.Door, new CellCoordinate(7, f), room.Id)
                }));
            }
            return BuildingTopologyState.FromBuildingTopology(new BuildingTopology(floors));
        }
    }
}
