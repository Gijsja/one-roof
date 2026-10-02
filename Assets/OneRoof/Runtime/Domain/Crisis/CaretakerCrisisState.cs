using System;
using System.Collections.Generic;
using OneRoof.Domain.CivilAction;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Population;

namespace OneRoof.Domain.Crisis
{
    public enum CaretakerCrisisPhase
    {
        Dormant,
        Warning,
        Ready,
        Active,
        Failed,
        Resolved
    }

    public enum CaretakerAttack
    {
        None,
        Punch,
        Kick,
        JumpKick
    }

    public enum CaretakerOpponentRole
    {
        Brawler,
        Thrower,
        FloorBoss
    }

    public readonly struct CaretakerCombatInput
    {
        public CaretakerCombatInput(int move, bool punch = false, bool kick = false, bool duck = false, bool jump = false)
        {
            Move = Math.Max(-1, Math.Min(1, move));
            Punch = punch;
            Kick = kick;
            Duck = duck;
            Jump = jump;
        }

        public int Move { get; }
        public bool Punch { get; }
        public bool Kick { get; }
        public bool Duck { get; }
        public bool Jump { get; }
    }

    public sealed class CaretakerRouteFloor
    {
        internal CaretakerRouteFloor(int floor, int min, int max, int entry, int exit, bool encounter)
        {
            Floor = floor;
            MinX = min;
            MaxX = max;
            EntryX = entry;
            ExitX = exit;
            Encounter = encounter;
        }

        public int Floor { get; }
        public int MinX { get; }
        public int MaxX { get; }
        public int EntryX { get; }
        public int ExitX { get; }
        public bool Encounter { get; }
    }

    public sealed class CaretakerOpponent
    {
        public int Id { get; internal set; }
        public int ResidentId { get; internal set; }
        public CaretakerOpponentRole Role { get; internal set; }
        public int X { get; internal set; }
        public int Health { get; internal set; }
        public int Facing { get; internal set; }
        public int Timer { get; internal set; }
        public int AttackFrame { get; internal set; }
        public int HitStop { get; internal set; }

        internal int HitAttack;
        internal int PendingKnockback;
    }

    public sealed class CaretakerEvictionRecord
    {
        public int ResidentId { get; internal set; }
        public int Floor { get; internal set; }
        public string FactionId { get; internal set; }
        public string Cause { get; internal set; }
    }

    /// <summary>Deterministic 60 Hz arcade authority. Resident participation comes from connected organized evidence, never membership alone.</summary>
    public sealed class CaretakerCrisisState
    {
        public const int UnitsPerCell = 1000, StepsPerSecond = 60;
        public const int InputBufferTicks = 6, HitStopTicks = 3;
        private readonly List<CaretakerRouteFloor> _route = new List<CaretakerRouteFloor>();
        private readonly List<CaretakerOpponent> _opponents = new List<CaretakerOpponent>();
        private readonly List<CaretakerEvictionRecord> _evictions = new List<CaretakerEvictionRecord>();
        private int[] _residentIds = Array.Empty<int>(), _floors = Array.Empty<int>();
        private int _attackSerial, _attackFrame, _jumpFrames;
        private int _punchBuffer, _kickBuffer, _jumpBuffer, _hitStop;
        private bool _attackDucking;
        private readonly List<int> _floorParticipants = new List<int>();
        private int _nextParticipant;
        private readonly SortedSet<int> _encounterResidents = new SortedSet<int>();
        private readonly Dictionary<int, int> _residentFloors = new Dictionary<int, int>();
        private CaretakerCrisisSaveData _beforePractice;
        public bool Practice { get; private set; }
        public CaretakerCrisisPhase Phase { get; private set; }
        public string FactionId { get; private set; } = "";
        public string Cause { get; private set; } = "none";
        public int WarningDays { get; private set; }
        public int ReliefDays { get; private set; }
        public long LastEvaluationTick { get; private set; } = -1;
        public IReadOnlyList<int> AffectedResidentIds => _residentIds;
        public IReadOnlyList<int> AffectedFloors => _floors;
        public IReadOnlyList<CaretakerRouteFloor> Route => _route;
        public IReadOnlyList<CaretakerOpponent> Opponents => _opponents;
        public IReadOnlyList<CaretakerEvictionRecord> Evictions => _evictions;
        public bool ConsequencesApplied { get; private set; }
        public int RouteIndex { get; private set; }
        public int CurrentFloor => _route.Count == 0 ? 0 : _route[RouteIndex].Floor;
        public int X { get; private set; }
        public int Facing { get; private set; } = 1;
        public int Health { get; private set; } = 100;
        public CaretakerAttack Attack { get; private set; }
        public int AttackFrame => _attackFrame;
        public int JumpFrames => _jumpFrames;
        public int HitStop => _hitStop;
        public int PunchBuffer => _punchBuffer;
        public int KickBuffer => _kickBuffer;
        public int JumpBuffer => _jumpBuffer;
        public bool Ducking { get; private set; }
        public bool Paused { get; private set; }
        public int Clock { get; private set; }
        public int Score { get; private set; }

