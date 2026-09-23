using System;

namespace OneRoof.Domain.Persistence
{
    /// <summary>
    /// Serializable sub-record capturing tower cash balance, sandbox toggle, and cumulative metrics.
    /// Owned and serialized directly by TowerEconomyState.
    /// </summary>
    [Serializable]
    public sealed class EconomySaveData
    {
        public long cashBalance;
        public bool sandboxMode;
        public long totalRevenue;
        public long totalExpenses;
        public int policyVersion;
        public float rentCapMultiplier;
        public float commercialTaxRate;
        public bool transitSubsidyEnabled;
        public bool quietHoursEnabled;
        public long lastSettlementTick;
        public long pendingRent;
        public long pendingTax;
        public long pendingUpkeep;
        public long pendingSubsidy;
        public long pendingConstruction;
        public long pendingConstructionSalvage;
        public long lastDailyRent;
        public long lastDailyTax;
        public long lastDailyUpkeep;
        public long lastDailySubsidy;
        public long lastDailyConstruction;
        public long lastDailyConstructionSalvage;
    }
}
