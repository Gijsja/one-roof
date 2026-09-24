using OneRoof.Application.Population;
using OneRoof.Application.Transit;
using OneRoof.Domain.Population;

namespace OneRoof.Presentation.Population
{
    /// <summary>Short, reusable labels derived from read-only resident projections.</summary>
    public static class ResidentActivityCaption
    {
        private static readonly string[] ToFloor = BuildFloorLabels("To floor ");
        private static readonly string[] LiftToFloor = BuildFloorLabels("Lift to F");
        private static readonly string[] WaitingForFloor = BuildFloorLabels("Waiting · F");
        private static readonly string[] LateLiftForFloor = BuildFloorLabels("Lift's late · F");

        public static string For(in NpcProjection resident)
        {
            if (resident.IsInTransit)
            {
                if (resident.WaitTicks >= 30)
                    return resident.DestinationFloor.HasValue
                        ? FloorLabel(LateLiftForFloor, "Lift's late · F", resident.DestinationFloor.Value)
                        : "Lift's late";
                if (resident.WaitTicks >= 5)
                    return resident.DestinationFloor.HasValue
                        ? FloorLabel(WaitingForFloor, "Waiting · F", resident.DestinationFloor.Value)
                        : "Waiting for lift";
                return resident.DestinationFloor.HasValue
                    ? FloorLabel(ToFloor, "To floor ", resident.DestinationFloor.Value)
                    : "In transit";
            }

            switch (resident.CurrentActivity)
            {
                case NpcActivityKind.Sleeping: return "Sleeping";
                case NpcActivityKind.Working: return "At work";
                case NpcActivityKind.Eating: return "Eating";
                case NpcActivityKind.Leisure: return "Relaxing";
                default: return "At home";
            }
        }

        public static string For(in TransitResidentProjection resident)
        {
            switch (resident.Status)
            {
                case TransitResidentStatus.Queued:
                    return resident.WaitTicks >= 30
                        ? FloorLabel(LateLiftForFloor, "Lift's late · F", resident.DestinationFloor)
                        : FloorLabel(WaitingForFloor, "Waiting · F", resident.DestinationFloor);
                case TransitResidentStatus.Riding:
                    return FloorLabel(LiftToFloor, "Lift to F", resident.DestinationFloor);
                case TransitResidentStatus.Walking:
                    return WalkingPurpose(resident.PurposeLabel);
                case TransitResidentStatus.Outside: return "Outside";
            }

            if (!string.IsNullOrEmpty(resident.PurposeLabel))
                return resident.PurposeLabel;

            switch (resident.Activity)
            {
                case ActivityKind.Sleeping: return "Sleeping";
                case ActivityKind.Working: return "At work";
                case ActivityKind.Eating: return "Eating";
                case ActivityKind.Leisure: return "Relaxing";
                case ActivityKind.Commuting: return "Commuting";
                default: return "At home";
            }
        }

        private static string WalkingPurpose(string purpose)
        {
            switch (purpose)
            {
                case "Eating at diner": return "To diner";
                case "Working": return "To work";
                case "Working outside": return "To outside work";
                case "Sleeping":
                case "Sitting":
                case "Reading":
                case "Learning":
                case "Chilling": return "Going home";
                case "Returning home": return "Going home";
                case "Socializing": return "To visit";
                case "Freshening up": return "To wash";
                default: return "Walking";
            }
        }

        private static string[] BuildFloorLabels(string prefix)
        {
            var labels = new string[101];
            for (var floor = 0; floor < labels.Length; floor++)
                labels[floor] = prefix + floor;
            return labels;
        }

        private static string FloorLabel(string[] labels, string prefix, int floor)
        {
            return floor >= 0 && floor < labels.Length ? labels[floor] : prefix + floor;
        }
    }
}
