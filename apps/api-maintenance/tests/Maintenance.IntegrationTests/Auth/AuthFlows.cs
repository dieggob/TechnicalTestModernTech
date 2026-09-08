using System.Net.Http.Json;
using System.Web;
using Maintenance.Infrastructure.Email;

namespace Maintenance.IntegrationTests.Auth;

/// <summary>Shared steps for auth journeys: register, read emailed tokens, log in.</summary>
internal static class AuthFlows
{
    public const string RegisterUrl = "/api/v1/auth/register";
    public const string VerifyUrl = "/api/v1/auth/verify-email";
    public const string ResendUrl = "/api/v1/auth/resend-verification";
    public const string LoginUrl = "/api/v1/auth/login";
    public const string DevEmailsUrl = "/api/v1/dev/emails";
    public const string DefaultPassword = "Secret123";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password = DefaultPassword) =>
        client.PostAsJsonAsync(RegisterUrl, new { email, password });

    public static Task<HttpResponseMessage> VerifyAsync(this HttpClient client, string token) =>
        client.PostAsJsonAsync(VerifyUrl, new { token });

    public static Task<HttpResponseMessage> ResendVerificationAsync(this HttpClient client, string email) =>
        client.PostAsJsonAsync(ResendUrl, new { email });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = DefaultPassword) =>
        client.PostAsJsonAsync(LoginUrl, new { email, password });

    /// <summary>Registers (when needed) and logs in, returning the session token.</summary>
    public static async Task<string> LoginTokenAsync(this HttpClient client, string email, string password = DefaultPassword)
    {
        await client.RegisterAsync(email, password);
        var response = await client.LoginAsync(email, password);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Maintenance.Application.Auth.AuthResult>())!.Token;
    }

    public static HttpClient WithBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    /// <summary>The token inside the newest recorded email sent to <paramref name="email"/>.</summary>
    public static async Task<string> LatestTokenAsync(this HttpClient client, string email)
    {
        var emails = await client.GetFromJsonAsync<List<RecordedEmail>>(DevEmailsUrl);
        var latest = emails!.First(e => string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase));
        var query = HttpUtility.ParseQueryString(new Uri(latest.Link).Query);
        return query["token"] ?? throw new InvalidOperationException("Email link has no token.");
    }
}