        public bool FloorCleared
        {
            get
            {
                if (_nextParticipant < _floorParticipants.Count)
                    return false;
                foreach (var o in _opponents)
                    if (o.Health > 0 || o.HitStop > 0)
                        return false;
                return true;
            }
        }

        public void Evaluate(long tick, IReadOnlyList<CivilActionSignal> signals, PopulationState population = null, BuildingTopologyState topology = null)
        {
            if (Practice)
                return;
            if (signals == null)
                throw new ArgumentNullException(nameof(signals));
            if (tick <= LastEvaluationTick)
                return;
            LastEvaluationTick = tick;
            if (Phase == CaretakerCrisisPhase.Active || Phase == CaretakerCrisisPhase.Failed || Phase == CaretakerCrisisPhase.Resolved)
                return;
            CivilActionSignal best = default;
            foreach (var signal in signals)
                if (signal.MemberCount >= 2 && signal.ResidentIds.Length >= 2 && signal.Floors.Length > 0 && signal.Pressure >= .55f && (best.FactionId == null || signal.Pressure > best.Pressure || signal.Pressure == best.Pressure && string.CompareOrdinal(signal.FactionId, best.FactionId) < 0))
                    best = signal;
            if (best.FactionId == null)
            {
                ReliefDays++;
                if (ReliefDays >= 2)
                {
                    Phase = CaretakerCrisisPhase.Dormant;
                    WarningDays = 0;
                    _residentIds = Array.Empty<int>();
                    _floors = Array.Empty<int>();
                }

                return;
            }

            ReliefDays = 0;
            if (FactionId != best.FactionId || Cause != best.Grievance)
                WarningDays = 0;
            FactionId = best.FactionId;
            Cause = best.Grievance;
            _residentIds = SortedUnique(best.ResidentIds);
            _floors = SortedUnique(best.Floors);
            if (population != null && topology != null)
            {
                _residentFloors.Clear();
                foreach (var person in population.Persons)
                    if (Array.IndexOf(_residentIds, person.Id.Value) >= 0 && topology.TryGetRoom(person.HomeRoomId, out var home))
                        _residentFloors[person.Id.Value] = home.Floor;
            }

            Phase = ++WarningDays >= 3 ? CaretakerCrisisPhase.Ready : CaretakerCrisisPhase.Warning;
        }

        private static int[] SortedUnique(int[] input)
        {
            var set = new SortedSet<int>(input);
            var result = new int[set.Count];
            set.CopyTo(result);
            return result;
        }

        public bool CanStart(BuildingTopologyState topology, out string reason)
        {
            if (Phase != CaretakerCrisisPhase.Ready && Phase != CaretakerCrisisPhase.Failed)
            {
                reason = "Sustained faction escalation has not reached an encounter.";
                return false;
            }

            return TryBuildRoute(topology, out _, out reason);
        }

