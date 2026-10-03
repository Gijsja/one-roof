// Read-only production Domain probe. Standalone .NET compilation, not Unity or new-chain acceptance.
// Output goes to the explicit directory argument; no scenes, saves or settings are mutated.
using System.Text.Json;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Trips;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Economy;

var rawOutput = args.Length > 0 ? Path.GetFullPath(args[0]) : "/tmp/one-roof-crisis-design-probe";
var isJsonFile = rawOutput.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
var output = isJsonFile ? Path.GetDirectoryName(rawOutput)! : rawOutput;
var summaryPath = isJsonFile ? rawOutput : Path.Combine(output, "summary.json");
Directory.CreateDirectory(output);
var json = new JsonSerializerOptions { IncludeFields = true };
// Canonicalize only proven non-authoritative fields/order; all causal values stay in the comparison.
string CanonicalSave(TowerSaveData data)
{
    foreach (var person in data.persons)
        if (person.currentLocationKind == (int)WorldLocationKind.Outside) person.currentRoomId = 0;
    if (data.factions?.supports != null)
        data.factions.supports = data.factions.supports.OrderBy(n => n.residentId)
            .ThenBy(n => n.factionId, StringComparer.Ordinal).ToArray();
    return JsonSerializer.Serialize(data, json);
}
var summary = new List<object>();
foreach (var tier in new[] { 1, 5 })
{
    var sim = GoldStandardCityFixture.Create();
    for (var next = 2; next <= tier; next++)
    {
        var command = new UpgradeElevatorTierCommand(next);
        if (!sim.CanExecute(command).Accepted || !sim.ExecuteCommand(command).Accepted)
            throw new InvalidOperationException($"Existing upgrade command rejected tier {next}");
    }
    var observed = new Dictionary<int, TripRecord>();
    var completed = new List<TripRecord>();
    var maxQueue = 0;
    long maxLandingWait = 0;
    var powerFailedFloorTicks = 0;
    var waterFailedFloorTicks = 0;
    var satisfactionSum = 0d;
    var minResidents = sim.ResidentCount;
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var daily = new List<object>();
    for (var step = 0; step < 1440 * 5; step++)
    {
        sim.AdvanceOneTick();
        if (sim.CaretakerOwnsInput || sim.WordBattles.OwnsInput)
            throw new InvalidOperationException($"Modal stopped production probe at {sim.CurrentTick}");
        foreach (var execution in sim.Transit.ActiveTrips)
            observed.TryAdd(execution.Trip.Id.Value, execution.Trip);
        // A retained production TripRecord exposes its final state even after execution removes it.
        foreach (var item in observed.Where(p => p.Value.State is TripState.Completed or TripState.Cancelled).ToArray())
        {
            completed.Add(item.Value);
            observed.Remove(item.Key);
        }
        maxQueue = Math.Max(maxQueue, sim.TotalQueuedElevatorPassengers);
        for (var floor = 0; floor < sim.Topology.FloorCount; floor++)
            maxLandingWait = Math.Max(maxLandingWait, sim.ElevatorBank.GetFloorWaitMetrics(floor).MaxWaitTicks);
        powerFailedFloorTicks += sim.ElectricalGridSnapshot().Floors.Count(f => f.IsBrownout);
        waterFailedFloorTicks += sim.WaterWasteNetworkSnapshot().Floors.Count(f => !f.HasWaterService);
        satisfactionSum += sim.Population.Persons.Average(p => p.Wellbeing.Satisfaction);
        minResidents = Math.Min(minResidents, sim.ResidentCount);
        if (sim.CurrentTick % 1440 == 0)
            daily.Add(new { tick = sim.CurrentTick, residents = sim.ResidentCount, treasury = sim.Economy.CashBalance,
                satisfaction = sim.Population.Persons.Average(p => p.Wellbeing.Satisfaction),
                strain = sim.Population.Persons.Average(p => p.Wellbeing.Strain), scrutiny = sim.Scrutiny.Value,
                queued = sim.TotalQueuedElevatorPassengers, bankCumulativeMeanWait = sim.AverageElevatorWaitTicks });
    }
    timer.Stop();
    var success = completed.Where(t => t.State == TripState.Completed).ToArray();
    var waitSamples = success.Where(t => t.WaitTicks > 0).Select(t => t.WaitTicks).OrderBy(x => x).ToArray();
    int? p95Wait = waitSamples.Length == 0 ? null : waitSamples[(int)Math.Ceiling(waitSamples.Length * .95) - 1];
    var snapshot = sim.ExportSaveData();
    var before = JsonSerializer.Serialize(snapshot, json);
    var restored = TowerSimulation.RestoreFromSaveData(snapshot);
    var after = JsonSerializer.Serialize(restored.ExportSaveData(), json);
    File.WriteAllText(Path.Combine(output, $"tier{tier}-before.json"), before);
    File.WriteAllText(Path.Combine(output, $"tier{tier}-after.json"), after);
    var semanticSnapshotEqual = CanonicalSave(snapshot) == CanonicalSave(restored.ExportSaveData());
    int? divergence = null;
    for (var step = 1; step <= 1500; step++)
    {
        sim.AdvanceOneTick();
        restored.AdvanceOneTick();
        var a = CanonicalSave(sim.ExportSaveData());
        var b = CanonicalSave(restored.ExportSaveData());
        if (a != b)
        {
            divergence = step;
            File.WriteAllText(Path.Combine(output, $"tier{tier}-future-original.json"), a);
            File.WriteAllText(Path.Combine(output, $"tier{tier}-future-restored.json"), b);
            break;
        }
    }
    summary.Add(new { tier, initialTick = 450, observedTicks = 1440 * 5, finalTickBeforeReplay = snapshot.simulationTick,
        minResidents, maxQueue, maxLandingWait, powerFailedFloorTicks, waterFailedFloorTicks,
        meanSatisfaction = satisfactionSum / (1440 * 5), observedTerminalTrips = completed.Count,
        observedSuccessfulTrips = success.Length,
        observedSuccessfulWithin360 = success.Count(t => t.CompletionTick.HasValue && t.CompletionTick.Value.Value - t.DepartureTick.Value <= 360),
        observedPositiveWaitSamples = waitSamples.Length, observedP95Wait = p95Wait,
        observedUnfinishedTrips = observed.Count, observedUnfinishedOver360 = observed.Values.Count(t => snapshot.simulationTick - t.DepartureTick.Value > 360), snapshotEqual = before == after, semanticSnapshotEqual, futureComparedTicks = 1500, completeDemandDay = sim.LastTransitDemandDay, firstFutureDivergenceStep = divergence,
        diagnosticElapsedSeconds = timer.Elapsed.TotalSeconds, daily });
}
// Actual validated construction commands: no RestoreFromData shortcut for physical layout.
var physical = new TowerSimulation(new SimulationClock(new Tick(0)), new BuildingTopologyState(),
    new PopulationState(Array.Empty<PersonRecord>(), Array.Empty<HouseholdRecord>()),
    new ElevatorBank(0, 2, Array.Empty<ElevatorCar>()), new TowerEconomyState(50000));
