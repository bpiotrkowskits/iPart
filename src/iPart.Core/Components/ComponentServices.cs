namespace iPart.Core.Components;

public interface ICurrentUser
{
    string GetAuthor();
}

public interface IComponentService
{
    Task<Component?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    // null expectedVersion creates; updates require the version returned by GetAsync/SaveAsync.
    Task<ComponentSaveResult> SaveAsync(Component component, Guid? expectedVersion, CancellationToken cancellationToken = default);
}

public enum SaveFailure { None, Validation, DuplicateBusinessKey, ConcurrentEdit, InvalidReference }
public sealed record ComponentSaveResult(bool Success, Guid ComponentId, Guid? Version, SaveFailure Failure, string? Message);

public enum HistoryOperation { Created = 1, Updated = 2 }

public sealed class ComponentHistoryEntry
{
    public long Id { get; set; }
    public Guid OperationId { get; set; }
    public Guid ComponentId { get; set; }
    public string Author { get; set; } = "";
    public DateTimeOffset OccurredAtUtc { get; set; }
    public HistoryOperation Operation { get; set; }
    public string FieldPath { get; set; } = "";
    // SQL NULL represents absent data; non-null scalar values use invariant JSON encoding.
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
}
