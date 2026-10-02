"""Make an isolated source hypothesis, never modify the project's runtime source.
Usage: python3 TransitHypothesis.py /tmp/one-roof-c0-transit-hypothesis
"""
from pathlib import Path
import difflib
import shutil
import sys
root = Path(__file__).resolve().parents[2]
destination = Path(sys.argv[1]).resolve()
if root == destination or root in destination.parents:
    raise SystemExit('Hypothesis must be outside the repository')
source = root/'Assets/OneRoof/Runtime/Domain'
if destination.exists():
    raise SystemExit('Use a fresh destination to preserve earlier evidence')
shutil.copytree(source, destination, ignore=shutil.ignore_patterns('*.meta', '*.asmdef'))
p = destination/'Transit/TransitExecutionSystem.cs'
before = p.read_text()
after = before
# Query actual queued passenger without mutation; wait accounting remains added only at delivery/removal.
old = 'else if (trip.WaitTicks >= 15 || (floorDelta <= 2 && trip.WaitTicks >= 5))'
new = 'else if (LiveQueuedWait(elevatorBank, trip.PersonId) >= 15 || (floorDelta <= 2 && LiveQueuedWait(elevatorBank, trip.PersonId) >= 5))'
assert old in after
after = after.replace(old,new)
old = '''if (((floorDelta <= 2 && queueLength > 0) || queueLength >= 3) &&
                            TryRerouteWithoutElevators(execution, topology.TransitGraph))'''
new = '''if (((floorDelta <= 2 && queueLength > 0) || queueLength >= 3) &&
                            TryRerouteWithoutElevators(execution, topology.TransitGraph,
                                360 - (tick.Value - trip.DepartureTick.Value)))'''
assert old in after
after = after.replace(old,new)
old = """if (elevatorBank.TryRemoveQueuedPassenger(trip.PersonId, out var waiting))
                            {
                                trip.AddWaitTicks((int)waiting.WaitTicks);
                                if (TryRerouteWithoutElevators(execution, topology.TransitGraph))
                                {
                                    continue;
                                }

                                elevatorBank.EnqueuePassenger(waiting);
                            }"""
new = """// Decide route before removal: a rejected alternative cannot charge the same wait again.
                            if (TryRerouteWithoutElevators(execution, topology.TransitGraph,
                                360 - (tick.Value - trip.DepartureTick.Value)))
                            {
                                if (elevatorBank.TryRemoveQueuedPassenger(trip.PersonId, out var waiting))
                                    trip.AddWaitTicks((int)waiting.WaitTicks);
                                continue;
                            }"""
assert old in after
after = after.replace(old,new)
old = '''ActiveTripExecution execution,
            HierarchicalTransitGraph graph)
        {
            var activeRoute'''
new = '''ActiveTripExecution execution,
            HierarchicalTransitGraph graph, long remainingDeadlineTicks = long.MaxValue)
        {
            var activeRoute'''
assert old in after
after = after.replace(old,new)
old = 'if (route == null) return false;\n\n            execution.Route = route;'
new = 'if (route == null || route.TotalCost > remainingDeadlineTicks) return false;\n\n            execution.Route = route;'
assert old in after
after = after.replace(old,new)
anchor = '        private static bool TryRerouteWithoutElevators('
helper = '''        private static long LiveQueuedWait(ElevatorBank bank, EntityId resident)
        {
            foreach (var queue in bank.FloorQueues.Values)
                foreach (var passenger in queue)
                    if (passenger.PersonId.Equals(resident)) return passenger.WaitTicks;
            return 0;
        }

'''
# Check current passenger field name: source uses PersonId or ResidentId? Fail compilation if it drifts.
after = after.replace(anchor,helper+anchor)
p.write_text(after)
(destination/'hypothesis.patch').write_text(''.join(difflib.unified_diff(before.splitlines(True),after.splitlines(True),fromfile='current/TransitExecutionSystem.cs',tofile='hypothesis/TransitExecutionSystem.cs')))
print(destination)
