using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Events;

namespace OneRoof.Domain.Commands
{
    /// <summary>Validated outcome of a domain command, independent of any concrete command type.</summary>
    public sealed class CommandResult
    {
        private CommandResult(bool accepted, IReadOnlyList<CommandRejectionReason> rejections, IReadOnlyList<DomainEvent> events)
        {
            Accepted = accepted;
            Rejections = rejections;
            Events = events;
        }

        public bool Accepted { get; }

        public IReadOnlyList<CommandRejectionReason> Rejections { get; }

        public IReadOnlyList<DomainEvent> Events { get; }

        public static CommandResult Accept(IEnumerable<DomainEvent> events = null)
        {
            return new CommandResult(true, Empty<CommandRejectionReason>(), Copy(events, nameof(events)));
        }

        public static CommandResult Reject(IEnumerable<CommandRejectionReason> reasons)
        {
            var copiedReasons = Copy(reasons, nameof(reasons));
            if (copiedReasons.Count == 0)
            {
                throw new ArgumentException("Rejected commands must provide at least one reason.", nameof(reasons));
            }

            return new CommandResult(false, copiedReasons, Empty<DomainEvent>());
        }

        private static IReadOnlyList<T> Empty<T>() => new ReadOnlyCollection<T>(Array.Empty<T>());

        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values, string parameterName)
        {
            if (values == null)
            {
                return Empty<T>();
            }

            var copy = new List<T>();
            foreach (var value in values)
            {
                if (ReferenceEquals(value, null))
                {
                    throw new ArgumentException("Result collections cannot contain null values.", parameterName);
                }

                copy.Add(value);
            }

            return new ReadOnlyCollection<T>(copy);
        }
    }
}
