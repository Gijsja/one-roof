using NUnit.Framework;
using OneRoof.Domain.Crisis;
using OneRoof.Domain.CivilAction;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class CaretakerCrisisTests
    {
        private static CivilActionSignal Signal(float pressure = .8f, int members = 2) => new CivilActionSignal("tenant_union", pressure, members, "arrears", new[] { 11, 12 }, new[] { 1, 2 });
        private static CaretakerCrisisState Ready()
        {
            var state = new CaretakerCrisisState();
            for (var day = 1; day <= 3; day++)
                state.Evaluate(day * 1440, new[] { Signal() });
            return state;
        }

        private static BuildingTopologyState Tower(bool stairs = true)
        {
            var topology = new BuildingTopologyState();
            for (var floor = 0; floor < 3; floor++)
                topology.Execute(new BuildFloorSlabCommand(floor, -14, 17), new Tick(0));
            if (stairs)
                topology.Execute(new BuildStairwellCommand(-14, -13, 0, 2), new Tick(0));
            return topology;
        }

        [Test]
        public void EscalationRequiresThreeDistinctSettlementsAndConnectedEvidence()
        {
            var state = new CaretakerCrisisState();
            state.Evaluate(1, new[] { Signal(members: 1) });
            Assert.That(state.Phase, Is.EqualTo(CaretakerCrisisPhase.Dormant));
            state.Evaluate(2, new[] { Signal() });
            state.Evaluate(2, new[] { Signal() });
            Assert.That(state.WarningDays, Is.EqualTo(1));
            state.Evaluate(3, new[] { Signal() });
            state.Evaluate(4, new[] { Signal() });
            Assert.That(state.Phase, Is.EqualTo(CaretakerCrisisPhase.Ready));
            state.Evaluate(5, new[] { Signal(.1f) });
            state.Evaluate(6, new[] { Signal(.1f) });
            Assert.That(state.Phase, Is.EqualTo(CaretakerCrisisPhase.Dormant));
        }

        [Test]
        public void DisconnectedLayoutRejectsStartAndActualStairsDefineRoute()
        {
            var state = Ready();
            Assert.That(state.CanStart(Tower(false), out var reason), Is.False);
            Assert.That(reason, Does.Contain("stairs"));
            Assert.That(state.Start(Tower()), Is.True);
            Assert.That(state.Route.Count, Is.EqualTo(3));
            Assert.That(state.Route[0].ExitX, Is.EqualTo(-14000));
            Assert.That(state.Route[1].EntryX, Is.EqualTo(-14000));
        }

        [Test]
        public void SaveMidStrikeRetainsSingleHitAndLoadedEncounterPauses()
        {
            var state = Ready();
            state.Start(Tower());
            var saved = state.ToSaveData();
            saved.routeIndex = 1;
            saved.x = 0;
            saved.opponents = new[]
            {
                new CaretakerOpponentSaveData
                {
                    id = 1,
                    residentId = 11,
                    x = 800,
                    health = 35,
                    facing = -1,
                    timer = 100
                }
            };
            state = CaretakerCrisisState.FromSaveData(saved);
            state.SetPaused(false);
            state.Step(new CaretakerCombatInput(0, punch: true));
            for (var i = 0; i < 5; i++)
                state.Step(default);
            Assert.That(state.Opponents[0].Health, Is.EqualTo(20));
            var loaded = CaretakerCrisisState.FromSaveData(state.ToSaveData());
            Assert.That(loaded.Paused, Is.True);
            var clock = loaded.Clock;
            loaded.Step(default);
            Assert.That(loaded.Clock, Is.EqualTo(clock));
            loaded.SetPaused(false);
            for (var i = 0; i < 10; i++)
                loaded.Step(default);
            Assert.That(loaded.Opponents[0].Health, Is.EqualTo(20));
        }

        [Test]
        public void ExistingAttackOwnsTicketEvenWhenLaterInOpponentOrder()
        {
            var state = Ready();
            state.Start(Tower());
            var data = state.ToSaveData();
            data.routeIndex = 1;
            data.x = 0;
            data.opponents = new[]
            {
                new CaretakerOpponentSaveData
                {
                    id = 1,
                    x = 800,
                    health = 35,
                    timer = 0
                },
                new CaretakerOpponentSaveData
                {
                    id = 2,
                    x = -800,
                    health = 35,
                    attackFrame = 10
                }
            };
            state = CaretakerCrisisState.FromSaveData(data);
            state.SetPaused(false);
            state.Step(default);
            Assert.That(state.Opponents[0].AttackFrame, Is.Zero);
            Assert.That(state.Opponents[1].AttackFrame, Is.EqualTo(11));
        }

        [Test]
        public void PracticePreservesCrisisAndNeverIssuesEvictions()
        {
            var state = Ready();
            Assert.That(state.StartPractice(Tower()), Is.True);
            Assert.That(state.Practice, Is.True);
            var loaded = CaretakerCrisisState.FromSaveData(state.ToSaveData());
            loaded.Exit();
            Assert.That(loaded.Phase, Is.EqualTo(CaretakerCrisisPhase.Ready));
            Assert.That(loaded.Evictions, Is.Empty);
            Assert.That(loaded.AffectedResidentIds, Is.EqualTo(new[] { 11, 12 }));
            Assert.That(loaded.Practice, Is.False);
        }

        [Test]
        public void FailureRetryExitProduceNoHousingConsequences()
        {
            var state = Ready();
            state.Start(Tower());
            var data = state.ToSaveData();
            data.clock = 1;
            state = CaretakerCrisisState.FromSaveData(data);
            state.SetPaused(false);
            state.Step(default);
            Assert.That(state.Phase, Is.EqualTo(CaretakerCrisisPhase.Failed));
            Assert.That(state.Evictions, Is.Empty);
            Assert.That(state.Retry(Tower()), Is.True);
            state.Exit();
            Assert.That(state.Phase, Is.EqualTo(CaretakerCrisisPhase.Ready));
        }

        [Test]
        public void CompletionRecordsOnlyNamedOrganizedResidentsOnce()
        {
            var state = Ready();
            state.Start(Tower());
            var data = state.ToSaveData();
            data.routeIndex = 2;
            data.floorParticipantIds = new[]
            {
                11,
                12
            };
            data.nextParticipant = 2;
            data.encounterResidentIds = new[]
            {
                11,
                12
            };
            data.x = data.route[2].exitX;
            data.opponents = new CaretakerOpponentSaveData[0];
            state = CaretakerCrisisState.FromSaveData(data);
            state.SetPaused(false);
            Assert.That(state.Advance(Tower()), Is.True);
            Assert.That(state.Phase, Is.EqualTo(CaretakerCrisisPhase.Resolved));
            Assert.That(state.Evictions.Count, Is.EqualTo(2));
            Assert.That(state.Advance(Tower()), Is.False);
            state.MarkConsequencesApplied();
            Assert.That(CaretakerCrisisState.FromSaveData(state.ToSaveData()).ConsequencesApplied, Is.True);
        }

        [Test]
        public void CorruptSaveCannotStrandActiveCombatWithoutRoute()
        {
            var state = CaretakerCrisisState.FromSaveData(new CaretakerCrisisSaveData { phase = (int)CaretakerCrisisPhase.Active, routeIndex = 999, health = 999 });
            Assert.That(state.Phase, Is.EqualTo(CaretakerCrisisPhase.Ready));
            Assert.That(state.CurrentFloor, Is.Zero);
            Assert.That(state.Health, Is.EqualTo(100));
        }

        [Test]
        public void LoadedRouteMustMatchLiveStairsBeforeResumeOrAdvance()
        {
            var state = Ready();
            state.Start(Tower());
            Assert.That(state.ValidateRoute(Tower(false), out _), Is.False);
        }

        [Test]
        public void CrouchingAttackIsAvailableAndMovementCannotLeaveSlab()
        {
            var state = Ready();
            state.Start(Tower());
            state.Step(new CaretakerCombatInput(-1, punch: true, duck: true));
            Assert.That(state.Ducking, Is.True);
            Assert.That(state.Attack, Is.EqualTo(CaretakerAttack.Punch));
            for (var i = 0; i < 600; i++)
                state.Step(new CaretakerCombatInput(-1));
            Assert.That(state.X, Is.GreaterThanOrEqualTo(state.Route[0].MinX));
        }

        [Test]
        public void CombatDeterministicallyContinuesAfterMidAttackSave()
        {
            var first = Ready();
            first.Start(Tower());
            first.Step(new CaretakerCombatInput(1, kick: true, jump: true));
            var second = CaretakerCrisisState.FromSaveData(first.ToSaveData());
            second.SetPaused(false);
            for (var step = 0; step < 240; step++)
            {
                var input = new CaretakerCombatInput(step % 80 < 40 ? 1 : -1, punch: step % 30 == 0, kick: step % 45 == 0, duck: step % 60 > 50, jump: step % 90 == 0);
                first.Step(input);
                second.Step(input);
                Assert.That(second.X, Is.EqualTo(first.X));
                Assert.That(second.Health, Is.EqualTo(first.Health));
                Assert.That(second.Clock, Is.EqualTo(first.Clock));
                Assert.That(second.Attack, Is.EqualTo(first.Attack));
            }
        }

        [Test]
        public void EvictionProvenanceUsesSavedHomeFloorAndExcludesUnfoughtSupporters()
        {
            var state = Ready();
            state.Start(Tower());
            var data = state.ToSaveData();
            data.routeIndex = 2;
            data.floorParticipantIds = new[]
            {
                11,
                12
            };
            data.nextParticipant = 2;
            data.x = data.route[2].exitX;
            data.opponents = new CaretakerOpponentSaveData[0];
            data.encounterResidentIds = new[]
            {
                11
            };
            data.residentFloors = new[]
            {
                new CaretakerResidentFloorSaveData
                {
                    residentId = 11,
                    floor = 1
                }
            };
            state = CaretakerCrisisState.FromSaveData(data);
            state.SetPaused(false);
            state.Advance(Tower());
            Assert.That(state.Evictions.Count, Is.EqualTo(1));
            Assert.That(state.Evictions[0].ResidentId, Is.EqualTo(11));
            Assert.That(state.Evictions[0].Floor, Is.EqualTo(1));
        }

        [Test]
        public void WholeFloorGroupUsesBoundedWavesAndSavesRemainingProgress()
        {
            var state = new CaretakerCrisisState();
            var signal = new CivilActionSignal("tenant_union", .8f, 9, "arrears", new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19 }, new[] { 1 });
            for (var day = 1; day <= 3; day++)
                state.Evaluate(day, new[] { signal });
            state.Start(Tower());
            var data = state.ToSaveData();
            data.routeIndex = 1;
            data.floorParticipantIds = signal.ResidentIds;
            data.nextParticipant = 4;
            data.encounterResidentIds = new[]
            {
                11,
                12,
                13,
                14
            };
            data.opponents = new[]
            {
                new CaretakerOpponentSaveData
                {
                    id = 1,
                    residentId = 11,
                    health = 0
                }
            };
            state = CaretakerCrisisState.FromSaveData(data);
            state.SetPaused(false);
            state.Step(default);
            Assert.That(state.Opponents.Count, Is.EqualTo(4));
            Assert.That(state.Opponents[0].ResidentId, Is.EqualTo(15));
            Assert.That(state.FloorCleared, Is.False);
            data = state.ToSaveData();
            foreach (var opponent in data.opponents)
                opponent.health = 0;
            state = CaretakerCrisisState.FromSaveData(data);
            state.SetPaused(false);
            state.Step(default);
            Assert.That(state.Opponents.Count, Is.EqualTo(1));
            Assert.That(state.Opponents[0].ResidentId, Is.EqualTo(19));
            Assert.That(state.Opponents[0].Role, Is.EqualTo(CaretakerOpponentRole.FloorBoss));
            Assert.That(state.FloorCleared, Is.False);
            data = state.ToSaveData();
            data.opponents[0].health = 0;
            data.x = data.route[1].exitX;
            state = CaretakerCrisisState.FromSaveData(data);
            state.SetPaused(false);
            Assert.That(state.FloorCleared, Is.True);
            Assert.That(state.Advance(Tower()), Is.True);
            Assert.That(state.Evictions.Count, Is.EqualTo(9));
        }

        [Test]
        public void CorruptSavedEvictionsCannotTargetUnrelatedResidentsAndOpponentStaysInCorridor()
        {
            var state = Ready();
            state.Start(Tower());
            var data = state.ToSaveData();
            data.phase = (int)CaretakerCrisisPhase.Resolved;
            data.encounterResidentIds = new[]
            {
                11,
                999
            };
            data.evictions = new[]
            {
                new CaretakerEvictionSaveData
                {
                    residentId = 11
                },
                new CaretakerEvictionSaveData
                {
                    residentId = 999
                },
                new CaretakerEvictionSaveData
                {
                    residentId = 12
                }
            };
            data.opponents = new[]
            {
                new CaretakerOpponentSaveData
                {
                    id = 1,
                    role = (int)CaretakerOpponentRole.Thrower,
                    x = int.MaxValue,
                    health = 35
                }
            };
            state = CaretakerCrisisState.FromSaveData(data);
            Assert.That(state.Evictions.Count, Is.EqualTo(1));
            Assert.That(state.Evictions[0].ResidentId, Is.EqualTo(11));
            Assert.That(state.Opponents[0].X, Is.LessThan(state.Route[0].MaxX));
        }

        [Test]
        public void InputBufferingQueuesAttackDuringActiveStrikeAndTriggersUponRecovery()
        {
            var state = Ready();
            state.Start(Tower());
            state.Step(new CaretakerCombatInput(0, punch: true));
            Assert.That(state.Attack, Is.EqualTo(CaretakerAttack.Punch));
            Assert.That(state.AttackFrame, Is.EqualTo(1));

            // Advance through active frames into recovery (punch duration is 18 frames)
            for (var frame = 2; frame <= 14; frame++)
                state.Step(default);
            Assert.That(state.Attack, Is.EqualTo(CaretakerAttack.Punch));
            Assert.That(state.AttackFrame, Is.EqualTo(14));

            // Queue a Kick input on frame 15 (within 6 ticks of frame 18 recovery)
            state.Step(new CaretakerCombatInput(0, kick: true));
            Assert.That(state.Attack, Is.EqualTo(CaretakerAttack.Punch));
            Assert.That(state.KickBuffer, Is.GreaterThan(0));

            // Step through remaining recovery frames (frame 16, 17, 18)
            state.Step(default);
            state.Step(default);
            state.Step(default);
            Assert.That(state.Attack, Is.EqualTo(CaretakerAttack.None));

            // On the earliest frame upon recovery, queued kick triggers immediately
            state.Step(default);
            Assert.That(state.Attack, Is.EqualTo(CaretakerAttack.Kick));
            Assert.That(state.AttackFrame, Is.EqualTo(1));

            // Verify buffer expiration: queue punch on frame 5 of kick (duration 28; 23 ticks before recovery)
            for (var frame = 2; frame <= 5; frame++)
                state.Step(default);
            state.Step(new CaretakerCombatInput(0, punch: true));
            Assert.That(state.Attack, Is.EqualTo(CaretakerAttack.Kick));

            // Advance to end of kick (duration 28)
            for (var frame = 7; frame <= 28; frame++)
                state.Step(default);
            Assert.That(state.Attack, Is.EqualTo(CaretakerAttack.None));

            // On next step, punch buffer expired (> 6 ticks passed), so attack remains None
            state.Step(default);
            Assert.That(state.Attack, Is.EqualTo(CaretakerAttack.None));
        }

        [Test]
        public void JumpBufferingTriggersImmediateJumpUponLandingWhenPressedWithinBufferWindow()
        {
            var state = Ready();
            state.Start(Tower());
            state.Step(new CaretakerCombatInput(0, jump: true));
            Assert.That(state.JumpFrames, Is.EqualTo(35));

            // Advance while airborne until within 6 ticks of landing (jumpFrames == 5)
            while (state.JumpFrames > 5)
                state.Step(default);
            Assert.That(state.JumpFrames, Is.EqualTo(5));

            // Press Jump while airborne within 6 ticks of landing
            state.Step(new CaretakerCombatInput(0, jump: true));
            Assert.That(state.JumpBuffer, Is.GreaterThan(0));

            // Step until landing (jumpFrames reaches 0)
            while (state.JumpFrames > 0)
                state.Step(default);
            Assert.That(state.JumpFrames, Is.Zero);

            // On the frame upon touching ground, buffered jump immediately triggers
            state.Step(default);
            Assert.That(state.JumpFrames, Is.EqualTo(35));

            // Verify jump buffer expires if pressed outside 6-tick window:
            // Jump again, but press jump high in the air at jumpFrames == 25 (> 6 ticks before landing)
            while (state.JumpFrames > 25)
                state.Step(default);
            state.Step(new CaretakerCombatInput(0, jump: true));

            // Step until landing
            while (state.JumpFrames > 0)
                state.Step(default);
            Assert.That(state.JumpFrames, Is.Zero);

            // Buffer expired; should not jump on touching ground
            state.Step(default);
            Assert.That(state.JumpFrames, Is.Zero);
        }

        [Test]
        public void HitStopMicroFreezePausesOpponentDisplacementAndCreatesImpactWeight()
        {
            var state = Ready();
            state.Start(Tower());
            var data = state.ToSaveData();
            data.routeIndex = 1;
            data.x = 0;
            data.opponents = new[]
            {
                new CaretakerOpponentSaveData
                {
                    id = 1,
                    residentId = 11,
                    x = 600,
                    health = 35,
                    facing = -1,
                    timer = 100
                }
            };
            state = CaretakerCrisisState.FromSaveData(data);
            state.SetPaused(false);
            var initialX = state.Opponents[0].X;

            // Punch activeStart is frame 5
            state.Step(new CaretakerCombatInput(0, punch: true)); // frame 1
            state.Step(default); // frame 2
            state.Step(default); // frame 3
            state.Step(default); // frame 4
            state.Step(default); // frame 5 - punch connects!

            // Attack connected: damage dealt, hit-stop active on simulation and opponent
            Assert.That(state.Opponents[0].Health, Is.EqualTo(20));
            Assert.That(state.HitStop, Is.EqualTo(3));
            Assert.That(state.Opponents[0].HitStop, Is.EqualTo(3));
            // Physical impact weight: opponent is NOT displaced yet on connect frame
            Assert.That(state.Opponents[0].X, Is.EqualTo(initialX));

            // During hit-stop micro-pause (tick 1): opponent stays at initial X, attacker attackFrame frozen
            state.Step(default);
            Assert.That(state.Opponents[0].X, Is.EqualTo(initialX));
            Assert.That(state.AttackFrame, Is.EqualTo(5));

            // During hit-stop micro-pause (tick 2): opponent still at initial X
            state.Step(default);
            Assert.That(state.Opponents[0].X, Is.EqualTo(initialX));
            Assert.That(state.AttackFrame, Is.EqualTo(5));

            // Hit-stop finishes (tick 3): knockback displacement applied
            state.Step(default);
            Assert.That(state.HitStop, Is.Zero);
            Assert.That(state.Opponents[0].HitStop, Is.Zero);
            Assert.That(state.Opponents[0].X, Is.EqualTo(initialX + 450));

            // Verify mid-hit-stop state roundtrip through save/load
            var midData = state.ToSaveData();
            midData.hitStop = 2;
            midData.opponents[0].hitStop = 2;
            midData.opponents[0].pendingKnockback = 450;
            var restored = CaretakerCrisisState.FromSaveData(midData);
            Assert.That(restored.HitStop, Is.EqualTo(2));
            Assert.That(restored.Opponents[0].HitStop, Is.EqualTo(2));
        }
    }
}
