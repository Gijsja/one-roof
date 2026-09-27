namespace OneRoof.Domain.Commands
{
    /// <summary>Sets operation-wide priorities; no individual resident is ordered.</summary>
    public sealed class SetUndergroundPolicyCommand : ICommand
    {
        public SetUndergroundPolicyCommand(int coverPriority, int staffingPriority, int securityPosture)
        {
            CoverPriority = coverPriority;
            StaffingPriority = staffingPriority;
            SecurityPosture = securityPosture;
        }

        public int CoverPriority { get; }
        public int StaffingPriority { get; }
        public int SecurityPosture { get; }
    }
}