        private bool TryBuildRoute(BuildingTopologyState topology, out List<CaretakerRouteFloor> route, out string reason)
        {
            route = new List<CaretakerRouteFloor>();
            reason = "";
            if (topology == null || !topology.HasFloor(0) || _floors.Length == 0)
            {
                reason = "A lobby and affected floors are required.";
                return false;
            }

            var current = 0;
            var entry = LobbyX(topology);
            foreach (var target in _floors)
            {
                if (!topology.HasFloor(target))
                {
                    reason = "Affected floor " + target + " no longer exists.";
                    return false;
                }

                var queue = new Queue<int>();
                var parent = new Dictionary<int, int>();
                var stair = new Dictionary<int, int>();
                queue.Enqueue(current);
                parent[current] = current;
                while (queue.Count > 0 && !parent.ContainsKey(target))
                {
                    var floor = queue.Dequeue();
                    foreach (var next in new[]
                    {
                        floor - 1,
                        floor + 1
                    }

                    )
                    {
                        if (parent.ContainsKey(next) || !topology.HasFloor(next))
                            continue;
                        var found = false;
                        var x = int.MaxValue;
                        foreach (var a in topology.GetPortalsOnFloor(floor))
                            if (IsValidStair(topology, a))
                                foreach (var b in topology.GetPortalsOnFloor(next))
                                    if (IsValidStair(topology, b) && a.Location.X == b.Location.X && topology.TryGetRoom(a.RoomId, out var firstStair) && topology.TryGetRoom(b.RoomId, out var secondStair) && firstStair.Bounds.MinX == secondStair.Bounds.MinX && firstStair.Bounds.MaxX == secondStair.Bounds.MaxX)
                                    {
                                        found = true;
                                        x = Math.Min(x, a.Location.X);
                                    }

                        if (found)
                        {
                            parent[next] = floor;
                            stair[next] = x * UnitsPerCell;
                            queue.Enqueue(next);
                        }
                    }
                }

                if (!parent.ContainsKey(target))
                {
                    reason = "Build connected stairs from lobby to affected floor " + target + ".";
                    return false;
                }

                var path = new List<int>();
                var cursor = target;
                while (cursor != current)
                {
                    path.Add(cursor);
                    cursor = parent[cursor];
                }

                path.Reverse();
                foreach (var next in path)
                {
                    topology.TryGetFloorSlab(current, out var slab);
                    route.Add(new CaretakerRouteFloor(current, slab.MinX * UnitsPerCell, (slab.MaxX + 1) * UnitsPerCell, entry, stair[next], Array.IndexOf(_floors, current) >= 0));
                    entry = stair[next];
                    current = next;
                }
            }

            topology.TryGetFloorSlab(current, out var final);
            route.Add(new CaretakerRouteFloor(current, final.MinX * UnitsPerCell, (final.MaxX + 1) * UnitsPerCell, entry, entry, true));
            foreach (var item in route)
                if (item.MaxX - item.MinX < 2000)
                {
                    reason = "Floor " + item.Floor + " needs two cells of corridor space.";
                    return false;
                }

            return true;
        }

        private static bool IsValidStair(BuildingTopologyState topology, Portal portal)
        {
            return portal.Type == PortalType.StairwellDoor && topology.TryGetFloorSlab(portal.Location.Floor, out var slab) && slab.Contains(portal.Location) && topology.TryGetRoom(portal.RoomId, out var room) && room.ContentType.Value == "amenity:stairwell" && room.Bounds.Contains(portal.Location) && room.Bounds.MinX >= slab.MinX && room.Bounds.MaxX <= slab.MaxX;
        }

        public static int LobbyX(BuildingTopologyState topology)
        {
            if (topology != null)
                foreach (var room in topology.GetRoomsOnFloor(0))
                    if (room.ContentType.Value == "room:lobby" || room.ContentType.Value == "service:lobby")
                        return (room.Bounds.MinX + room.Bounds.MaxX) * UnitsPerCell / 2;
            if (topology != null && topology.TryGetFloorSlab(0, out var slab))
                return (slab.MinX + slab.MaxX + 1) * UnitsPerCell / 2;
            return 0;
        }

        public bool Start(BuildingTopologyState topology)
        {
            if (!CanStart(topology, out _) || !TryBuildRoute(topology, out var route, out _))
                return false;
            _route.Clear();
            _route.AddRange(route);
            RouteIndex = 0;
            Score = 0;
            _evictions.Clear();
            _encounterResidents.Clear();
            ConsequencesApplied = false;
            Phase = CaretakerCrisisPhase.Active;
            Paused = false;
            BeginFloor();
            return true;
        }

        private void BeginFloor()
        {
            X = _route[RouteIndex].EntryX;
            Health = 100;
            Clock = 90 * StepsPerSecond;
            Attack = CaretakerAttack.None;
            _attackFrame = 0;
            _jumpFrames = 0;
            Ducking = false;
            _opponents.Clear();
            BuildFloorParticipants();
            // Longer legitimate groups get additional encounter time while the active fighter cap stays fixed.
            Clock += Math.Max(0, (_floorParticipants.Count + 3) / 4 - 1) * 30 * StepsPerSecond;
            SpawnNextWave();
        }

