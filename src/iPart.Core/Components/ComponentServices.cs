namespace iPart.Core.Components;

public interface IComponentService
{
    Task<Component?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    // null expectedVersion creates; updates require the version returned by GetAsync/SaveAsync.
    Task<ComponentSaveResult> SaveAsync(Component component, Guid? expectedVersion, CancellationToken cancellationToken = default);
}

public enum SaveFailure { None, Validation, DuplicateBusinessKey, ConcurrentEdit, InvalidReference }
public sealed record ComponentSaveResult(bool Success, Guid ComponentId, Guid? Version, SaveFailure Failure, string? Message);
