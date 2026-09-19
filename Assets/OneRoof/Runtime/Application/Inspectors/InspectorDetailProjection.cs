using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Application.Inspectors
{
    /// <summary>
    /// Immutable, presentation-safe detail model for a selected tower entity.
    /// The rows deliberately carry plain text so UI can render the same cause data
    /// without retaining mutable simulation records.
    /// </summary>
    public sealed class InspectorDetailProjection
    {
        public InspectorDetailProjection(string title, string symptom, IReadOnlyList<string> details, string suggestedResponse)
        {
            Title = title ?? string.Empty;
            Symptom = symptom ?? string.Empty;
            Details = details != null
                ? new ReadOnlyCollection<string>(new List<string>(details))
                : new ReadOnlyCollection<string>(Array.Empty<string>());
            SuggestedResponse = suggestedResponse ?? string.Empty;
        }

        public string Title { get; }
        public string Symptom { get; }
        public IReadOnlyList<string> Details { get; }
        public string SuggestedResponse { get; }
    }
}