        private void BuildFloorParticipants()
        {
            _floorParticipants.Clear();
            _nextParticipant = 0;
            if (_route.Count == 0 || !_route[RouteIndex].Encounter)
                return;
            if (Practice)
            {
                _floorParticipants.Add(0);
                _floorParticipants.Add(0);
                _floorParticipants.Add(0);
                return;
            }

            foreach (var id in _residentIds)
                if (!_residentFloors.TryGetValue(id, out var homeFloor) || homeFloor == CurrentFloor)
                    _floorParticipants.Add(id);
        }

        private bool WaveDefeated()
        {
            foreach (var opponent in _opponents)
                if (opponent.Health > 0)
                    return false;
            return true;
        }

        private void SpawnNextWave()
        {
            if (_nextParticipant >= _floorParticipants.Count)
                return;
            _opponents.Clear();
            var floor = _route[RouteIndex];
            var count = Math.Min(4, _floorParticipants.Count - _nextParticipant);
            for (var i = 0; i < count; i++)
            {
                var participantIndex = _nextParticipant++;
                var residentId = _floorParticipants[participantIndex];
                if (!Practice)
                    _encounterResidents.Add(residentId);
                var boss = participantIndex == _floorParticipants.Count - 1;
                var role = boss ? CaretakerOpponentRole.FloorBoss : participantIndex % 3 == 1 ? CaretakerOpponentRole.Thrower : CaretakerOpponentRole.Brawler;
                var spawnX = floor.MinX + (floor.MaxX - floor.MinX) * (i + 1) / (count + 1);
                // Telegraph arrival through the initial timer, keeping new waves out of the Caretaker's contact zone.
                if (Math.Abs(spawnX - X) < 1400)
                    spawnX = Clamp(X + (X < (floor.MinX + floor.MaxX) / 2 ? 1800 : -1800), floor.MinX + 150, floor.MaxX - 150);
                _opponents.Add(new CaretakerOpponent { Id = RouteIndex * 1000 + participantIndex + 1, ResidentId = residentId, Role = role, X = spawnX, Health = boss ? 80 : 35, Facing = X >= spawnX ? 1 : -1, Timer = 60 + i * 20 });
            }
        }

