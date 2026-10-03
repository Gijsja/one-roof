using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

public static class TransitDiagnosticProbe
{
    public sealed class FailureRecord
    {
        public int Tier { get; set; }
        public string Reason { get; set; }
        public long Day { get; set; }
        public int ResidentId { get; set; }
        public TripPurpose Purpose { get; set; }
        public long SubmissionTick { get; set; }
        public long SubmissionTimeOfDay { get; set; }
        public long FailureTick { get; set; }
        public long Duration { get; set; }
        public int QueueWait { get; set; }
        public long TripId { get; set; }
        public int OriginFloor { get; set; }
        public int DestinationFloor { get; set; }
        public int FloorDelta { get; set; }
        public int HomeFloor { get; set; }
        public int WorkplaceFloor { get; set; }
        public string TripState { get; set; }
        public bool IsRerouted { get; set; }
        public bool UsedElevator { get; set; }
        public int LegCount { get; set; }
        public string StatusAtFailure { get; set; }
    }

    public static void Main(string[] args)
    {
        var targetTiers = args.Length > 0 ? args.Select(int.Parse).ToArray() : new[] { 1, 5 };
        var reportDir = "/tmp/c0-transit-diagnostic";
        Directory.CreateDirectory(reportDir);

        foreach (var tier in targetTiers)
        {
            Console.WriteLine($"\n=======================================================");
            Console.WriteLine($"RUNNING DIAGNOSTIC PROBE FOR TIER {tier}");
            Console.WriteLine($"=======================================================");

            var sim = GoldStandardCityFixture.Create();
            for (var next = 2; next <= tier; next++)
            {
                var command = new UpgradeElevatorTierCommand(next);
                if (!sim.CanExecute(command).Accepted || !sim.ExecuteCommand(command).Accepted)
                    throw new InvalidOperationException($"Existing upgrade command rejected tier {next}");
            }

            var failures = new List<FailureRecord>();
            var allTrips = new Dictionary<int, TripRecord>();
            var allExecutions = new Dictionary<int, TransitExecutionSystem.ActiveTripExecution>();

            int GetLocationFloor(WorldLocation loc)
            {
                if (loc.IsOutside) return 0;
                return sim.Topology.Rooms.TryGetValue(loc.RoomId, out var r) ? r.Floor : 0;
            }

            int GetRoomFloor(EntityId roomId)
            {
                if (roomId.Value == 0) return 0;
                return sim.Topology.Rooms.TryGetValue(roomId, out var r) ? r.Floor : 0;
            }

            PersonRecord GetPerson(int residentId)
            {
                return sim.Population.Persons.FirstOrDefault(p => p.Id.Value == residentId);
            }

            TransitDemandObserver.DiagnosticFailure = (reason, episode, failureTick, success) =>
            {
                var person = GetPerson(episode.residentId);
                var homeFloor = person != null ? GetRoomFloor(person.HomeRoomId) : -1;
                var workFloor = person != null ? GetLocationFloor(person.WorkplaceLocation) : -1;

                TripRecord trip = null;
                TransitExecutionSystem.ActiveTripExecution exec = null;
                if (episode.tripId > 0 && allTrips.TryGetValue((int)episode.tripId, out var tr))
                {
                    trip = tr;
                    allExecutions.TryGetValue((int)episode.tripId, out exec);
                }

                int originFloor = -1, destFloor = -1;
                if (trip != null)
                {
                    originFloor = GetLocationFloor(trip.Origin);
                    destFloor = GetLocationFloor(trip.Destination);
                }
                else if (person != null)
                {
                    originFloor = GetLocationFloor(person.CurrentLocation);
                    destFloor = episode.purpose switch
                    {
                        TripPurpose.Home => homeFloor,
                        TripPurpose.Work => workFloor,
                        _ => 0
                    };
                }

                bool usedElevator = false;
                bool isRerouted = false;
                int legCount = 0;
                if (exec?.Route != null)
                {
                    legCount = exec.Route.Legs.Count;
                    usedElevator = exec.Route.Legs.Any(l => l.Mode == TransitMode.Elevator);
                    isRerouted = exec.Route != trip.PlannedRoute;
                }

                string statusAtFailure = "Unknown";
                if (trip == null)
                {
                    statusAtFailure = "NeverStartedTrip";
                }
                else if (exec != null)
                {
                    if (exec.IsQueuedInElevator)
                    {
                        statusAtFailure = $"QueuedInElevator(Floor={exec.CurrentFloor})";
                    }
                    else if (exec.IsRidingElevator)
                    {
                        statusAtFailure = $"RidingElevator(Floor={exec.CurrentFloor})";
                    }
                    else
                    {
                        statusAtFailure = $"Walking(Floor={exec.CurrentFloor},Leg={exec.CurrentLegIndex}/{legCount})";
                    }
                }
                else
                {
                    statusAtFailure = trip.State.ToString();
                }

                failures.Add(new FailureRecord
                {
                    Tier = tier,
                    Reason = reason,
                    Day = episode.day,
                    ResidentId = episode.residentId,
                    Purpose = episode.purpose,
                    SubmissionTick = episode.submissionTick,
                    SubmissionTimeOfDay = episode.submissionTick % 1440,
                    FailureTick = failureTick,
                    Duration = failureTick - episode.submissionTick,
                    QueueWait = episode.queueWait,
                    TripId = episode.tripId,
                    OriginFloor = originFloor,
                    DestinationFloor = destFloor,
                    FloorDelta = Math.Abs(originFloor - destFloor),
                    HomeFloor = homeFloor,
                    WorkplaceFloor = workFloor,
                    TripState = trip?.State.ToString() ?? "NoTrip",
                    IsRerouted = isRerouted,
                    UsedElevator = usedElevator,
                    LegCount = legCount,
                    StatusAtFailure = statusAtFailure
                });
            };

            var floorMaxQueues = new int[sim.Topology.FloorCount];
            var floorDay5MaxQueues = new int[sim.Topology.FloorCount];
            var peakQueueEvents = new List<(long tick, int floor, int queue, long maxWait)>();

            // Run for 5 full days (0..7200) + 1800 ticks (up to 9000 to fully settle Day 5)
            for (var tick = 0; tick < 1440 * 5 + 1800; tick++)
            {
                sim.AdvanceOneTick();

                foreach (var execution in sim.Transit.ActiveTrips)
                {
                    var id = execution.Trip.Id.Value;
                    allTrips.TryAdd(id, execution.Trip);
                    allExecutions[id] = execution;
                }

                var currentTick = sim.CurrentTick;
                bool isDay5 = currentTick >= 7200 && currentTick < 8640;
                foreach (var kvp in sim.ElevatorBank.FloorQueues)
                {
                    var floor = kvp.Key;
                    var qCount = kvp.Value.Count;
                    if (qCount > floorMaxQueues[floor]) floorMaxQueues[floor] = qCount;
                    if (isDay5 && qCount > floorDay5MaxQueues[floor]) floorDay5MaxQueues[floor] = qCount;

                    if (qCount >= 10 && currentTick % 10 == 0)
                    {
                        var (maxWait, _) = sim.ElevatorBank.GetFloorWaitMetrics(floor);
                        if (maxWait > 60)
                        {
                            peakQueueEvents.Add((currentTick, floor, qCount, maxWait));
                        }
                    }
                }
            }

            var lastDay = sim.LastTransitDemandDay;
            Console.WriteLine($"Day 5 Summary: Attempted={lastDay?.attempted}, Succeeded={lastDay?.succeeded}, Failed={lastDay?.failed}, Status={lastDay?.Status}, P95Wait={lastDay?.P95Wait}, LiveMax={lastDay?.liveMaximum}");

            var day5Failures = failures.Where(f => f.Day == 5).ToList();
            Console.WriteLine($"Total Day 5 Failures captured by diagnostic: {day5Failures.Count}");

            Console.WriteLine("\n--- Failures by Reason ---");
            foreach (var g in day5Failures.GroupBy(f => f.Reason))
            {
                Console.WriteLine($"  {g.Key}: {g.Count()}");
            }

            Console.WriteLine("\n--- Failures by StatusAtFailure ---");
            foreach (var g in day5Failures.GroupBy(f => f.StatusAtFailure))
            {
                Console.WriteLine($"  {g.Key}: {g.Count()}");
            }

            Console.WriteLine("\n--- Failures by Purpose ---");
            foreach (var g in day5Failures.GroupBy(f => f.Purpose))
            {
                Console.WriteLine($"  {g.Key}: {g.Count()}");
            }

            Console.WriteLine("\n--- Failures by (OriginFloor -> DestinationFloor) Top 15 ---");
            foreach (var g in day5Failures.GroupBy(f => $"{f.OriginFloor} -> {f.DestinationFloor}").OrderByDescending(g => g.Count()).Take(15))
            {
                Console.WriteLine($"  F{g.Key}: {g.Count()} episodes");
            }

            Console.WriteLine("\n--- Failures by Time of Day (Hour / 60-tick window) ---");
            foreach (var g in day5Failures.GroupBy(f => (f.SubmissionTimeOfDay / 60) * 60).OrderBy(g => g.Key))
            {
                Console.WriteLine($"  Ticks {g.Key:D4}..{g.Key + 59:D4} (Hour {g.Key / 60:D2}): {g.Count()} failures");
            }

            Console.WriteLine("\n--- Day 5 ElevatorBank Max Queues per Floor ---");
            for (var f = 0; f < floorDay5MaxQueues.Length; f++)
            {
                if (floorDay5MaxQueues[f] > 0)
                {
                    Console.WriteLine($"  Floor {f,2}: Day 5 Max Queue = {floorDay5MaxQueues[f],3} (All-time Max: {floorMaxQueues[f],3})");
                }
            }

            Console.WriteLine("\n--- Sample Day 5 Failure Details (first 36) ---");
            foreach (var f in day5Failures.Take(36))
            {
                Console.WriteLine($"  Res={f.ResidentId} Purpose={f.Purpose,-7} SubTick={f.SubmissionTick} (TOD={f.SubmissionTimeOfDay,4}) Dur={f.Duration,3} QWait={f.QueueWait,3} F{f.OriginFloor}->F{f.DestinationFloor} Status={f.StatusAtFailure} Rerouted={f.IsRerouted} UsedElev={f.UsedElevator} Reason={f.Reason}");
            }

            var jsonPath = Path.Combine(reportDir, $"tier{tier}-day5-failures.json");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(day5Failures, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"\nFull failure details written to {jsonPath}");
        }
    }
}