var commandReceipts = new List<object>();
void Build(ICommand command)
{
    var preview = physical.CanExecute(command);
    var actual = physical.ExecuteCommand(command);
    commandReceipts.Add(new { command = command.ToString(), preview = preview.Accepted, actual = actual.Accepted });
    if (!preview.Accepted || !actual.Accepted) throw new InvalidOperationException("Physical fixture command rejected: " + command);
}
for (var floor = 0; floor < 3; floor++) Build(new BuildFloorSlabCommand(floor, -20, 17));
for (var floor = 0; floor < 3; floor++)
{
    Build(new BuildRoomCommand(floor, -20, -19, WaterWasteNetworkState.WaterRiserContentId, 0));
    Build(new BuildRoomCommand(floor, -18, -17, WaterWasteNetworkState.WasteChuteContentId, 0));
    Build(new BuildRoomCommand(floor, 2, 7, new ContentId("residential:studio"), 1));
}
Build(new BuildRoomCommand(0, 10, 13, WaterWasteNetworkState.WaterPumpContentId, 120));
Build(new BuildRoomCommand(0, 14, 17, WaterWasteNetworkState.WasteCollectionContentId, 120));
physical.UtilityOperations.Snapshot(physical.Topology);
var sourceRoom = physical.Topology.Rooms.Values.Single(r => r.Floor == 1 && r.ContentType == WaterWasteNetworkState.WaterRiserContentId);
var wasteRoom = physical.Topology.Rooms.Values.Single(r => r.Floor == 1 && r.ContentType == WaterWasteNetworkState.WasteChuteContentId);
var networkCases = new List<object>();
foreach (var condition in new[] { .2f, .6f, 1f })
{
    var save = physical.UtilityOperations.ToSaveData();
    // Declared condition fixture only; this is not an autonomous incident or new-quality implementation.
    foreach (var equipment in save.equipment)
    {
        if (equipment.roomId == sourceRoom.Id.Value) equipment.condition = condition;
        else if (physical.Topology.Rooms[new EntityId(equipment.roomId)].ContentType == WaterWasteNetworkState.WasteCollectionContentId) equipment.condition = .2f;
        else equipment.condition = 1f;
    }
    var operations = UtilityOperationsState.FromSaveData(save);
    var network = physical.WaterWasteNetwork.Evaluate(physical.Topology, operations.Snapshot(physical.Topology));
    networkCases.Add(new { sourceCondition = condition, sourceRoom = sourceRoom.Id.Value, adjacentWasteRoom = wasteRoom.Id.Value,
        sourceFloorAvailable = network.Floors.Single(f => f.Floor == 1).HasWaterService,
        sourcePressure = network.Floors.Single(f => f.Floor == 1).WaterPressure,
        wasteAvailable = network.Floors.Single(f => f.Floor == 1).HasWasteCollection });
}
File.WriteAllText(summaryPath, JsonSerializer.Serialize(new {
    evidence = args.Length > 1 ? args[1] : "Actual current Domain source, standalone net10; not Unity, City daily sampling, new-chain acceptance or performance certification",
    limitations = "Captured-trip diagnostics remain positive-wait polling samples; completeDemandDay uses integrated pre-planning observation including local and rejected demand. Starts at tick450: first day is partial/unavailable. Semantic replay ignores only faction-support ordering and stale room IDs while Outside. Standalone timing is not native certification.",
    scenarios = summary, physicalFixture = new { commandReceipts, networkCases }
}, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));