        public void Step(CaretakerCombatInput input)
        {
            if (Phase != CaretakerCrisisPhase.Active || Paused)
                return;
            Clock--;
            if (Clock <= 0)
            {
                Phase = CaretakerCrisisPhase.Failed;
                return;
            }

            var r = _route[RouteIndex];

            // 1. Record buffered inputs (buffer window of 6 ticks / 100ms)
            if (input.Punch)
                _punchBuffer = InputBufferTicks;
            if (input.Kick)
                _kickBuffer = InputBufferTicks;
            if (input.Jump)
                _jumpBuffer = InputBufferTicks;

            // 2. Hit-stop tick counter & delayed knockback application
            var inHitStop = _hitStop > 0;
            if (_hitStop > 0)
                _hitStop--;

            foreach (var o in _opponents)
            {
                if (o.HitStop > 0)
                {
                    o.HitStop--;
                    if (o.HitStop == 0 && o.PendingKnockback != 0)
                    {
                        o.X = Clamp(o.X + o.PendingKnockback, r.MinX + 150, r.MaxX - 150);
                        o.PendingKnockback = 0;
                    }
                }
            }

            Ducking = _jumpFrames == 0 && (Attack == CaretakerAttack.None ? input.Duck : _attackDucking);

            // 3. Movement, jump and attack processing (frozen during hit-stop micro-pause)
            if (!inHitStop)
            {
                // Jump & Jump buffering: if Jump is pressed while airborne within 6 ticks of landing, immediately jump upon touching the ground.
                if (_jumpBuffer > 0 && _jumpFrames == 0 && !Ducking && Attack == CaretakerAttack.None)
                {
                    _jumpFrames = 36;
                    _jumpBuffer = 0;
                }
                if (_jumpFrames > 0)
                    _jumpFrames--;

                if (input.Move != 0 && !Ducking)
                {
                    Facing = input.Move;
                    X = Clamp(X + input.Move * (Attack == CaretakerAttack.None ? 90 : 35), r.MinX + 150, r.MaxX - 150);
                }

                // Attack initiation & input buffering: execute immediately on the earliest frame when Attack == None
                if (Attack == CaretakerAttack.None && (_kickBuffer > 0 || _punchBuffer > 0))
                {
                    Attack = _kickBuffer > 0 ? (_jumpFrames > 0 ? CaretakerAttack.JumpKick : CaretakerAttack.Kick) : CaretakerAttack.Punch;
                    _attackFrame = 0;
                    _attackSerial++;
                    _attackDucking = Ducking;
                    _kickBuffer = 0;
                    _punchBuffer = 0;
                }

                if (Attack != CaretakerAttack.None)
                {
                    _attackFrame++;
                    var activeStart = Attack == CaretakerAttack.Punch ? 5 : 9;
                    var duration = Attack == CaretakerAttack.Punch ? 18 : 28;
                    if (_attackFrame >= activeStart && _attackFrame <= activeStart + 3)
                        foreach (var o in _opponents)
                            if (o.Health > 0 && o.HitAttack != _attackSerial && (o.X - X) * Facing >= -100 && Math.Abs(o.X - X) <= (Attack == CaretakerAttack.Punch ? 1400 : 2000))
                            {
                                o.HitAttack = _attackSerial;
                                o.Health = Math.Max(0, o.Health - (Attack == CaretakerAttack.Punch ? 15 : 25));
                                o.HitStop = HitStopTicks;
                                o.PendingKnockback = Facing * 450;
                                _hitStop = HitStopTicks;
                                if (o.Health == 0)
                                    Score += o.Role == CaretakerOpponentRole.FloorBoss ? 500 : 100;
                            }

                    if (_attackFrame >= duration)
                    {
                        Attack = CaretakerAttack.None;
                        _attackFrame = 0;
                    }
                }
            }

            // 4. Opponent AI update (paused during hit-stop)
            var committed = 0;
            foreach (var active in _opponents)
                if (active.Health > 0 && active.AttackFrame > 0)
                    committed++;
            foreach (var o in _opponents)
            {
                if (o.HitStop > 0 || o.Health <= 0)
                    continue;
                o.Facing = X >= o.X ? 1 : -1;
                if (o.AttackFrame > 0)
                {
                    o.AttackFrame++;
                    if (o.AttackFrame == 24 && Math.Abs(o.X - X) <= (o.Role == CaretakerOpponentRole.Thrower ? 6500 : 1600) && !(o.Role == CaretakerOpponentRole.Thrower ? (Ducking || _jumpFrames > 8) : _jumpFrames > 10))
                        Health = Math.Max(0, Health - (o.Role == CaretakerOpponentRole.FloorBoss ? 15 : 7));
                    if (o.AttackFrame >= 42)
                    {
                        o.AttackFrame = 0;
                        o.Timer = o.Role == CaretakerOpponentRole.FloorBoss ? 35 : 60;
                    }
                }
                else if (o.Timer > 0)
                    o.Timer--;
                else if (Math.Abs(o.X - X) <= (o.Role == CaretakerOpponentRole.Thrower ? 6500 : 1300) && committed < 1)
                {
                    o.AttackFrame = 1;
                    committed++;
                }
                else if (o.Role != CaretakerOpponentRole.Thrower || Math.Abs(o.X - X) > 6500)
                    o.X = Clamp(o.X + o.Facing * (o.Role == CaretakerOpponentRole.FloorBoss ? 45 : 55), r.MinX + 150, r.MaxX - 150);
            }

            // 5. Decrement input buffers if input was not pressed on this tick
            if (!input.Punch && _punchBuffer > 0)
                _punchBuffer--;
            if (!input.Kick && _kickBuffer > 0)
                _kickBuffer--;
            if (!input.Jump && _jumpBuffer > 0)
                _jumpBuffer--;

            if (Health <= 0)
                Phase = CaretakerCrisisPhase.Failed;
            else if (WaveDefeated() && _hitStop == 0 && _nextParticipant < _floorParticipants.Count)
                SpawnNextWave();
        }

        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        public bool ValidateRoute(BuildingTopologyState topology, out string reason)
        {
            if (!TryBuildRoute(topology, out var live, out reason))
                return false;
            if (live.Count != _route.Count)
            {
                reason = "The encounter route changed. Exit and restart.";
                return false;
            }

            for (var i = 0; i < live.Count; i++)
            {
                var a = live[i];
                var b = _route[i];
                if (a.Floor != b.Floor || a.EntryX != b.EntryX || a.ExitX != b.ExitX || a.MinX != b.MinX || a.MaxX != b.MaxX || a.Encounter != b.Encounter)
                {
                    reason = "The encounter route changed. Exit and restart.";
                    return false;
                }
            }

            reason = "";
            return true;
        }

