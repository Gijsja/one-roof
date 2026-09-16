using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Application.Inspectors
{
    /// <summary>
    /// Projection used by the Inspector to display symptom, causes, and direct routes
    /// to build/policy response (Docs/04_UX_CONTRACT.md - Explanation chain).
    /// </summary>
    public sealed class ElevatorCongestionInspectorProjection
    {
        public ElevatorCongestionInspectorProjection(
            int floorLevel,
            string title,
            string symptomDescription,
            IReadOnlyList<string> contributingCauses,
            string suggestedResponseAction,
            bool canDirectRouteToBuild = true,
            string targetBuildTool = "transit:elevator_car")
        {
            FloorLevel = floorLevel;
            Title = title ?? string.Empty;
            SymptomDescription = symptomDescription ?? string.Empty;
            ContributingCauses = contributingCauses != null
                ? new ReadOnlyCollection<string>(new List<string>(contributingCauses))
                : new ReadOnlyCollection<string>(Array.Empty<string>());
            SuggestedResponseAction = suggestedResponseAction ?? string.Empty;
            CanDirectRouteToBuild = canDirectRouteToBuild;
            TargetBuildTool = targetBuildTool ?? string.Empty;
        }

        public int FloorLevel { get; }

        public string Title { get; }

        public string SymptomDescription { get; }

        public IReadOnlyList<string> ContributingCauses { get; }

        public string SuggestedResponseAction { get; }

        public bool CanDirectRouteToBuild { get; }

        public string TargetBuildTool { get; }
    }
}
