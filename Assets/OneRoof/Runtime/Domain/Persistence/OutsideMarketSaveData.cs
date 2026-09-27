using System;

namespace OneRoof.Domain.Persistence
{
    /// <summary>Persisted outside-market counterparty accounting totals.</summary>
    [Serializable]
    public sealed class OutsideMarketSaveData
    {
        public int version;
        public long cashBalance;
        public long externalContractRevenue;
        public long wageOutflow;
        public long purchaseRevenue;
        public long creditFunding;
        public long creditRepayment;
        public long creditReceivableBalance;
    }
}