        public bool CanAdvance(BuildingTopologyState topology, out string reason)
        {
            if (Phase != CaretakerCrisisPhase.Active || Paused)
            {
                reason = "No unpaused encounter is active.";
                return false;
            }

            if (!FloorCleared)
            {
                reason = "Clear the floor encounter first.";
                return false;
            }

            if (Math.Abs(X - _route[RouteIndex].ExitX) > 1200)
            {
                reason = "Reach the marked stair door.";
                return false;
            }

            return ValidateRoute(topology, out reason);
        }

        public bool Advance(BuildingTopologyState topology)
        {
            if (!CanAdvance(topology, out _))
                return false;
            if (RouteIndex + 1 < _route.Count)
            {
                RouteIndex++;
                BeginFloor();
                return true;
            }

            if (Practice)
            {
                Exit();
                return true;
            }

            Phase = CaretakerCrisisPhase.Resolved;
            foreach (var id in _encounterResidents)
                _evictions.Add(new CaretakerEvictionRecord { ResidentId = id, Floor = _residentFloors.TryGetValue(id, out var floor) ? floor : CurrentFloor, FactionId = FactionId, Cause = Cause });
            return true;
        }

        public bool CanPractice(BuildingTopologyState topology, out string reason)
        {
            if (Phase == CaretakerCrisisPhase.Active || Phase == CaretakerCrisisPhase.Failed || Practice)
            {
                reason = "Exit the current encounter first.";
                return false;
            }

            if (topology == null || !topology.TryGetFloorSlab(0, out var lobby) || lobby.Width < 2)
            {
                reason = "Build a lobby floor with at least two cells.";
                return false;
            }

            reason = "";
            return true;
        }

        public bool StartPractice(BuildingTopologyState topology)
        {
            if (!CanPractice(topology, out _))
                return false;
            _beforePractice = ToSaveData();
            Practice = true;
            _residentIds = new[]
            {
                0,
                0
            };
            var reachable = new List<int>
            {
                0
            };
            foreach (var slab in topology.FloorSlabs)
            {
                if (slab.Key == 0)
                    continue;
                _floors = new[]
                {
                    slab.Key
                };
                if (TryBuildRoute(topology, out _, out _))
                    reachable.Add(slab.Key);
            }

            reachable.Sort();
            _floors = reachable.ToArray();
            TryBuildRoute(topology, out var route, out _);
            _route.Clear();
            _route.AddRange(route);
            RouteIndex = 0;
            Score = 0;
            Phase = CaretakerCrisisPhase.Active;
            Paused = false;
            BeginFloor();
            return true;
        }

        public bool Retry(BuildingTopologyState topology)
        {
            if (Phase != CaretakerCrisisPhase.Failed)
                return false;
            return Start(topology);
        }

        public void Exit()
        {
            if (Practice)
            {
                RestorePractice();
                return;
            }

            if (Phase == CaretakerCrisisPhase.Active || Phase == CaretakerCrisisPhase.Failed)
            {
                Phase = CaretakerCrisisPhase.Ready;
                Paused = false;
                Attack = CaretakerAttack.None;
                _punchBuffer = 0;
                _kickBuffer = 0;
                _jumpBuffer = 0;
                _hitStop = 0;
                _opponents.Clear();
            }
        }

        private void RestorePractice()
        {
            var restored = FromSaveData(_beforePractice);
            Phase = restored.Phase;
            FactionId = restored.FactionId;
            Cause = restored.Cause;
            WarningDays = restored.WarningDays;
            ReliefDays = restored.ReliefDays;
            LastEvaluationTick = restored.LastEvaluationTick;
            _residentIds = restored._residentIds;
            _floors = restored._floors;
            _route.Clear();
            _opponents.Clear();
            _floorParticipants.Clear();
            _nextParticipant = 0;
            _evictions.Clear();
            _evictions.AddRange(restored._evictions);
            _residentFloors.Clear();
            foreach (var pair in restored._residentFloors)
                _residentFloors[pair.Key] = pair.Value;
            ConsequencesApplied = restored.ConsequencesApplied;
            Practice = false;
            _beforePractice = null;
            Paused = false;
            RouteIndex = 0;
            Attack = CaretakerAttack.None;
            _punchBuffer = 0;
            _kickBuffer = 0;
            _jumpBuffer = 0;
            _hitStop = 0;
            Health = 100;
        }

