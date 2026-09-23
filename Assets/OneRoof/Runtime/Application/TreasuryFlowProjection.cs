namespace OneRoof.Application.Economy
{
    /// <summary>
    /// Immutable snapshot of one day's treasury flows. Inflow and expense amounts are
    /// exposed as non-negative magnitudes; <see cref="Net"/> is their signed balance.
    /// </summary>
    public readonly struct TreasuryFlowProjection
    {
        public TreasuryFlowProjection(long rent, long tax, long upkeep, long subsidy, long construction, long constructionSalvage = 0)
        {
            Rent = rent;
            Tax = tax;
            Upkeep = upkeep;
            Subsidy = subsidy;
            Construction = construction;
            ConstructionSalvage = constructionSalvage;
            Net = rent + tax + constructionSalvage - upkeep - subsidy - construction;
        }

        /// <summary>Daily residential and commercial rent receipts.</summary>
        public long Rent { get; }

        /// <summary>Daily tax receipts.</summary>
        public long Tax { get; }

        /// <summary>Daily utility upkeep expense.</summary>
        public long Upkeep { get; }

        /// <summary>Daily active policy subsidy expense.</summary>
        public long Subsidy { get; }

        /// <summary>Construction expenses recorded for the day.</summary>
        public long Construction { get; }

        /// <summary>Daily treasury inflow from construction salvage.</summary>
        public long ConstructionSalvage { get; }

        /// <summary>Daily treasury balance change: rent + tax + salvage - upkeep - subsidy - construction.</summary>
        public long Net { get; }
    }
}
