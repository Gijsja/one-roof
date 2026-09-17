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
    }
}
