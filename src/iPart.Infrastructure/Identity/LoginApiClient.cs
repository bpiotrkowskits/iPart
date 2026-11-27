using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using iPart.Core.Shared;

namespace iPart.Infrastructure.Identity;

/// <summary>
/// Logowanie kontem Nexo przez firmową usługę LoginApi (ta sama co w ProduMan; ADR-0011).
/// <c>POST login/</c> z nagłówkiem <c>Authorization: Basic base64(login:hasło)</c>, bez treści.
/// Udane tylko przy HTTP 200, <c>status == 1</c> i niepustym <c>user.id</c>.
/// Bezpieczeństwo: odczytujemy wyłącznie potrzebne pola (odpowiedź zawiera też dane, których nie wolno zapisywać),
/// nie logujemy treści odpowiedzi, hasło istnieje tylko na czas wywołania, token z odpowiedzi jest pomijany.
/// </summary>
/// <param name="http">Klient z <c>BaseAddress</c> zakończonym ukośnikiem, np. <c>https://login.api.ts/api/</c>.</param>
public sealed class LoginApiClient(HttpClient http) : ILoginService
{
    private const int SuccessStatus = 1;
    private const int WrongPasswordStatus = 0;

    public async Task<LoginResult> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password)) return LoginResult.WrongCredentials;
        try
        {
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login.Trim()}:{password}"));
            using var request = new HttpRequestMessage(HttpMethod.Post, "login/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            using var response = await http.SendAsync(request, cancellationToken);
            // Błędne hasło: 401, a w statusDetails tekst wyjątku — nie czytamy go i nie pokazujemy.
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginResult.WrongCredentials;
            if (response.StatusCode != HttpStatusCode.OK) return LoginResult.Unavailable;
            var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
            if (body is { Status: SuccessStatus, User: { } user } && user.Id != Guid.Empty)
                return LoginResult.Success(new LoginAccount(user.Id, user.Login?.Trim() is { Length: > 0 } name ? name : login.Trim(),
                    Clean(user.Nazwa) ?? Clean($"{user.Imie} {user.Nazwisko}"), Clean(user.Kontakt), user.DepartmentId));
            // Status 0 przy HTTP 200 — błędne hasło; 2 — brak dostępu; 3 — błąd, m.in. konto nieaktywne.
            return body?.Status == WrongPasswordStatus ? LoginResult.WrongCredentials : LoginResult.Refused;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
            or System.Text.Json.JsonException or NotSupportedException)
        {
            if (cancellationToken.IsCancellationRequested) throw;
            return LoginResult.Unavailable;
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Tylko potrzebne pola; pozostałe (token, dane uwierzytelniające, identyfikatory stanowiskowe) są pomijane przy odczycie.
    private sealed record LoginUser(Guid Id, string? Login, string? Nazwa, string? Imie, string? Nazwisko, string? Kontakt, int? DepartmentId);
    private sealed record LoginResponse(int Status, LoginUser? User);
}
