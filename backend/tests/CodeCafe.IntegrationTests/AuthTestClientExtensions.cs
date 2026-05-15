using CodeCafe.Contracts.Auth;

namespace CodeCafe.IntegrationTests;


internal static class AuthTestClientExtensions
{
    public static async Task LoginAsync(this HttpClient client, string username = "test-user", string password = "test-password")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
