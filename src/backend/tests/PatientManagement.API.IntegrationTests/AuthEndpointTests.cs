using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PatientManagement.API.Auth;
using PatientManagement.API.Errors;
using PatientManagement.API.IntegrationTests.Support;

namespace PatientManagement.API.IntegrationTests;

public sealed class AuthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string Username = "dev.doctor";
    private const string LoginPath = "/api/v1/auth/login";
    private const string RefreshPath = "/api/v1/auth/refresh-token";
    private const string LogoutPath = "/api/v1/auth/logout";
    private const string ChangePasswordPath = "/api/v1/auth/change-password";

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAccessTokenAndHttpOnlyRefreshCookie()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);

        var response = await Login(client, Username, Password);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.ExpiresInSeconds.Should().Be(15 * 60);

        var cookie = SetCookieHeader(response);
        cookie.Should().Contain("pm_refresh=");
        cookie.Should().ContainEquivalentOf("httponly");
        cookie.Should().ContainEquivalentOf("secure");
        cookie.Should().ContainEquivalentOf("samesite=strict");
        cookie.Should().ContainEquivalentOf("path=/api/v1/auth");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401WithGenericCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);

        var response = await Login(client, Username, "Wrong-Passw0rd");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("invalid_credentials");
        error.Message.Should().Be("Invalid username or password.");
    }

    [Fact]
    public async Task Login_FiveWrongPasswords_LocksTheAccountEvenForTheCorrectPassword()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            (await Login(client, Username, "Wrong-Passw0rd")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var locked = await Login(client, Username, Password);

        locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await locked.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task RefreshToken_RotatedCookieUsedAgain_EndsTheWholeSession()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var first = CookieValue(await Login(client, Username, Password));

        var rotated = await Refresh(client, first);
        rotated.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = CookieValue(rotated);

        // Presenting the first cookie again is reuse: the second cookie of the same session stops working too.
        (await Refresh(client, first)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Refresh(client, second)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithoutAccessToken_Returns401()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);

        var response = await client.PostAsJsonAsync(
            ChangePasswordPath,
            new ChangePasswordRequest(Password, "NewPassw0rd!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_Returns204AndEndsEverySession()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var loginResponse = await Login(client, Username, Password);
        var accessToken = (await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!.AccessToken;
        var refreshCookie = CookieValue(loginResponse);

        using var request = new HttpRequestMessage(HttpMethod.Post, ChangePasswordPath)
        {
            Content = JsonContent.Create(new ChangePasswordRequest(Password, "NewPassw0rd!")),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var changed = await client.SendAsync(request);

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Refresh(client, refreshCookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Login(client, Username, "NewPassw0rd!")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithSession_EndsTheSession()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var refreshCookie = CookieValue(await Login(client, Username, Password));

        using var request = new HttpRequestMessage(HttpMethod.Post, LogoutPath);
        request.Headers.Add("Cookie", $"pm_refresh={refreshCookie}");
        var logout = await client.SendAsync(request);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Refresh(client, refreshCookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> StartAsync(TemporaryDatabase database)
    {
        await database.MigrateAsync();
        await database.SeedDoctorAsync(Password);
        return TestHost.With(factory, "Development", database.ConnectionString).CreateClient();
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync(LoginPath, new LoginRequest(username, password));

    private static Task<HttpResponseMessage> Refresh(HttpClient client, string refreshCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, RefreshPath);
        request.Headers.Add("Cookie", $"pm_refresh={refreshCookie}");
        return client.SendAsync(request);
    }

    private static string SetCookieHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? string.Join(";", values)
            : string.Empty;

    private static string CookieValue(HttpResponseMessage response)
    {
        var refresh = SetCookieHeader(response).Split(';')
            .First(part => part.Trim().StartsWith("pm_refresh=", StringComparison.Ordinal));
        return refresh.Trim()["pm_refresh=".Length..];
    }
}