Console.WriteLine($"\n[CrisisDesignProbe] Complete!");
Console.WriteLine($"[CrisisDesignProbe] Summary output: {summaryPath}");
Console.WriteLine($"[CrisisDesignProbe] Auxiliary files directory: {output}");
foreach (var s in summary)
{
    var doc = JsonDocument.Parse(JsonSerializer.Serialize(s, json));
    var root = doc.RootElement;
    var tierVal = root.GetProperty("tier").GetInt32();
    var cDay = root.GetProperty("completeDemandDay");
    int status = cDay.TryGetProperty("Status", out var pStat) ? pStat.GetInt32() : 0;
    long att = cDay.TryGetProperty("attempted", out var pAtt) ? pAtt.GetInt64() : 0;
    long succ = cDay.TryGetProperty("succeeded", out var pSucc) ? pSucc.GetInt64() : 0;
    long fail = cDay.TryGetProperty("failed", out var pFail) ? pFail.GetInt64() : 0;
    long unreach = cDay.TryGetProperty("unreachable", out var pUnreach) ? pUnreach.GetInt64() : 0;
    int p95 = cDay.TryGetProperty("P95Wait", out var pP95) ? pP95.GetInt32() : 0;
    int liveMax = cDay.TryGetProperty("liveMaximum", out var pLiveMax) ? pLiveMax.GetInt32() : 0;
    var divergence = root.GetProperty("firstFutureDivergenceStep").ValueKind == JsonValueKind.Null ? "None" : root.GetProperty("firstFutureDivergenceStep").ToString();
    Console.WriteLine($"   Tier {tierVal}: Status={status} Attempted={att} Succeeded={succ} Failed={fail} Unreach={unreach} P95={p95} LiveMax={liveMax} Divergence={divergence}");
}
