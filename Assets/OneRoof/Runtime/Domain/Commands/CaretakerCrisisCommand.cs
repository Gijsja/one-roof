using OneRoof.Domain.Crisis;

namespace OneRoof.Domain.Commands
{
    public enum CaretakerCrisisAction { Start, Step, Advance, Retry, Exit, Pause, Resume, Practice }

    /// <summary>Validated intent for the Caretaker's separate fixed-step arcade encounter.</summary>
    public sealed class CaretakerCrisisCommand : ICommand
    {
        public CaretakerCrisisCommand(CaretakerCrisisAction action, CaretakerCombatInput input = default)
        { Action = action; Input = input; }
        public CaretakerCrisisAction Action { get; }
        public CaretakerCombatInput Input { get; }
    }
}
