using System;
using System.Collections.Generic;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Events;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
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
            IRandomStream randomStream = null)
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

        public long CurrentTick => Clock.CurrentTick.Value;

        public int ResidentCount => Population.ResidentCount;

        public int ActiveTripCount => Transit.ActiveTripCount;

        public int TotalQueuedElevatorPassengers => ElevatorBank.TotalQueuedCount;

        public float AverageElevatorWaitTicks => ElevatorBank.AverageWaitTicks;

        public void AdvanceOneTick()
        {
            var previousTick = Clock.CurrentTick;
            Clock.Advance();
            var currentTick = Clock.CurrentTick;

            // 1. Periodic autonomous leasing demand evaluation (every 10 ticks)
            if (currentTick.Value % 10 == 0)
            {
                Leasing.EvaluateLeasingDemand(Topology, Population, ElevatorBank, RandomStream, currentTick, ref _nextEntityId);
            }

            // 2. Periodic rental collection cycle (every 50 ticks)
            if (currentTick.Value % 50 == 0)
            {
                Economy.ProcessRentCycle(Topology, Population);
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

        public OneRoof.Domain.Commands.CommandResult BuildFloorSlab(OneRoof.Domain.Commands.BuildFloorSlabCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var cost = Economy.CalculateFloorSlabCost(cmd.Bounds);
            if (!Economy.CanAfford(cost))
            {
                return OneRoof.Domain.Commands.CommandResult.Reject(new[]
                {
                    new OneRoof.Domain.Commands.CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Cannot afford floor slab cost of {cost} (Treasury: {Economy.CashBalance}).")
                });
            }

            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (result.Accepted)
            {
                Economy.TryDeduct(cost);
                SyncTransitServices();
            }

            return result;
        }

        public OneRoof.Domain.Commands.CommandResult BuildRoom(OneRoof.Domain.Commands.BuildRoomCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var cost = Economy.CalculateRoomCost(cmd.ContentType, cmd.Bounds);
            if (!Economy.CanAfford(cost))
            {
                return OneRoof.Domain.Commands.CommandResult.Reject(new[]
                {
                    new OneRoof.Domain.Commands.CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Cannot afford room cost of {cost} (Treasury: {Economy.CashBalance}).")
                });
            }

            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (result.Accepted)
            {
                Economy.TryDeduct(cost);
                SyncTransitServices();
            }

            return result;
        }

        public OneRoof.Domain.Commands.CommandResult AddElevatorShaft(OneRoof.Domain.Commands.AddElevatorShaftCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var cost = Economy.CalculateElevatorShaftCost(cmd.FloorSpan, cmd.ShaftMaxX - cmd.ShaftMinX + 1);
            if (!Economy.CanAfford(cost))
            {
                return OneRoof.Domain.Commands.CommandResult.Reject(new[]
                {
                    new OneRoof.Domain.Commands.CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Cannot afford elevator shaft cost of {cost} (Treasury: {Economy.CashBalance}).")
                });
            }

            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (result.Accepted)
            {
                Economy.TryDeduct(cost);
                ElevatorBank.ExpandFloorRange(cmd.BottomFloor, cmd.TopFloor);
                SyncTransitServices();
            }

            return result;
        }

        public OneRoof.Domain.Commands.CommandResult BuildStairwell(OneRoof.Domain.Commands.BuildStairwellCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            var cost = Economy.CalculateStairwellCost(cmd.FloorSpan, cmd.StairMaxX - cmd.StairMinX + 1);
            if (!Economy.CanAfford(cost))
            {
                return OneRoof.Domain.Commands.CommandResult.Reject(new[]
                {
                    new OneRoof.Domain.Commands.CommandRejectionReason(new ContentId("economy:insufficient_funds"), $"Cannot afford stairwell cost of {cost} (Treasury: {Economy.CashBalance}).")
                });
            }

            var result = Topology.Execute(cmd, Clock.CurrentTick);
            if (result.Accepted)
            {
                Economy.TryDeduct(cost);
                SyncTransitServices();
            }

            return result;
        }

        public OneRoof.Domain.Commands.CommandResult DemolishRoom(OneRoof.Domain.Commands.DemolishRoomCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));

            if (Topology.Rooms.TryGetValue(cmd.RoomId, out var room))
            {
                var contentVal = room.ContentType.Value ?? "";
                if (contentVal.Contains("lobby"))
                {
                    return OneRoof.Domain.Commands.CommandResult.Reject(new[]
                    {
                        new OneRoof.Domain.Commands.CommandRejectionReason(new ContentId("demolish:protected"), "Cannot demolish main reception lobby.")
                    });
                }

                if (contentVal.Contains("elevator_shaft"))
                {
                    return OneRoof.Domain.Commands.CommandResult.Reject(new[]
                    {
                        new OneRoof.Domain.Commands.CommandRejectionReason(new ContentId("demolish:protected"), "Elevator shafts cannot be demolished with room bulldozer.")
                    });
                }

                if (!cmd.Force && contentVal.StartsWith("residential:"))
                {
                    var households = Population.Households;
                    for (var i = 0; i < households.Count; i++)
                    {
                        if (households[i].HomeRoomId.Equals(cmd.RoomId))
                        {
                            return OneRoof.Domain.Commands.CommandResult.Reject(new[]
                            {
                                new OneRoof.Domain.Commands.CommandRejectionReason(new ContentId("demolish:occupied"), "Cannot demolish occupied apartment with active tenants.")
                            });
                        }
                    }
                }

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

        public void AddElevatorCar(int capacity = 10, int startingFloor = 0)
        {
            var carId = new EntityId(_nextElevatorCarId++);
            var car = new ElevatorCar(carId, startingFloor, capacity);
            ElevatorBank.AddCar(car);
            Economy.TryDeduct(TowerEconomyState.ElevatorCarCost);
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
                cashBalance = Economy.CashBalance,
                sandboxMode = Economy.SandboxMode,
                totalRevenue = Economy.TotalRevenue,
                totalExpenses = Economy.TotalExpenses,
                nextElevatorCarId = _nextElevatorCarId,
                nextEntityId = _nextEntityId
            };

            // 1. Floor Slabs
            var slabList = new List<FloorSlabSaveData>(Topology.FloorSlabs.Count);
            foreach (var slab in Topology.FloorSlabs.Values)
            {
                slabList.Add(new FloorSlabSaveData
                {
                    floorLevel = slab.Floor,
                    minX = slab.MinX,
                    maxX = slab.MaxX
                });
            }
            data.floorSlabs = slabList.ToArray();

            // 2. Rooms
            var roomList = new List<RoomSaveData>(Topology.Rooms.Count);
            foreach (var room in Topology.Rooms.Values)
            {
                var pIds = new int[room.PortalIds.Count];
                for (var i = 0; i < room.PortalIds.Count; i++) pIds[i] = room.PortalIds[i].Value;

                roomList.Add(new RoomSaveData
                {
                    id = room.Id.Value,
                    contentType = room.ContentType.Value,
                    floor = room.Floor,
                    minX = room.Bounds.MinX,
                    maxX = room.Bounds.MaxX,
                    capacity = room.Capacity,
                    portalIds = pIds
                });
            }
            data.rooms = roomList.ToArray();

            // 3. Portals
            var portalList = new List<PortalSaveData>(Topology.Portals.Count);
            foreach (var portal in Topology.Portals.Values)
            {
                portalList.Add(new PortalSaveData
                {
                    id = portal.Id.Value,
                    portalType = (int)portal.Type,
                    floor = portal.Location.Floor,
                    x = portal.Location.X,
                    roomId = portal.RoomId.Value,
                    targetPortalId = portal.TargetPortalId?.Value ?? -1
                });
            }
            data.portals = portalList.ToArray();

            // 4. Households
            var householdList = new List<HouseholdSaveData>(Population.Households.Count);
            foreach (var h in Population.Households)
            {
                var mIds = new int[h.MemberIds.Count];
                for (var i = 0; i < h.MemberIds.Count; i++) mIds[i] = h.MemberIds[i].Value;

                householdList.Add(new HouseholdSaveData
                {
                    id = h.Id.Value,
                    homeRoomId = h.HomeRoomId.Value,
                    memberIds = mIds,
                    budget = h.Budget,
                    satisfaction = h.Satisfaction
                });
            }
            data.households = householdList.ToArray();

            // 5. Persons
            var personList = new List<PersonSaveData>(Population.Persons.Count);
            foreach (var p in Population.Persons)
            {
                float hunger = 1f, rest = 1f, social = 1f;
                foreach (var need in p.Needs)
                {
                    if (need.Kind == NeedKind.Hunger) hunger = need.Satisfaction;
                    else if (need.Kind == NeedKind.Rest) rest = need.Satisfaction;
                    else if (need.Kind == NeedKind.Social) social = need.Satisfaction;
                }

                personList.Add(new PersonSaveData
                {
                    id = p.Id.Value,
                    householdId = p.HouseholdId.Value,
                    homeRoomId = p.HomeRoomId.Value,
                    workplaceRoomId = p.WorkplaceRoomId.Value,
                    currentRoomId = p.CurrentRoomId.Value,
                    currentActivity = (int)p.CurrentActivity,
                    trait = p.Traits.Count > 0 ? (int)p.Traits[0].Kind : 0,
                    hungerSatisfaction = hunger,
                    restSatisfaction = rest,
                    socialSatisfaction = social
                });
            }
            data.persons = personList.ToArray();

            // 6. Elevator Bank & Cars
            var carSaveList = new List<ElevatorCarSaveData>(ElevatorBank.Cars.Count);
            foreach (var car in ElevatorBank.Cars)
            {
                var riderList = new List<ElevatorPassengerSaveData>(car.Passengers.Count);
                foreach (var rider in car.Passengers)
                {
                    riderList.Add(new ElevatorPassengerSaveData
                    {
                        personId = rider.PersonId.Value,
                        originFloor = rider.OriginFloor,
                        destinationFloor = rider.DestinationFloor,
                        waitTicks = rider.WaitTicks,
                        rideTicks = rider.RideTicks
                    });
                }

                carSaveList.Add(new ElevatorCarSaveData
                {
                    id = car.Id.Value,
                    currentFloor = car.CurrentFloor,
                    capacity = car.Capacity,
                    phase = (int)car.Phase,
                    direction = (int)car.Direction,
                    timerTicksRemaining = car.TimerTicksRemaining,
                    passengers = riderList.ToArray()
                });
            }

            var queuedPassengerList = new List<ElevatorPassengerSaveData>();
            foreach (var queue in ElevatorBank.FloorQueues.Values)
            {
                foreach (var p in queue)
                {
                    queuedPassengerList.Add(new ElevatorPassengerSaveData
                    {
                        personId = p.PersonId.Value,
                        originFloor = p.OriginFloor,
                        destinationFloor = p.DestinationFloor,
                        waitTicks = p.WaitTicks,
                        rideTicks = p.RideTicks
                    });
                }
            }

            var deliveredPassengerList = new List<ElevatorPassengerSaveData>();
            foreach (var p in ElevatorBank.DeliveredPassengers)
            {
                deliveredPassengerList.Add(new ElevatorPassengerSaveData
                {
                    personId = p.PersonId.Value,
                    originFloor = p.OriginFloor,
                    destinationFloor = p.DestinationFloor,
                    waitTicks = p.WaitTicks,
                    rideTicks = p.RideTicks
                });
            }

            data.elevatorBank = new ElevatorBankSaveData
            {
                minFloor = ElevatorBank.MinFloor,
                maxFloor = ElevatorBank.MaxFloor,
                cars = carSaveList.ToArray(),
                queuedPassengers = queuedPassengerList.ToArray(),
                deliveredPassengers = deliveredPassengerList.ToArray()
            };

            // 7. Active in-flight trips
            var tripList = new List<ActiveTripSaveData>(Transit.ActiveTrips.Count);
            foreach (var exec in Transit.ActiveTrips)
            {
                tripList.Add(new ActiveTripSaveData
                {
                    tripId = exec.Trip.Id.Value,
                    personId = exec.Trip.PersonId.Value,
                    originRoomId = exec.Trip.OriginRoomId.Value,
                    destinationRoomId = exec.Trip.DestinationRoomId.Value,
                    purpose = (int)exec.Trip.Purpose,
                    departureTick = exec.Trip.DepartureTick.Value,
                    state = (int)exec.Trip.State,
                    waitTicks = exec.Trip.WaitTicks,
                    currentLegIndex = exec.CurrentLegIndex,
                    legRemainingTicks = exec.LegRemainingTicks,
                    isQueuedInElevator = exec.IsQueuedInElevator,
                    isRidingElevator = exec.IsRidingElevator,
                    currentFloor = exec.CurrentFloor,
                    currentX = exec.CurrentX
                });
            }
            data.activeTrips = tripList.ToArray();

            return data;
        }

        public static TowerSimulation RestoreFromSaveData(TowerSaveData data, IRandomStream randomStream = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var clock = new SimulationClock(new Tick(data.simulationTick));

            // 1. Restore Topology
            var topology = new BuildingTopologyState(data.nextEntityId);
            var slabs = new List<CellBounds>();
            if (data.floorSlabs != null)
            {
                foreach (var s in data.floorSlabs)
                {
                    slabs.Add(new CellBounds(s.floorLevel, s.minX, s.maxX));
                }
            }

            var rooms = new List<Room>();
            if (data.rooms != null)
            {
                foreach (var r in data.rooms)
                {
                    var pIdList = new List<EntityId>();
                    if (r.portalIds != null)
                    {
                        foreach (var pid in r.portalIds) pIdList.Add(new EntityId(pid));
                    }
                    rooms.Add(new Room(new EntityId(r.id), new ContentId(r.contentType), new CellBounds(r.floor, r.minX, r.maxX), pIdList, r.capacity));
                }
            }

            var portals = new List<Portal>();
            if (data.portals != null)
            {
                foreach (var p in data.portals)
                {
                    EntityId? target = p.targetPortalId > 0 ? new EntityId(p.targetPortalId) : null;
                    portals.Add(new Portal(new EntityId(p.id), (PortalType)p.portalType, new CellCoordinate(p.x, p.floor), new EntityId(p.roomId), target));
                }
            }

            topology.RestoreFromData(slabs, rooms, portals);

            // 2. Restore Population
            var households = new List<HouseholdRecord>();
            if (data.households != null)
            {
                foreach (var h in data.households)
                {
                    var mList = new List<EntityId>();
                    if (h.memberIds != null)
                    {
                        foreach (var mid in h.memberIds) mList.Add(new EntityId(mid));
                    }
                    households.Add(new HouseholdRecord(new EntityId(h.id), mList, new EntityId(h.homeRoomId), h.budget, h.satisfaction));
                }
            }

            var persons = new List<PersonRecord>();
            if (data.persons != null)
            {
                var rng = randomStream ?? new DeterministicRandomStream(1337);
                foreach (var p in data.persons)
                {
                    var traitKind = Enum.IsDefined(typeof(PersonTraitKind), p.trait) ? (PersonTraitKind)p.trait : PersonTraitKind.EarlyBird;
                    var trait = new PersonTrait(traitKind);
                    var schedule = DailySchedule.Standard(trait, rng);
                    var needs = new[]
                    {
                        new NeedState(NeedKind.Hunger, p.hungerSatisfaction),
                        new NeedState(NeedKind.Rest, p.restSatisfaction),
                        new NeedState(NeedKind.Social, p.socialSatisfaction)
                    };
                    var traits = new[] { trait };
                    var person = new PersonRecord(
                        new EntityId(p.id),
                        new EntityId(p.householdId),
                        new EntityId(p.homeRoomId),
                        new EntityId(p.workplaceRoomId),
                        schedule,
                        needs,
                        traits);

                    person.UpdateLocation(new EntityId(p.currentRoomId > 0 ? p.currentRoomId : p.homeRoomId));
                    person.UpdateActivity((ActivityKind)p.currentActivity);
                    persons.Add(person);
                }
            }

            var population = new PopulationState(persons, households);

            // 3. Restore Elevator Bank
            var cars = new List<ElevatorCar>();
            if (data.elevatorBank?.cars != null)
            {
                foreach (var c in data.elevatorBank.cars)
                {
                    var car = new ElevatorCar(new EntityId(c.id), c.currentFloor, c.capacity);
                    var riders = new List<ElevatorPassenger>();
                    if (c.passengers != null)
                    {
                        foreach (var riderData in c.passengers)
                        {
                            riders.Add(new ElevatorPassenger(new EntityId(riderData.personId), riderData.originFloor, riderData.destinationFloor)
                            {
                                WaitTicks = riderData.waitTicks,
                                RideTicks = riderData.rideTicks
                            });
                        }
                    }

                    car.RestoreState(c.currentFloor, (ElevatorCarPhase)c.phase, (ElevatorDirection)c.direction, c.timerTicksRemaining, riders);
                    cars.Add(car);
                }
            }

            var minFloor = data.elevatorBank?.minFloor ?? 0;
            var maxFloor = data.elevatorBank?.maxFloor ?? 4;
            var elevatorBank = new ElevatorBank(minFloor, maxFloor, cars);

            if (data.elevatorBank?.queuedPassengers != null)
            {
                foreach (var qp in data.elevatorBank.queuedPassengers)
                {
                    elevatorBank.EnqueuePassenger(new ElevatorPassenger(new EntityId(qp.personId), qp.originFloor, qp.destinationFloor)
                    {
                        WaitTicks = qp.waitTicks,
                        RideTicks = qp.rideTicks
                    });
                }
            }

            if (data.elevatorBank?.deliveredPassengers != null)
            {
                var delivered = new List<ElevatorPassenger>(data.elevatorBank.deliveredPassengers.Length);
                foreach (var dp in data.elevatorBank.deliveredPassengers)
                {
                    delivered.Add(new ElevatorPassenger(new EntityId(dp.personId), dp.originFloor, dp.destinationFloor)
                    {
                        WaitTicks = dp.waitTicks,
                        RideTicks = dp.rideTicks
                    });
                }
                elevatorBank.RestoreDeliveredPassengers(delivered);
            }

            // 4. Restore Economy
            var economy = new TowerEconomyState(data.cashBalance, data.sandboxMode, data.totalRevenue, data.totalExpenses);

            // 5. Build Simulation
            var sim = new TowerSimulation(clock, topology, population, elevatorBank, economy, randomStream);
            sim._nextElevatorCarId = data.nextElevatorCarId > 0 ? data.nextElevatorCarId : 500;
            sim._nextEntityId = data.nextEntityId > 0 ? data.nextEntityId : 3000;

            // 6. Restore Active Trips
            if (data.activeTrips != null)
            {
                foreach (var t in data.activeTrips)
                {
                    var originNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(new EntityId(t.originRoomId));
                    var destNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(new EntityId(t.destinationRoomId));
                    TransitRoute route = null;
                    if (originNode != null && destNode != null && t.originRoomId != t.destinationRoomId)
                    {
                        route = sim.Planner.FindRoute(originNode.Id, destNode.Id);
                    }

                    var trip = new TripRecord(
                        new EntityId(t.tripId),
                        new EntityId(t.personId),
                        new EntityId(t.originRoomId),
                        new EntityId(t.destinationRoomId),
                        (TripPurpose)t.purpose,
                        new Tick(t.departureTick),
                        route);

                    if (t.state == (int)TripState.InProgress)
                    {
                        trip.Begin();
                    }

                    trip.AddWaitTicks(t.waitTicks);

                    sim.Transit.RestoreTripExecution(
                        trip,
                        t.currentLegIndex,
                        t.legRemainingTicks,
                        t.isQueuedInElevator,
                        t.isRidingElevator,
                        t.currentFloor,
                        t.currentX);
                }
            }

            sim.SyncTransitServices();
            return sim;
        }
    }
}