        public void SetPaused(bool paused)
        {
            if (Phase == CaretakerCrisisPhase.Active)
                Paused = paused;
        }

        public void MarkConsequencesApplied()
        {
            if (Phase == CaretakerCrisisPhase.Resolved)
                ConsequencesApplied = true;
        }

        public CaretakerCrisisSaveData ToSaveData()
        {
            var data = new CaretakerCrisisSaveData
            {
                practice = Practice,
                beforePractice = _beforePractice,
                attackSerial = _attackSerial,
                nextParticipant = _nextParticipant,
                floorParticipantIds = _floorParticipants.ToArray(),
                encounterResidentIds = new int[_encounterResidents.Count],
                residentFloors = new CaretakerResidentFloorSaveData[_residentFloors.Count],
                phase = (int)Phase,
                warningDays = WarningDays,
                reliefDays = ReliefDays,
                lastEvaluationTick = LastEvaluationTick,
                factionId = FactionId,
                cause = Cause,
                affectedResidentIds = (int[])_residentIds.Clone(),
                affectedFloors = (int[])_floors.Clone(),
                routeIndex = RouteIndex,
                x = X,
                facing = Facing,
                health = Health,
                attack = (int)Attack,
                attackFrame = _attackFrame,
                jumpFrames = _jumpFrames,
                hitStop = _hitStop,
                punchBuffer = _punchBuffer,
                kickBuffer = _kickBuffer,
                jumpBuffer = _jumpBuffer,
                clock = Clock,
                score = Score,
                attackDucking = _attackDucking,
                paused = Paused,
                consequencesApplied = ConsequencesApplied,
                route = new CaretakerRouteSaveData[_route.Count],
                opponents = new CaretakerOpponentSaveData[_opponents.Count],
                evictions = new CaretakerEvictionSaveData[_evictions.Count]
            };
            for (var i = 0; i < _route.Count; i++)
            {
                var r = _route[i];
                data.route[i] = new CaretakerRouteSaveData
                {
                    floor = r.Floor,
                    minX = r.MinX,
                    maxX = r.MaxX,
                    entryX = r.EntryX,
                    exitX = r.ExitX,
                    encounter = r.Encounter
                };
            }

            for (var i = 0; i < _opponents.Count; i++)
            {
                var o = _opponents[i];
                data.opponents[i] = new CaretakerOpponentSaveData
                {
                    id = o.Id,
                    residentId = o.ResidentId,
                    role = (int)o.Role,
                    x = o.X,
                    health = o.Health,
                    facing = o.Facing,
                    timer = o.Timer,
                    attackFrame = o.AttackFrame,
                    hitAttack = o.HitAttack,
                    hitStop = o.HitStop,
                    pendingKnockback = o.PendingKnockback
                };
            }

            for (var i = 0; i < _evictions.Count; i++)
            {
                var e = _evictions[i];
                data.evictions[i] = new CaretakerEvictionSaveData
                {
                    residentId = e.ResidentId,
                    floor = e.Floor,
                    factionId = e.FactionId,
                    cause = e.Cause
                };
            }

            _encounterResidents.CopyTo(data.encounterResidentIds);
            var mappingIndex = 0;
            foreach (var pair in _residentFloors)
                data.residentFloors[mappingIndex++] = new CaretakerResidentFloorSaveData
                {
                    residentId = pair.Key,
                    floor = pair.Value
                };
            return data;
        }

