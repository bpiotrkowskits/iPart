namespace iPart.Core.Shared;

/// <summary>
/// Konto Nexo z listy kont ProduMana — kopia 7 kolumn widoku <c>produman.users</c> na czas pilotażu (ADR-0014),
/// w tabeli o tej samej nazwie i kolumnach; po pilotażu zastąpi ją odczyt z widoku, bez zmian w zapytaniach.
/// Tylko do odczytu. <see cref="UserId"/> to Guid konta — identyfikator osoby w iPart.
/// Bez identyfikatorów stanowiskowych (PIN, RFID, QR) — nigdy ich nie kopiujemy.
/// </summary>
public sealed class ProduManAccount
{
    public Guid UserId { get; set; }
    public string? Login { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    /// <summary>Dział z Nexo — może mieć numer spoza słownika działów (np. 100110 „Brak”).</summary>
    public int? Department { get; set; }
    public bool IsActive { get; set; }
    public bool Deleted { get; set; }

    /// <summary>Na listę wyboru trafiają tylko konta aktywne i nieusunięte.</summary>
    public bool Selectable => IsActive && !Deleted;
}

/// <summary>
/// Dział ze wspólnego słownika działów ProduMana — kopia <c>employees.Departments</c> na czas pilotażu (ADR-0014).
/// Działów się nie usuwa, tylko wyłącza. Nieznany numer pokazujemy wprost: „dział 100110 (nieznany)”.
/// </summary>
public sealed class Department
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    public static string Describe(int? departmentId, IReadOnlyDictionary<int, Department> dictionary) =>
        departmentId is not { } id ? "bez działu"
        : dictionary.TryGetValue(id, out var department) ? department.Name
        : $"dział {id} (nieznany)";
}

/// <summary>
/// Uprawnienia konta (ADR-0013): mapa klucz → poziom i zakres w JSON. Brak wpisu = brak dostępu.
/// Zmiany zapisuje historia obszaru; <see cref="Version"/> chroni przed nadpisaniem cudzej zmiany.
/// </summary>
public sealed class UserAccess
{
    public Guid UserId { get; set; }
    public string PermissionsJson { get; set; } = "{}";
    public Guid Version { get; set; } = Guid.NewGuid();
    public Guid? ChangedByUserId { get; set; }
    public string ChangedBy { get; set; } = "";
    public DateTimeOffset ChangedAtUtc { get; set; }

    public AccountPermissions Permissions => AccountPermissions.FromJson(PermissionsJson);
}

/// <summary>Grupa: dział albo etap pracy. Służy do zastępstw, administratorów i zadań bez operatora — nie daje dostępów.</summary>
public sealed class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class GroupMember
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Administrator grupy — cecha członkostwa, nie rola (9.8).</summary>
    public bool IsAdministrator { get; set; }
}

/// <summary>Grupy na start (10.7).</summary>
public static class GroupCodes
{
    public const string ProjectService = "OBSLUGA_PROJEKTU";
    public const string SmtTechnology = "TECHNOLOGIA_SMT";
    public const string Purchasing = "ZAKUPY";
    public const string Warehouse = "MAGAZYN";
    public const string Production = "PRODUKCJA";
    public const string CustomerAccountManagers = "OPIEKUNOWIE_KLIENTA";
}

/// <summary>Bieżący użytkownik operacji. Na serwerze — z uwierzytelnienia żądania.</summary>
public interface ICurrentUser
{
    /// <summary>Nazwa konta z chwili zapisu — zapisywana w historii obok Guida (ADR-0037 ProduMan, pkt 7).</summary>
    string GetAuthor();
    /// <summary>Guid konta Nexo, jeśli jest znany (na serwerze — zawsze po zalogowaniu).</summary>
    Guid? UserId => null;
}
