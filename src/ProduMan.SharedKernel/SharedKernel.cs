// Kopia z ProduMan.SharedKernel (2026-11-26). Nie zmieniać.
using ProduMan.SharedKernel.Events;
namespace ProduMan.SharedKernel.Common
{
    public sealed record Result(bool IsSuccess, string? Error)
    {
        public static Result Ok() => new(true, null);
        public static Result Fail(string error) => new(false, error);
    }
    public sealed record Result<T>(bool IsSuccess, T? Value, string? Error)
    {
        public static Result<T> Ok(T value) => new(true, value, null);
        public static Result<T> Fail(string error) => new(false, default, error);
    }
    /// <summary>Id — klucz w bazie; PublicId — identyfikator do odwołań z zewnątrz.</summary>
    public abstract class EntityBase
    {
        public int Id { get; protected set; }
        public Guid PublicId { get; protected set; }
        protected EntityBase() { PublicId = Guid.NewGuid(); }
    }
    public abstract class AggregateRoot : EntityBase
    {
        private readonly List<DomainEventBase> _domainEvents = new();
        public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();
        protected void RaiseDomainEvent(DomainEventBase domainEvent) => _domainEvents.Add(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
    public abstract class ValueObject
    {
        protected abstract IEnumerable<object?> GetEqualityComponents();
        public override bool Equals(object? obj)
        {
            if (obj is null || obj.GetType() != GetType()) return false;
            return GetEqualityComponents().SequenceEqual(((ValueObject)obj).GetEqualityComponents());
        }
        public override int GetHashCode()
            => GetEqualityComponents().Aggregate(0, (hash, o) => HashCode.Combine(hash, o));
    }
}
namespace ProduMan.SharedKernel.Events
{
    public abstract record DomainEventBase
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }
}
