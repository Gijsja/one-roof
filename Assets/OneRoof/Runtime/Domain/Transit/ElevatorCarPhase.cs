namespace OneRoof.Domain.Transit
{
    public enum ElevatorCarPhase
    {
        Idle,
        Moving,
        DoorsOpening,
        OpenLoading,
        DoorsClosing
    }

    public enum ElevatorDirection
    {
        None,
        Up,
        Down
    }
}
