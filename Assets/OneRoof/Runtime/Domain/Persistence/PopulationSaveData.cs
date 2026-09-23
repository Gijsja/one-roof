using System;

namespace OneRoof.Domain.Persistence
{
    /// <summary>
    /// Serializable sub-record capturing households and resident person records.
    /// Owned and serialized directly by PopulationState.
    /// </summary>
    [Serializable]
    public sealed class PopulationSaveData
    {
        /// <summary>Version of the household cash ledger fields; 0 denotes a pre-ledger save.</summary>
        public int householdLedgerVersion;
        public HouseholdSaveData[] households;
        public PersonSaveData[] persons;
    }
}