        public static CaretakerCrisisState FromSaveData(CaretakerCrisisSaveData data)
        {
            var state = new CaretakerCrisisState();
            if (data == null)
                return state;
            state.Phase = Enum.IsDefined(typeof(CaretakerCrisisPhase), data.phase) ? (CaretakerCrisisPhase)data.phase : CaretakerCrisisPhase.Dormant;
            if (data.encounterResidentIds != null)
                foreach (var id in data.encounterResidentIds)
                    state._encounterResidents.Add(id);
            state.Practice = data.practice;
            state._beforePractice = data.beforePractice;
            state._attackSerial = Math.Max(0, data.attackSerial);
            if (data.residentFloors != null)
                foreach (var mapping in data.residentFloors)
                    if (mapping != null)
                        state._residentFloors[mapping.residentId] = mapping.floor;
            state.WarningDays = Math.Max(0, data.warningDays);
            state.ReliefDays = Math.Max(0, data.reliefDays);
            state.LastEvaluationTick = data.lastEvaluationTick;
            state.FactionId = data.factionId ?? "";
            state.Cause = data.cause ?? "none";
            state._residentIds = SortedUnique(data.affectedResidentIds ?? Array.Empty<int>());
            state._floors = SortedUnique(data.affectedFloors ?? Array.Empty<int>());
            state._encounterResidents.RemoveWhere(id => Array.IndexOf(state._residentIds, id) < 0);
            if (data.route != null)
                foreach (var r in data.route)
                    if (r != null && r.maxX - r.minX >= 2000 && r.entryX >= r.minX && r.entryX <= r.maxX && r.exitX >= r.minX && r.exitX <= r.maxX)
                        state._route.Add(new CaretakerRouteFloor(r.floor, r.minX, r.maxX, r.entryX, r.exitX, r.encounter));
            state.RouteIndex = Clamp(data.routeIndex, 0, Math.Max(0, state._route.Count - 1));
            state.BuildFloorParticipants();
            state._nextParticipant = Clamp(data.floorParticipantIds != null ? data.nextParticipant : Math.Min(4, state._floorParticipants.Count), 0, state._floorParticipants.Count);
            state.X = data.x;
            state.Facing = data.facing < 0 ? -1 : 1;
            state.Health = Clamp(data.health, 0, 100);
            state.Clock = Math.Max(0, data.clock);
            state.Score = Math.Max(0, data.score);
            state.ConsequencesApplied = data.consequencesApplied;
            state._attackDucking = data.attackDucking;
            state.Attack = Enum.IsDefined(typeof(CaretakerAttack), data.attack) ? (CaretakerAttack)data.attack : CaretakerAttack.None;
            state._attackFrame = Clamp(data.attackFrame, 0, 28);
            state._jumpFrames = Clamp(data.jumpFrames, 0, 36);
            state._hitStop = Math.Max(0, data.hitStop);
            state._punchBuffer = Math.Max(0, data.punchBuffer);
            state._kickBuffer = Math.Max(0, data.kickBuffer);
            state._jumpBuffer = Math.Max(0, data.jumpBuffer);
            if (data.opponents != null)
                foreach (var o in data.opponents)
                    if (state._opponents.Count < 4 && o != null && Enum.IsDefined(typeof(CaretakerOpponentRole), o.role))
                    {
                        state._opponents.Add(new CaretakerOpponent
                        {
                            Id = o.id,
                            ResidentId = o.residentId,
                            Role = (CaretakerOpponentRole)o.role,
                            X = o.x,
                            Health = Clamp(o.health, 0, 80),
                            Facing = o.facing < 0 ? -1 : 1,
                            Timer = Math.Max(0, o.timer),
                            AttackFrame = Clamp(o.attackFrame, 0, 42),
                            HitAttack = o.hitAttack,
                            HitStop = Math.Max(0, o.hitStop),
                            PendingKnockback = o.pendingKnockback
                        });
                        state._attackSerial = Math.Max(state._attackSerial, o.hitAttack);
                    }

            var evictionIds = new HashSet<int>();
            if (data.evictions != null)
                foreach (var e in data.evictions)
                    if (e != null && !state.Practice && state.Phase == CaretakerCrisisPhase.Resolved && state._encounterResidents.Contains(e.residentId) && evictionIds.Add(e.residentId))
                        state._evictions.Add(new CaretakerEvictionRecord { ResidentId = e.residentId, Floor = state._residentFloors.TryGetValue(e.residentId, out var floor) ? floor : e.floor, FactionId = state.FactionId, Cause = state.Cause });
            // Loaded combat is safely paused; management must explicitly resume after validating the live route.
            state.Paused = state.Phase == CaretakerCrisisPhase.Active;
            if (state._route.Count > 0)
            {
                var floor = state._route[state.RouteIndex];
                state.X = Clamp(state.X, floor.MinX + 150, floor.MaxX - 150);
                foreach (var opponent in state._opponents)
                    opponent.X = Clamp(opponent.X, floor.MinX + 150, floor.MaxX - 150);
            }

            if (state.Phase == CaretakerCrisisPhase.Active && (state._route.Count == 0 || state._residentIds.Length == 0))
            {
                state.Phase = CaretakerCrisisPhase.Ready;
                state.Paused = false;
            }

            return state;
        }
    }
}
