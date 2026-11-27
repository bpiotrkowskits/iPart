using System.Net;
using System.Text;
using iPart.Core.Shared;
using iPart.Infrastructure.Identity;

namespace iPart.Tests;

public sealed class LoginApiClientTests
{
    private static readonly Guid AccountId = Guid.Parse("8a1f6d2c-3b4e-4f5a-9c6d-7e8f9a0b1c2d");

    [Fact]
    public async Task SuccessReadsOnlyNeededFieldsAndSendsBasicHeaderWithoutBody()
    {
        var handler = new Handler(HttpStatusCode.OK, $$$"""
            {"status":1,"statusDetails":null,"user":{"id":"{{{AccountId}}}","login":"utest","nazwa":"Testowy Użytkownik","imie":"Użytkownik","nazwisko":"Testowy",
             "kontakt":"u@firma.test","departmentId":100102,"token":"SEKRET-TOKEN","pin":"1234","rfid":"ABC"}}
            """);
        var result = await Client(handler).LoginAsync(" utest ", "tajne:hasło");

        Assert.Equal(LoginOutcome.Success, result.Outcome);
        Assert.Equal(new LoginAccount(AccountId, "utest", "Testowy Użytkownik", "u@firma.test", 100102), result.Account);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://login.test/api/login/", handler.Uri);
        Assert.Equal("Basic", handler.Scheme);
        Assert.Equal("utest:tajne:hasło", Encoding.UTF8.GetString(Convert.FromBase64String(handler.Parameter!)));
        Assert.Null(handler.Body);
    }

    [Fact]
    public async Task UnauthorizedIsWrongCredentialsAndServiceTextIsNotExposed()
    {
        var result = await Client(new Handler(HttpStatusCode.Unauthorized, """{"status":0,"statusDetails":"System.Exception: stack trace"}""")).LoginAsync("x", "y");
        Assert.Equal(LoginOutcome.WrongCredentials, result.Outcome);
        Assert.Equal("Błędny login lub hasło.", result.Message);
    }

    [Theory]
    [InlineData("""{"status":3,"user":{"id":"8a1f6d2c-3b4e-4f5a-9c6d-7e8f9a0b1c2d"}}""", LoginOutcome.Refused)]   // konto nieaktywne
    [InlineData("""{"status":2,"user":null}""", LoginOutcome.Refused)]
    [InlineData("""{"status":1,"user":{"id":"00000000-0000-0000-0000-000000000000"}}""", LoginOutcome.Refused)]
    [InlineData("""{"status":1,"user":null}""", LoginOutcome.Refused)]
    [InlineData("""{"status":0,"user":null}""", LoginOutcome.WrongCredentials)]
    [InlineData("""nie-json""", LoginOutcome.Unavailable)]
    public async Task OnlyStatusOneWithAccountIdIsSuccess(string json, LoginOutcome expected)
    {
        var result = await Client(new Handler(HttpStatusCode.OK, json)).LoginAsync("x", "y");
        Assert.Equal(expected, result.Outcome);
        Assert.Null(result.Account);
    }

    [Fact]
    public async Task ServerErrorAndNetworkFailureAreUnavailable()
    {
        Assert.Equal(LoginOutcome.Unavailable, (await Client(new Handler(HttpStatusCode.InternalServerError, "")).LoginAsync("x", "y")).Outcome);
        Assert.Equal(LoginOutcome.Unavailable, (await Client(new Handler(null, "")).LoginAsync("x", "y")).Outcome);
    }

    [Fact]
    public async Task EmptyLoginOrPasswordIsNotSent()
    {
        var handler = new Handler(HttpStatusCode.OK, "{}");
        Assert.Equal(LoginOutcome.WrongCredentials, (await Client(handler).LoginAsync(" ", "y")).Outcome);
        Assert.Equal(LoginOutcome.WrongCredentials, (await Client(handler).LoginAsync("x", "")).Outcome);
        Assert.Null(handler.Method);
    }

    private static LoginApiClient Client(Handler handler) => new(new HttpClient(handler) { BaseAddress = new Uri("https://login.test/api/") });

    private sealed class Handler(HttpStatusCode? status, string body) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? Uri { get; private set; }
        public string? Scheme { get; private set; }
        public string? Parameter { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri?.ToString();
            Scheme = request.Headers.Authorization?.Scheme;
            Parameter = request.Headers.Authorization?.Parameter;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (status is null) throw new HttpRequestException("brak sieci");
            return new HttpResponseMessage(status.Value) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
