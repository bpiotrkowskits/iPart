namespace iPart.Core.Shared;

/// <summary>
/// Zestaw zmian — jeden zapis użytkownika: „Zapisz zmiany”, import, zatwierdzenie albo decyzja (ADR-0007).
/// Każdy obszar ma własne tabele historii w swoim schemacie, o tej samej budowie.
/// Historii nie edytujemy ani nie usuwamy z aplikacji.
/// </summary>
public abstract class ChangeSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Użytkownik iPart; puste dla wpisów sprzed wprowadzenia użytkowników (np. przeniesionych z 0.1.0).</summary>
    public Guid? UserId { get; set; }
    /// <summary>Konto w chwili zapisu — czytelne także po zmianie danych użytkownika.</summary>
    public string Author { get; set; } = "";
    public DateTimeOffset OccurredAtUtc { get; set; }
    /// <summary>Czego dotyczy zapis, np. <c>Component</c>, <c>Package</c>.</summary>
    public string SubjectType { get; set; } = "";
    public Guid SubjectId { get; set; }
    /// <summary>Rodzaj operacji, np. <see cref="ChangeOperations.Created"/>.</summary>
    public string Operation { get; set; } = "";
    public string? Comment { get; set; }
}

/// <summary>Zmiana jednego pola w zestawie zmian: obiekt, pole, było → będzie.</summary>
public abstract class FieldChange
{
    public long Id { get; set; }
    public Guid ChangeSetId { get; set; }
    public string ObjectType { get; set; } = "";
    public Guid ObjectId { get; set; }
    public string Field { get; set; } = "";
    /// <summary>NULL oznacza brak danych; wartości skalarne w niezmiennym zapisie JSON.</summary>
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
}

public static class ChangeOperations
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Imported = "Imported";
    public const string Confirmed = "Confirmed";
}

// Konkretne tabele historii w schematach obszarów. Ta sama budowa, osobne miejsce (ADR-0005, ADR-0007).
public sealed class ComponentsChangeSet : ChangeSet { public List<ComponentsFieldChange> Changes { get; set; } = []; }
public sealed class ComponentsFieldChange : FieldChange;
public sealed class DocumentationChangeSet : ChangeSet { public List<DocumentationFieldChange> Changes { get; set; } = []; }
public sealed class DocumentationFieldChange : FieldChange;
public sealed class SharedChangeSet : ChangeSet { public List<SharedFieldChange> Changes { get; set; } = []; }
public sealed class SharedFieldChange : FieldChange;
