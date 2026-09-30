using OneRoof.Application.Population;
using OneRoof.Application.Transit;
using OneRoof.Domain.Population;

namespace OneRoof.Presentation.Population
{
    public enum ResidentBubbleKind { Action, Thought }
    public enum ResidentBubbleEmotion { Neutral, Connected, Flirty, Embarrassed, Angry, Impatient }

    /// <summary>Presentation cue derived from a projected routine or actual social memory.</summary>
    public readonly struct ResidentBubbleCue
    {
        public ResidentBubbleCue(string text, ResidentBubbleKind kind = ResidentBubbleKind.Action,
            ResidentBubbleEmotion emotion = ResidentBubbleEmotion.Neutral, int priority = 10)
        {
            Text = text;
            Kind = kind;
            Emotion = emotion;
            Priority = priority;
        }

        public string Text { get; }
        public ResidentBubbleKind Kind { get; }
        public ResidentBubbleEmotion Emotion { get; }
        public int Priority { get; }
    }

    /// <summary>Short, reusable labels derived from read-only resident projections.</summary>
    public static class ResidentActivityCaption
    {
        public const int MaximumBubbleCharacters = 38;
        private static readonly string[] ToFloor = BuildFloorLabels("To floor ");
        private static readonly string[] LiftToFloor = BuildFloorLabels("Lift to F");
        private static readonly string[] WaitingForFloor = BuildFloorLabels("Waiting · F");
        private static readonly string[] LateLiftForFloor = BuildFloorLabels("Lift's late · F");

        /// <summary>
        /// Urgent transit symptoms outrank memories; sleep retains its current action.
        /// Social emotion describes the recorded encounter, never a relationship outcome.
        /// </summary>
        public static ResidentBubbleCue Resolve(in TransitResidentProjection resident)
        {
            if (resident.Status == TransitResidentStatus.Queued && resident.WaitTicks >= 30)
                return new ResidentBubbleCue(For(resident), ResidentBubbleKind.Thought,
                    ResidentBubbleEmotion.Impatient, 100);

            if (resident.Activity == ActivityKind.Sleeping || resident.PurposeLabel == "Sleeping")
                return new ResidentBubbleCue(For(resident));

            var thought = resident.RecentThought;
            if (!string.IsNullOrWhiteSpace(thought))
            {
                if (thought.StartsWith("Flirted with resident ", System.StringComparison.Ordinal))
                    return new ResidentBubbleCue("Thinking of that flirt", ResidentBubbleKind.Thought,
                        ResidentBubbleEmotion.Flirty, 50);
                if (thought.StartsWith("Had deep conversation with resident ", System.StringComparison.Ordinal))
                    return new ResidentBubbleCue("A meaningful conversation", ResidentBubbleKind.Thought,
                        ResidentBubbleEmotion.Connected, 50);
                if (thought.StartsWith("Awkward encounter with resident ", System.StringComparison.Ordinal))
                    return new ResidentBubbleCue("That was awkward", ResidentBubbleKind.Thought,
                        ResidentBubbleEmotion.Embarrassed, 60);
                if (thought.StartsWith("Heated argument with resident ", System.StringComparison.Ordinal))
                    return new ResidentBubbleCue("Still thinking of that argument", ResidentBubbleKind.Thought,
                        ResidentBubbleEmotion.Angry, 70);
                return new ResidentBubbleCue(Fit(thought), ResidentBubbleKind.Thought,
                    ResidentBubbleEmotion.Neutral, 40);
            }

            return new ResidentBubbleCue(Fit(For(resident)));
        }

        /// <summary>Bounds arbitrary projected text at a word boundary.</summary>
        public static string Fit(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.Trim();
            if (text.Length <= MaximumBubbleCharacters) return text;
            var cut = text.LastIndexOf(' ', MaximumBubbleCharacters - 2, MaximumBubbleCharacters - 1);
            if (cut < MaximumBubbleCharacters / 2) cut = MaximumBubbleCharacters - 1;
            return text.Substring(0, cut).TrimEnd() + "…";
        }

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
