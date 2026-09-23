using System;
using OneRoof.Domain.Economy;

namespace OneRoof.Domain.Commands
{
    /// <summary>Command to replace the tower's current economy policy decree settings.</summary>
    public sealed class SetPolicyDecreeCommand : ICommand
    {
        public SetPolicyDecreeCommand(PolicyDecreeState policy)
        {
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public PolicyDecreeState Policy { get; }

        public override string ToString() => "SetPolicyDecree";
    }
}
