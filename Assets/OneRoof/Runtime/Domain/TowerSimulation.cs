using System;
using System.Collections.Generic;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Events;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Scrutiny;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain
{
    /// <summary>
    /// Master pure C# domain simulation orchestrator for the vertical city tower.
    /// Drives simulation clock, routine-based trip generation, hierarchical routing,
    /// leg-by-leg transit execution, economy, and demand-driven leasing.
    /// </summary>
    public sealed class TowerSimulation
    {
        private int _nextElevatorCarId = 500;
        private int _nextEntityId = 3000;

        public TowerSimulation(
            SimulationClock clock,
            BuildingTopologyState topology,
            PopulationState population,
            ElevatorBank elevatorBank,
            TowerEconomyState economy = null,
            IRandomStream randomStream = null,
            ScrutinyState scrutiny = null)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Topology = topology ?? throw new ArgumentNullException(nameof(topology));
            Population = population ?? throw new ArgumentNullException(nameof(population));
            ElevatorBank = elevatorBank ?? throw new ArgumentNullException(nameof(elevatorBank));
            Economy = economy ?? new TowerEconomyState();
            RandomStream = randomStream ?? new DeterministicRandomStream(1337);

            Planner = new TransitRoutePlanner(Topology.TransitGraph);
            TripGenerator = new ScheduleTripGenerator(Topology.ToSnapshot(), Topology.TransitGraph, Planner);
            Transit = new TransitExecutionSystem();
            Leasing = new LeasingDemandSystem();
            Needs = new ResidentNeedsSystem();
            Specialists = new SpecialistRoleSystem();
            Businesses = new BusinessState();
            ElectricalGrid = new ElectricalGridState();
            WaterWasteNetwork = new WaterWasteNetworkState();
            UtilityOperations = new UtilityOperationsState();
            Wellbeing = new ResidentWellbeingSystem();
            Scrutiny = scrutiny ?? new ScrutinyState();
        }

        public SimulationClock Clock { get; }

        public BuildingTopologyState Topology { get; }

        public PopulationState Population { get; }

        public ElevatorBank ElevatorBank { get; }

        public TowerEconomyState Economy { get; }

        public LeasingDemandSystem Leasing { get; }

        public IRandomStream RandomStream { get; }

        public TransitRoutePlanner Planner { get; private set; }

        public ScheduleTripGenerator TripGenerator { get; private set; }

        public TransitExecutionSystem Transit { get; }

        public ResidentNeedsSystem Needs { get; }
        public SpecialistRoleSystem Specialists { get; }
        public BusinessState Businesses { get; private set; }
        public ElectricalGridState ElectricalGrid { get; }
        public WaterWasteNetworkState WaterWasteNetwork { get; }
        public UtilityOperationsState UtilityOperations { get; }
        public ResidentWellbeingSystem Wellbeing { get; }
        public ScrutinyState Scrutiny { get; }

        public long CurrentTick => Clock.CurrentTick.Value;

        public int ResidentCount => Population.ResidentCount;

        public int ActiveTripCount => Transit.ActiveTripCount;

        public int TotalQueuedElevatorPassengers => ElevatorBank.TotalQueuedCount;

        public float AverageElevatorWaitTicks => ElevatorBank.AverageWaitTicks;

        /// <summary>Immutable electrical state derived from the authoritative topology at the time of request.</summary>
        public ElectricalGridSnapshot ElectricalGridSnapshot() => ElectricalGrid.Evaluate(Topology);

        /// <summary>Immutable water pressure and gravity-waste collection state derived from the authoritative topology.</summary>
        public WaterWasteNetworkSnapshot WaterWasteNetworkSnapshot() => WaterWasteNetwork.Evaluate(Topology);

        /// <summary>Mutable operational condition of installed utility equipment, projected without Unity dependencies.</summary>
        public UtilityOperationsSnapshot UtilityOperationsSnapshot() => UtilityOperations.Snapshot(Topology);

        public void AdvanceOneTick()
        {
            var previousTick = Clock.CurrentTick;
            Clock.Advance();
            var currentTick = Clock.CurrentTick;

            // 0. Advance resident needs (decay and replenishment based on activity)
            Needs.Advance(Population, currentTick);
            Specialists.Advance(Population, Topology, currentTick);
            UtilityOperations.Advance(Topology, Population);
            Wellbeing.Advance(Population, ElevatorBank, Specialists.ServiceEfficiencyMultiplier);
            Scrutiny.Advance(Topology, Population, Specialists.CrisisResponseMultiplier);

            // 1. Periodic autonomous leasing demand evaluation (every 10 ticks)
            if (currentTick.Value % 10 == 0)
            {
                Leasing.EvaluateLeasingDemand(Topology, Population, ElevatorBank, RandomStream, currentTick, ref _nextEntityId);
                Businesses.Advance(Topology, Population, ref _nextEntityId);
            }

            // 2. Periodic rental collection cycle (every 50 ticks)
            if (currentTick.Value % 50 == 0)
            {
                Economy.ProcessRentCycle(Topology, Population);
                Businesses.ProcessBusinessCycle(Population);
            }

            // 3. Generate scheduled routine trips when schedule blocks transition
            var trips = TripGenerator.GenerateTripsForTick(previousTick, currentTick, Population);
            for (var i = 0; i < trips.Count; i++)
            {
                Transit.SubmitTrip(trips[i], Topology, currentTick, Population);
            }

            // 4. Advance transit execution (walking legs, elevator queues, riding cars)
            Transit.Advance(currentTick, Topology, ElevatorBank, Population);
        }

        public void SyncTransitServices()
        {
            var currentGraph = Topology.TransitGraph;
            Planner = new TransitRoutePlanner(currentGraph);
            TripGenerator.UpdateTopology(Topology.ToSnapshot(), currentGraph, Planner);
        }

        // ── Command Seam & Validation ─────────────────────────────────────────

        public CommandResult CanExecute(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            if (Scrutiny.IsExpansionConstrained && (command is BuildFloorSlabCommand || command is BuildRoomCommand || command is AddElevatorShaftCommand || command is BuildStairwellCommand))
            {
                return CommandResult.Reject(new[]
                {
                    new CommandRejectionReason(new ContentId("scrutiny:expansion_constrained"), "External scrutiny is temporarily constraining further expansion. Improve capacity or household conditions, then allow pressure to recede.")
                });
            }

            switch (command)
            {
                case BuildFloorSlabCommand slabCmd:
                {
                    var cost = Economy.CalculateFloorSlabCost(slabCmd.Bounds);
                    if (!Economy.CanAfford(cost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for floor slab ({cost} required, Treasury: {Economy.CashBalance}).")
                        });
                    }
                    return Topology.CanExecute(slabCmd);
                }

                case BuildRoomCommand roomCmd:
                {
                    var cost = Economy.CalculateRoomCost(roomCmd.ContentType, roomCmd.Bounds);
                    if (!Economy.CanAfford(cost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for room ({cost} required, Treasury: {Economy.CashBalance}).")
                        });
                    }
                    return Topology.CanExecute(roomCmd);
                }

                case AddElevatorShaftCommand shaftCmd:
                {
                    var cost = Economy.CalculateElevatorShaftCost(shaftCmd.FloorSpan, shaftCmd.ShaftMaxX - shaftCmd.ShaftMinX + 1);
                    if (!Economy.CanAfford(cost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for elevator shaft ({cost} required, Treasury: {Economy.CashBalance}).")
                        });
                    }
                    return Topology.CanExecute(shaftCmd);
                }

                case BuildStairwellCommand stairCmd:
                {
                    var cost = Economy.CalculateStairwellCost(stairCmd.FloorSpan, stairCmd.StairMaxX - stairCmd.StairMinX + 1);
                    if (!Economy.CanAfford(cost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for stairwell ({cost} required, Treasury: {Economy.CashBalance}).")
                        });
                    }
                    return Topology.CanExecute(stairCmd);
                }

                case DemolishRoomCommand demoCmd:
                {
                    if (!Topology.Rooms.TryGetValue(demoCmd.RoomId, out var room))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("topology:room_not_found"), $"Room {demoCmd.RoomId} not found.")
                        });
                    }

                    var contentVal = room.ContentType.Value ?? "";
                    if (contentVal.Contains("lobby"))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("demolish:protected"), "Cannot demolish main reception lobby.")
                        });
                    }

                    if (contentVal.Contains("elevator_shaft"))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("demolish:protected"), "Elevator shafts cannot be demolished with room bulldozer.")
                        });
                    }

                    if (!demoCmd.Force && contentVal.StartsWith("residential:"))
                    {
                        var households = Population.Households;
                        for (var i = 0; i < households.Count; i++)
                        {
                            if (households[i].HomeRoomId.Equals(demoCmd.RoomId))
                            {
                                return CommandResult.Reject(new[]
                                {
                                    new CommandRejectionReason(new ContentId("demolish:occupied"), "Cannot demolish occupied apartment with active tenants.")
                                });
                            }
                        }
                    }

                    return Topology.CanExecute(demoCmd);
                }

                case AddElevatorCarCommand carCmd:
                {
                    if (carCmd.StartingFloor < 0 || carCmd.StartingFloor >= Topology.FloorCount)
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("transit:invalid_floor"), $"Elevator car must be placed within active tower floors (0..{Topology.FloorCount - 1}).")
                        });
                    }

                    if (ElevatorBank.Cars.Count >= ElevatorBank.MaxCarsPerBank)
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("transit:max_cars"), $"Elevator bank has reached maximum capacity ({ElevatorBank.MaxCarsPerBank} cars).")
                        });
                    }

                    if (!Economy.CanAfford(TowerEconomyState.ElevatorCarCost))
                    {
                        return CommandResult.Reject(new[]
                        {
                            new CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Insufficient funds for elevator car ({TowerEconomyState.ElevatorCarCost} required).")
                        });
                    }

                    return CommandResult.Success();
                }

                default:
                    return CommandResult.Reject(new[]
                    {
                        new CommandRejectionReason(new ContentId("command:unknown"), $"Unsupported command type '{command.GetType().Name}'.")
                    });
            }
        }

        public CommandResult ExecuteCommand(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            switch (command)
            {
                case BuildFloorSlabCommand slabCmd:
                    return BuildFloorSlab(slabCmd);
                case BuildRoomCommand roomCmd:
                    return BuildRoom(roomCmd);
                case AddElevatorShaftCommand shaftCmd:
                    return AddElevatorShaft(shaftCmd);
                case BuildStairwellCommand stairCmd:
                    return BuildStairwell(stairCmd);
                case DemolishRoomCommand demoCmd:
                    return DemolishRoom(demoCmd);
                case AddElevatorCarCommand carCmd:
                {
                    var canExec = CanExecute(carCmd);
                    if (!canExec.Accepted) return canExec;
                    AddElevatorCarUnchecked(carCmd.Capacity, carCmd.StartingFloor);
                    return CommandResult.Success();
                }
                default:
                    return CommandResult.Reject(new[]
                    {
                        new CommandRejectionReason(new ContentId("command:unknown"), $"Unsupported command type '{command.GetType().Name}'.")
                    });
            }
        }

        public CommandResult BuildFloorSlab(BuildFloorSlabCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var cost = Economy.CalculateFloorSlabCost(cmd.Bounds);
            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (result.Accepted)
            {
                Economy.TryDeduct(cost);
                Scrutiny.RecordExpansion(cmd.Bounds.Width);
                SyncTransitServices();
            }

            return result;
        }

        public CommandResult BuildRoom(BuildRoomCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var cost = Economy.CalculateRoomCost(cmd.ContentType, cmd.Bounds);
            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (result.Accepted)
            {
                Economy.TryDeduct(cost);
                Scrutiny.RecordExpansion(cmd.Bounds.Width);
                var content = cmd.ContentType.Value ?? string.Empty;
                if (content.Contains("diner") || content.Contains("amenity") || content.Contains("service")) Scrutiny.RecordCapacityOrServiceInvestment();
                SyncTransitServices();
            }

            return result;
        }

        public CommandResult AddElevatorShaft(AddElevatorShaftCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var cost = Economy.CalculateElevatorShaftCost(cmd.FloorSpan, cmd.ShaftMaxX - cmd.ShaftMinX + 1);
            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (result.Accepted)
            {
                Economy.TryDeduct(cost);
                Scrutiny.RecordExpansion(cmd.FloorSpan);
                ElevatorBank.ExpandFloorRange(cmd.BottomFloor, cmd.TopFloor);
                SyncTransitServices();
            }

            return result;
        }

        public CommandResult BuildStairwell(BuildStairwellCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var cost = Economy.CalculateStairwellCost(cmd.FloorSpan, cmd.StairMaxX - cmd.StairMinX + 1);
            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (result.Accepted)
            {
                Economy.TryDeduct(cost);
                Scrutiny.RecordExpansion(cmd.FloorSpan);
                SyncTransitServices();
            }

            return result;
        }

        public CommandResult DemolishRoom(DemolishRoomCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            if (Topology.Rooms.TryGetValue(cmd.RoomId, out var room))
            {
                var cost = Economy.CalculateRoomCost(room.ContentType, room.Bounds);
                var salvageRefund = cost / 2;

                var result = Topology.Execute(cmd, Clock.CurrentTick);
                if (result.Accepted && salvageRefund > 0)
                {
                    Economy.AddRevenue(salvageRefund);
                }

                if (result.Accepted)
                {
                    SyncTransitServices();
                }

                return result;
            }

            var fallbackResult = Topology.Execute(cmd, Clock.CurrentTick);
            if (fallbackResult.Accepted)
            {
                SyncTransitServices();
            }

            return fallbackResult;
        }

        public CommandResult AddElevatorCar(int capacity = 10, int startingFloor = 0)
        {
            return ExecuteCommand(new AddElevatorCarCommand(capacity, startingFloor));
        }

        private void AddElevatorCarUnchecked(int capacity, int startingFloor)
        {
            var carId = new EntityId(_nextElevatorCarId++);
            var car = new ElevatorCar(carId, startingFloor, capacity);
            ElevatorBank.AddCar(car);
            Economy.TryDeduct(TowerEconomyState.ElevatorCarCost);
            Scrutiny.RecordCapacityOrServiceInvestment();
        }

        public ResidentSpatialPosition GetResidentPosition(EntityId personId)
        {
            return Transit.GetResidentPosition(personId, Population, Topology);
        }

        public (long MaxWaitTicks, float AverageWaitTicks) GetFloorWaitMetrics(int floor)
        {
            return ElevatorBank.GetFloorWaitMetrics(floor);
        }

        public int GetQueueLength(int floor)
        {
            return ElevatorBank.GetQueueLength(floor);
        }

        /// <summary>
        /// Seeds the elevator bank with a synthetic morning-rush queue (one passenger per floor cycle)
        /// using entity IDs allocated from the simulation's own counter so they cannot collide with
        /// real <see cref="PopulationState"/> person IDs.  Only runs when the bank queue is empty;
        /// calling it multiple times is safe.
        /// </summary>
        public void SeedMorningRush()
        {
            if (ElevatorBank.TotalQueuedCount > 0)
            {
                return;
            }

            var floorRange = ElevatorBank.MaxFloor - ElevatorBank.MinFloor;
            if (floorRange <= 0) return;

            const int residentCount = 50;
            for (var i = 0; i < residentCount; i++)
            {
                var passengerEntityId = new EntityId(_nextEntityId++);
                var destinationFloor = ElevatorBank.MinFloor + 1 + (i % floorRange);
                ElevatorBank.EnqueuePassenger(
                    new ElevatorPassenger(passengerEntityId, ElevatorBank.MinFloor, destinationFloor));
            }
        }


        public static TowerSimulation CreateStandardFiveFloor(TowerEconomyState economy = null, IRandomStream randomStream = null, bool enqueueMorningRush = false)
        {
            var clock = new SimulationClock(new Tick(0));
            var topologyState = BuildingTopologyState.CreateWithFixture();
            var population = FiftyResidentFixture.Create();

            var car = new ElevatorCar(new EntityId(501), startingFloor: 0, capacity: 10);
            var elevatorBank = new ElevatorBank(minFloor: 0, maxFloor: 4, new[] { car });

            var sim = new TowerSimulation(
                clock,
                topologyState,
                population,
                elevatorBank,
                economy,
                randomStream);

            if (enqueueMorningRush)
            {
                // Generate real morning commute trips for residents from their apartments on Floors 1-4
                // down to their workplace (Diner on Floor 0) using the full transit system.
                EntityId? dinerId = population.Persons.Count > 0 ? population.Persons[0].WorkplaceRoomId : (EntityId?)null;
                var nextTripId = 1000;

                foreach (var person in population.Persons)
                {
                    var originRoomId = person.HomeRoomId;
                    var destinationRoomId = person.WorkplaceRoomId.IsValid
                        ? person.WorkplaceRoomId
                        : (dinerId ?? originRoomId);

                    if (!originRoomId.Equals(destinationRoomId))
                    {
                        var originNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(originRoomId);
                        var destNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(destinationRoomId);
                        TransitRoute route = null;
                        if (originNode != null && destNode != null)
                        {
                            route = sim.Planner.FindRoute(originNode.Id, destNode.Id);
                        }

                        if (route != null)
                        {
                            var trip = new TripRecord(
                                new EntityId(nextTripId++),
                                person.Id,
                                originRoomId,
                                destinationRoomId,
                                TripPurpose.Work,
                                clock.CurrentTick,
                                route);

                            sim.Transit.SubmitTrip(trip, topologyState, clock.CurrentTick, population);
                        }
                    }
                }
            }

            return sim;
        }

        public TowerSaveData ExportSaveData()
        {
            var data = new TowerSaveData
            {
                simulationTick = Clock.CurrentTick.Value,
                nextElevatorCarId = _nextElevatorCarId,
                nextEntityId = _nextEntityId
            };

            data.SetEconomySaveData(Economy.ToSaveData());
            data.SetTopologySaveData(Topology.ToSaveData());
            data.SetPopulationSaveData(Population.ToSaveData());
            data.elevatorBank = ElevatorBank.ToSaveData();
            data.activeTrips = Transit.ToSaveData();
            data.scrutiny = new ScrutinySaveData { value = Scrutiny.Value, previousValue = Scrutiny.PreviousValue, recentExpansionPressure = Scrutiny.RecentExpansionPressure, recentPolicyPressure = Scrutiny.RecentPolicyPressure };
            data.businesses = Businesses.ToSaveData();

            return data;
        }

        public static TowerSimulation RestoreFromSaveData(TowerSaveData data, IRandomStream randomStream = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var clock = new SimulationClock(new Tick(data.simulationTick));
            var topology = BuildingTopologyState.FromSaveData(data.GetTopologySaveData(), data.nextEntityId > 0 ? data.nextEntityId : 3000);
            var population = PopulationState.FromSaveData(data.GetPopulationSaveData(), randomStream);
            var elevatorBank = ElevatorBank.FromSaveData(data.elevatorBank);
            var economy = TowerEconomyState.FromSaveData(data.GetEconomySaveData());
            var scrutiny = data.scrutiny == null ? new ScrutinyState() : new ScrutinyState(data.scrutiny.value, data.scrutiny.previousValue, data.scrutiny.recentExpansionPressure, data.scrutiny.recentPolicyPressure);

            var sim = new TowerSimulation(clock, topology, population, elevatorBank, economy, randomStream, scrutiny);
            sim.Businesses = BusinessState.FromSaveData(data.businesses);
            sim._nextElevatorCarId = data.nextElevatorCarId > 0 ? data.nextElevatorCarId : 500;
            sim._nextEntityId = data.nextEntityId > 0 ? data.nextEntityId : 3000;

            sim.Transit.RestoreFromSaveData(data.activeTrips, sim.Topology, sim.Planner);
            sim.SyncTransitServices();
            return sim;
        }
    }
}
