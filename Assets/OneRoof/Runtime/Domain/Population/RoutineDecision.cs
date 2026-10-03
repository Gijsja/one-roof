using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Population
{
    public enum RoutineDecisionKind { None, LocalActivity, Travel }

    /// <summary>An explicit activity intent; absence of travel is not evidence of attendance.</summary>
    public readonly struct RoutineDecision
    {
        public RoutineDecision(RoutineDecisionKind kind, TripPurpose? purpose, bool isRequired, long episodeStartTick,
            long appointmentStartTick = -1, long appointmentEndTick = -1, long anticipatedDepartureTick = -1)
        {
            Kind = kind;
            Purpose = purpose;
            IsRequired = isRequired;
            EpisodeStartTick = episodeStartTick;
            AppointmentStartTick = appointmentStartTick;
            AppointmentEndTick = appointmentEndTick;
            AnticipatedDepartureTick = anticipatedDepartureTick;
        }

        public RoutineDecisionKind Kind { get; }
        public TripPurpose? Purpose { get; }
        public bool IsRequired { get; }
        public bool IsLocal => Kind == RoutineDecisionKind.LocalActivity;
        public long EpisodeStartTick { get; }
        public long AppointmentStartTick { get; }
        public long AppointmentEndTick { get; }
        public long AnticipatedDepartureTick { get; }
    }
}
