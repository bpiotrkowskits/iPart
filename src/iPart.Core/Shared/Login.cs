namespace iPart.Core.Shared;

/// <summary>Wynik logowania kontem Nexo. Hasło nie jest nigdzie przechowywane.</summary>
public enum LoginOutcome
{
    Success,
    /// <summary>Błędny login lub hasło.</summary>
    WrongCredentials,
    /// <summary>Konto bez dostępu albo nieaktywne — odmowa.</summary>
    Refused,
    /// <summary>Usługa logowania niedostępna albo zwróciła błąd.</summary>
    Unavailable,
}

/// <summary>Dane konta potrzebne iPart — tylko te pola odczytujemy z odpowiedzi usługi logowania.</summary>
public sealed record LoginAccount(Guid Id, string Login, string? DisplayName, string? Email, int? DepartmentId);

public sealed record LoginResult(LoginOutcome Outcome, LoginAccount? Account)
{
    public static LoginResult Success(LoginAccount account) => new(LoginOutcome.Success, account);
    public static readonly LoginResult WrongCredentials = new(LoginOutcome.WrongCredentials, null);
    public static readonly LoginResult Refused = new(LoginOutcome.Refused, null);
    public static readonly LoginResult Unavailable = new(LoginOutcome.Unavailable, null);

    /// <summary>Komunikat dla użytkownika — nigdy treść błędu z usługi logowania.</summary>
    public string Message => Outcome switch
    {
        LoginOutcome.Success => "Zalogowano.",
        LoginOutcome.WrongCredentials => "Błędny login lub hasło.",
        LoginOutcome.Refused => "Konto nie ma dostępu albo jest nieaktywne.",
        _ => "Usługa logowania jest niedostępna. Spróbuj ponownie później.",
    };
}

/// <summary>Sprawdzenie loginu i hasła konta Nexo (LoginApi). Wywołuje je wyłącznie serwer iPart.</summary>
public interface ILoginService
{
    Task<LoginResult> LoginAsync(string login, string password, CancellationToken cancellationToken = default);
}
