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
        public HouseholdSaveData[] households;
        public PersonSaveData[] persons;
    }
}
