namespace iPart.Core.Shared;

/// <summary>
/// Konto wybrane przy starcie programu (pilotaż, ADR-0014: wybór z listy, bez hasła — tożsamość umowna).
/// Po pilotażu to samo miejsce wypełni logowanie przez LoginApi.
/// </summary>
public sealed class Session : ICurrentUser
{
    public Guid? AccountId { get; private set; }
    public string AccountName { get; private set; } = "";
    public bool IsSignedIn => AccountId is not null;

    public void SignIn(ProduManAccount account)
    {
        AccountId = account.UserId;
        AccountName = string.IsNullOrWhiteSpace(account.FullName) ? account.Login ?? account.UserId.ToString() : account.FullName.Trim();
    }

    public string GetAuthor() => IsSignedIn ? AccountName : Environment.UserName;
    public Guid? UserId => AccountId;
}

/// <summary>Odczyt kopii listy kont i słownika działów ProduMana (tylko do odczytu).</summary>
public interface IAccountDirectory
{
    /// <summary>Konta do wyboru: aktywne i nieusunięte, posortowane po nazwie.</summary>
    Task<IReadOnlyList<ProduManAccount>> ListSelectableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, Department>> DepartmentsAsync(CancellationToken cancellationToken = default);
}
