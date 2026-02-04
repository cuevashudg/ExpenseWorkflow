namespace Workflow.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain that other parts of the system should know about.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}
