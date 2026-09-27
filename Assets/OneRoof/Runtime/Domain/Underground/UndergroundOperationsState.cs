using System;
using System.Collections.Generic;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Scrutiny;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Underground
{
    public enum InvestigatorPhase { None, Street, Lobby, Core, Corridor, Inspect, Return }

    /// <summary>Deterministic operating economy and noncombat investigation for the excavated district.</summary>
    public sealed class UndergroundOperationsState
    {
        private readonly Dictionary<int, int> _roomByResident = new Dictionary<int, int>();
        private readonly List<int> _orderedResidents = new List<int>();
        private int _disruptedRoomId;
        private long _disruptionEndsAtTick;
        private int _phaseTicks;
        private long _lastVisitTick;
        private readonly List<UndergroundCell> _investigatorRoute = new List<UndergroundCell>();
        private int _routeIndex;

        public int Supplies { get; private set; }
        public int Intel { get; private set; }
        public int ResearchPoints { get; private set; }
        public float Exposure { get; private set; }
        public int CoverPriority { get; private set; } = 1;
        public int StaffingPriority { get; private set; } = 1;
        public int SecurityPosture { get; private set; } = 1;
        public InvestigatorPhase Phase { get; private set; }
        public int InvestigatorTargetRoomId { get; private set; }
        public int InvestigatorX { get; private set; }
        public int InvestigatorDepth { get; private set; }
        public int DisruptedRoomId => _disruptedRoomId;
        public long DisruptionEndsAtTick => _disruptionEndsAtTick;
        public IReadOnlyDictionary<int, int> RoomByResident => _roomByResident;
        public int BackupPowerCapacity { get; private set; }
        public float RepairBoost { get; private set; }
        public float CareBoost { get; private set; }
        public float CommonsMoraleBoost { get; private set; }
        public float TrainingBoost { get; private set; }
        public int ShelterCapacity { get; private set; }
        public int LastContractIncome { get; private set; }
        public int LastDailyCost { get; private set; }

        public bool SetPolicy(int cover, int staffing, int security)
        {
            if (cover < 0 || cover > 2 || staffing < 0 || staffing > 2 || security < 0 || security > 2) return false;
            CoverPriority = cover;
            StaffingPriority = staffing;
            SecurityPosture = security;
            return true;
        }

        public bool IsDisrupted(int roomId, long tick) => roomId == _disruptedRoomId && tick < _disruptionEndsAtTick;

        public void AdvanceDaily(UndergroundDigState layout, PopulationState population,
            TowerEconomyState economy, OutsideMarketState outside, ScrutinyState scrutiny, long tick)
        {
            if (layout == null || population == null || economy == null || outside == null || scrutiny == null) return;
            AssignResidents(layout, population);
            BackupPowerCapacity = 0;
            RepairBoost = 0f;
            CareBoost = 0f;
            CommonsMoraleBoost = 0f;
            TrainingBoost = 0f;
            ShelterCapacity = 0;
            LastContractIncome = 0;
            LastDailyCost = 0;
            var storage = 0;
            for (var i = 0; i < layout.Rooms.Count; i++)
            {
                var room = layout.Rooms[i];
                if (room.IsReachable && room.Type == UndergroundRoomType.SupplyDepot)
                    storage += Math.Max(1, room.Capacity) * 4;
            }
            // Delivered supplies have a named outside counterparty and cost tower cash.
            var order = Math.Min(Math.Max(0, storage - Supplies), Math.Max(0, 4 + StaffingPriority * 3));
            var affordable = economy.SandboxMode ? order : (int)Math.Min(order, Math.Max(0, economy.CashBalance / 2));
            if (affordable > 0)
            {
                var cost = affordable * 2;
                economy.ChargeDailyExpense(cost, false);
                outside.RecordTowerPurchase(cost);
                Supplies += affordable;
                LastDailyCost += cost;
            }

            var cover = 0;
            var security = 0;
            var vault = 0;
            var researchFactor = 1f + Math.Min(.25f, ResearchPoints * .0025f);
            for (var i = 0; i < layout.Rooms.Count; i++)
            {
                var room = layout.Rooms[i];
                if (!room.IsReachable || IsDisrupted(room.Id, tick)) continue;
                var staffed = CountStaff(room.Id);
                if (staffed < room.RequiredStaff) continue;
                var upkeep = room.DailyUpkeep + (StaffingPriority * staffed);
                economy.ChargeDailyExpense(upkeep, false);
                LastDailyCost += upkeep;
                PayResidentWages(room.Id, population, economy);
                var capacity = Math.Max(1, room.Capacity);
                switch (room.Type)
                {
                    case UndergroundRoomType.AccessHub: break;
                    case UndergroundRoomType.SupplyDepot: break;
                    case UndergroundRoomType.Generator:
                        if (ConsumeSupplies(2)) BackupPowerCapacity += (int)Math.Round(capacity * researchFactor);
                        break;
                    case UndergroundRoomType.Workshop:
                        if (ConsumeSupplies(1)) RepairBoost += .02f * capacity * researchFactor;
                        break;
                    case UndergroundRoomType.OperationsCenter:
                        if (Intel >= 1 && ConsumeSupplies(1))
                        {
                            Intel--;
                            var payment = (int)Math.Round(10 * capacity * researchFactor);
                            outside.RecordTowerContractPayment(economy, payment);
                            LastContractIncome += payment;
                            Exposure = Clamp(Exposure + .05f);
                        }
                        break;
                    case UndergroundRoomType.ResearchLab:
                        if (Intel >= 2 && ConsumeSupplies(1)) { Intel -= 2; ResearchPoints += capacity; }
                        break;
                    case UndergroundRoomType.CoverOffice: cover += capacity; break;
                    case UndergroundRoomType.SecurityPost: security += capacity; break;
                    case UndergroundRoomType.StaffCommons: CommonsMoraleBoost += .01f * capacity; break;
                    case UndergroundRoomType.Infirmary: CareBoost += .01f * capacity; break;
                    case UndergroundRoomType.Vault: vault += capacity; break;
                    case UndergroundRoomType.TrainingRoom: TrainingBoost += .01f * capacity; break;
                    case UndergroundRoomType.Communications:
                        Intel = Math.Min(1000, Intel + capacity * 2);
                        Exposure = Clamp(Exposure + .005f);
                        break;
                    case UndergroundRoomType.EmergencyShelter: ShelterCapacity += capacity; break;
                }
            }
            // Cover and research mitigate exposure; hard security can deter a visit,
            // while a vault protects the treasury if disruption succeeds.
            Exposure = Clamp(Exposure - .012f - cover * (.006f + CoverPriority * .003f) -
                Math.Min(.025f, ResearchPoints * .0002f));
            _securityStrength = security + SecurityPosture * 2;
            _vaultProtection = vault;
            if (Exposure > .2f) scrutiny.RecordAggressivePolicy(Exposure * .025f);
        }

        private int _securityStrength;
        private int _vaultProtection;

        public void AdvanceTick(UndergroundDigState layout, TowerEconomyState economy,
            ScrutinyState scrutiny, long tick)
        {
            if (layout == null || economy == null || scrutiny == null) return;
            if (_disruptedRoomId != 0 && tick >= _disruptionEndsAtTick) _disruptedRoomId = 0;
            if (Phase == InvestigatorPhase.None)
            {
                if (Exposure < .28f || tick - _lastVisitTick < 720 || tick % 120 != 0) return;
                var target = ChooseTarget(layout);
                if (target == null) return;
                if (!BuildRoute(layout, target)) return;
                InvestigatorTargetRoomId = target.Id;
                InvestigatorX = UndergroundDigState.GridWidthCells - 1;
                InvestigatorDepth = -1;
                _phaseTicks = 0;
                Phase = InvestigatorPhase.Street;
                _lastVisitTick = tick;
                return;
            }
            _phaseTicks++;
            switch (Phase)
            {
                case InvestigatorPhase.Street:
                    InvestigatorX = Math.Max(layout.AccessCore?.X ?? 0, InvestigatorX - 1);
                    if (_phaseTicks >= 20) Next(InvestigatorPhase.Lobby);
                    break;
                case InvestigatorPhase.Lobby:
                    if (_phaseTicks >= 12) Next(InvestigatorPhase.Core);
                    break;
                case InvestigatorPhase.Core:
                    InvestigatorX = layout.AccessCore?.X ?? InvestigatorX;
                    if (_phaseTicks >= 12)
                    {
                        InvestigatorDepth = 0;
                        _routeIndex = 0;
                        Next(InvestigatorPhase.Corridor);
                    }
                    break;
                case InvestigatorPhase.Corridor:
                    var target = FindRoom(layout, InvestigatorTargetRoomId);
                    if (target == null || !target.IsReachable) { Next(InvestigatorPhase.Return); break; }
                    if (_investigatorRoute.Count == 0 && !BuildRoute(layout, target)) { Next(InvestigatorPhase.Return); break; }
                    if (_routeIndex + 1 < _investigatorRoute.Count) _routeIndex++;
                    InvestigatorX = _investigatorRoute[_routeIndex].X;
                    InvestigatorDepth = _investigatorRoute[_routeIndex].Depth;
                    if (_routeIndex == _investigatorRoute.Count - 1) Next(InvestigatorPhase.Inspect);
                    break;
                case InvestigatorPhase.Inspect:
                    if (_phaseTicks >= Math.Max(8, 28 - _securityStrength))
                    {
                        if (_securityStrength < 12)
                        {
                            _disruptedRoomId = InvestigatorTargetRoomId;
                            _disruptionEndsAtTick = tick + DailySchedule.TicksPerDay + 240;
                            economy.ChargeDailyExpense(Math.Max(0, 25 - _vaultProtection), false);
                            scrutiny.RecordAggressivePolicy(.18f);
                            Exposure = Clamp(Exposure + .10f);
                        }
                        else Exposure = Clamp(Exposure - .10f);
                        Next(InvestigatorPhase.Return);
                    }
                    break;
                case InvestigatorPhase.Return:
                    if (_routeIndex > 0 && _investigatorRoute.Count > _routeIndex)
                    {
                        _routeIndex--;
                        InvestigatorX = _investigatorRoute[_routeIndex].X;
                        InvestigatorDepth = _investigatorRoute[_routeIndex].Depth;
                    }
                    else if (InvestigatorX < UndergroundDigState.GridWidthCells - 1)
                    {
                        InvestigatorDepth = -1;
                        InvestigatorX++;
                    }
                    else
                    {
                        Phase = InvestigatorPhase.None;
                        InvestigatorTargetRoomId = 0;
                        InvestigatorDepth = -1;
                        _investigatorRoute.Clear();
                    }
                    break;
            }
        }

        private void Next(InvestigatorPhase phase) { Phase = phase; _phaseTicks = 0; }

        private static UndergroundRoom FindRoom(UndergroundDigState layout, int id)
        {
            for (var i = 0; i < layout.Rooms.Count; i++) if (layout.Rooms[i].Id == id) return layout.Rooms[i];
            return null;
        }

        private static UndergroundRoom ChooseTarget(UndergroundDigState layout)
        {
            UndergroundRoom target = null;
            for (var i = 0; i < layout.Rooms.Count; i++)
            {
                var room = layout.Rooms[i];
                if (!room.IsReachable || room.Type == UndergroundRoomType.AccessHub) continue;
                if (target == null || room.Id < target.Id) target = room;
            }
            return target;
        }

        private bool BuildRoute(UndergroundDigState layout, UndergroundRoom target)
        {
            _investigatorRoute.Clear();
            if (!layout.AccessCore.HasValue) return false;
            var width = UndergroundDigState.GridWidthCells;
            var total = width * UndergroundDigState.MaxDepthCells;
            var start = layout.AccessCore.Value.Depth * width + layout.AccessCore.Value.X;
            var seen = new bool[total];
            var previous = new int[total];
            for (var i = 0; i < previous.Length; i++) previous[i] = -1;
            var queue = new Queue<int>();
            queue.Enqueue(start);
            seen[start] = true;
            var goal = -1;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var x = cell % width;
                var depth = cell / width;
                if (AdjacentToRoom(x, depth, target)) { goal = cell; break; }
                Visit(x - 1, depth, cell);
                Visit(x + 1, depth, cell);
                Visit(x, depth - 1, cell);
                Visit(x, depth + 1, cell);
            }
            if (goal < 0) return false;
            for (var cell = goal; cell >= 0; cell = previous[cell])
                _investigatorRoute.Add(new UndergroundCell(cell % width, cell / width));
            _investigatorRoute.Reverse();
            _routeIndex = 0;
            return true;

            void Visit(int x, int depth, int from)
            {
                if (x < 0 || x >= width || depth < 0 || depth >= UndergroundDigState.MaxDepthCells ||
                    (!layout.IsCorridor(x, depth) && !layout.IsShaft(x, depth))) return;
                var index = depth * width + x;
                if (seen[index]) return;
                seen[index] = true;
                previous[index] = from;
                queue.Enqueue(index);
            }
        }

        private static bool AdjacentToRoom(int x, int depth, UndergroundRoom room)
        {
            var horizontal = depth >= room.Depth && depth < room.Depth + room.Height &&
                (x == room.X - 1 || x == room.X + room.Width);
            var vertical = x >= room.X && x < room.X + room.Width &&
                (depth == room.Depth - 1 || depth == room.Depth + room.Height);
            return horizontal || vertical;
        }

        private void AssignResidents(UndergroundDigState layout, PopulationState population)
        {
            _roomByResident.Clear();
            _orderedResidents.Clear();
            var maxStaff = 0;
            for (var i = 0; i < layout.Rooms.Count; i++)
                if (layout.Rooms[i].IsReachable && layout.Rooms[i].Type == UndergroundRoomType.AccessHub)
                    maxStaff += layout.Rooms[i].Capacity;
            for (var i = 0; i < population.Persons.Count; i++)
            {
                var person = population.Persons[i];
                if (person.CurrentLocation.IsOutside || person.CurrentActivity == ActivityKind.Commuting ||
                    person.GetNeedSatisfaction(NeedKind.Energy) < .35f) continue;
                _orderedResidents.Add(person.Id.Value);
            }
            _orderedResidents.Sort();
            var next = 0;
            for (var i = 0; i < layout.Rooms.Count; i++)
            {
                var room = layout.Rooms[i];
                if (!room.IsReachable) continue;
                for (var slot = 0; slot < room.RequiredStaff && next < _orderedResidents.Count && next < maxStaff; slot++)
                    _roomByResident[_orderedResidents[next++]] = room.Id;
            }
        }

        private int CountStaff(int roomId)
        {
            var count = 0;
            foreach (var assignment in _roomByResident)
                if (assignment.Value == roomId) count++;
            return count;
        }

        private void PayResidentWages(int roomId, PopulationState population, TowerEconomyState economy)
        {
            foreach (var assignment in _roomByResident)
            {
                if (assignment.Value != roomId || !population.TryGetPerson(new EntityId(assignment.Key), out var person) ||
                    !population.TryGetHousehold(person.HouseholdId, out var household)) continue;
                const int wage = 3;
                economy.ChargeDailyExpense(wage, false);
                household.RecordDailyIncome(wage);
                LastDailyCost += wage;
            }
        }

        private bool ConsumeSupplies(int amount)
        {
            if (Supplies < amount) return false;
            Supplies -= amount;
            return true;
        }

        public UndergroundOperationsSaveData ToSaveData()
        {
            var ids = new List<int>(_roomByResident.Keys);
            ids.Sort();
            var rooms = new int[ids.Count];
            for (var i = 0; i < ids.Count; i++) rooms[i] = _roomByResident[ids[i]];
            var routeX = new int[_investigatorRoute.Count];
            var routeDepth = new int[_investigatorRoute.Count];
            for (var i = 0; i < _investigatorRoute.Count; i++)
            {
                routeX[i] = _investigatorRoute[i].X;
                routeDepth[i] = _investigatorRoute[i].Depth;
            }
            return new UndergroundOperationsSaveData
            {
                supplies = Supplies, intel = Intel, researchPoints = ResearchPoints, exposure = Exposure,
                coverPriority = CoverPriority, staffingPriority = StaffingPriority, securityPosture = SecurityPosture,
                assignedResidentIds = ids.ToArray(), assignedRoomIds = rooms,
                disruptedRoomId = _disruptedRoomId, disruptionEndsAtTick = _disruptionEndsAtTick,
                investigatorPhase = (int)Phase, investigatorTargetRoomId = InvestigatorTargetRoomId,
                investigatorX = InvestigatorX, investigatorDepth = InvestigatorDepth,
                investigatorPhaseTicks = _phaseTicks, lastVisitTick = _lastVisitTick,
                securityStrength = _securityStrength, vaultProtection = _vaultProtection,
                backupPowerCapacity = BackupPowerCapacity, repairBoost = RepairBoost,
                careBoost = CareBoost, commonsMoraleBoost = CommonsMoraleBoost, trainingBoost = TrainingBoost,
                shelterCapacity = ShelterCapacity, lastContractIncome = LastContractIncome,
                lastDailyCost = LastDailyCost,
                investigatorRouteX = routeX, investigatorRouteDepth = routeDepth,
                investigatorRouteIndex = _routeIndex
            };
        }

        public static UndergroundOperationsState FromSaveData(UndergroundOperationsSaveData data)
        {
            var state = new UndergroundOperationsState();
            if (data == null) return state;
            state.Supplies = Math.Max(0, data.supplies);
            state.Intel = Math.Max(0, data.intel);
            state.ResearchPoints = Math.Max(0, data.researchPoints);
            state.Exposure = Clamp(data.exposure);
            state.SetPolicy(data.coverPriority, data.staffingPriority, data.securityPosture);
            var ids = data.assignedResidentIds;
            var rooms = data.assignedRoomIds;
            if (ids != null && rooms != null)
                for (var i = 0; i < Math.Min(ids.Length, rooms.Length); i++)
                    if (ids[i] > 0 && rooms[i] > 0) state._roomByResident[ids[i]] = rooms[i];
            state._disruptedRoomId = Math.Max(0, data.disruptedRoomId);
            state._disruptionEndsAtTick = Math.Max(0, data.disruptionEndsAtTick);
            state.Phase = Enum.IsDefined(typeof(InvestigatorPhase), data.investigatorPhase)
                ? (InvestigatorPhase)data.investigatorPhase : InvestigatorPhase.None;
            state.InvestigatorTargetRoomId = Math.Max(0, data.investigatorTargetRoomId);
            state.InvestigatorX = data.investigatorX;
            state.InvestigatorDepth = data.investigatorDepth;
            state._phaseTicks = Math.Max(0, data.investigatorPhaseTicks);
            state._lastVisitTick = Math.Max(0, data.lastVisitTick);
            state._securityStrength = Math.Max(0, data.securityStrength);
            state._vaultProtection = Math.Max(0, data.vaultProtection);
            state.BackupPowerCapacity = Math.Max(0, data.backupPowerCapacity);
            state.RepairBoost = Math.Max(0f, data.repairBoost);
            state.CareBoost = Math.Max(0f, data.careBoost);
            state.CommonsMoraleBoost = Math.Max(0f, data.commonsMoraleBoost);
            state.TrainingBoost = Math.Max(0f, data.trainingBoost);
            state.ShelterCapacity = Math.Max(0, data.shelterCapacity);
            state.LastContractIncome = Math.Max(0, data.lastContractIncome);
            state.LastDailyCost = Math.Max(0, data.lastDailyCost);
            if (data.investigatorRouteX != null && data.investigatorRouteDepth != null)
                for (var i = 0; i < Math.Min(data.investigatorRouteX.Length, data.investigatorRouteDepth.Length); i++)
                    if (data.investigatorRouteX[i] >= 0 && data.investigatorRouteX[i] < UndergroundDigState.GridWidthCells &&
                        data.investigatorRouteDepth[i] >= 0 && data.investigatorRouteDepth[i] < UndergroundDigState.MaxDepthCells)
                        state._investigatorRoute.Add(new UndergroundCell(data.investigatorRouteX[i], data.investigatorRouteDepth[i]));
            state._routeIndex = Math.Max(0, Math.Min(data.investigatorRouteIndex, state._investigatorRoute.Count - 1));
            return state;
        }

        private static float Clamp(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
