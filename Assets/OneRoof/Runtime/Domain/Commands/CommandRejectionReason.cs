using System;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Commands
{
    /// <summary>Stable, presentation-neutral explanation for a rejected command.</summary>
    public sealed class CommandRejectionReason : IEquatable<CommandRejectionReason>
    {
        public CommandRejectionReason(ContentId code, string detail = null)
        {
            code.EnsureValid();
            Code = code;
            Detail = detail ?? string.Empty;
        }

        public ContentId Code { get; }

        public string Detail { get; }

        public string Message => string.IsNullOrEmpty(Detail) ? Code.Value : Detail;

        public bool Equals(CommandRejectionReason other) => other != null && Code == other.Code && StringComparer.Ordinal.Equals(Detail, other.Detail);

        public override bool Equals(object obj) => Equals(obj as CommandRejectionReason);

        public override int GetHashCode() => HashCode.Combine(Code, Detail);
    }
}
