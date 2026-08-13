using System.Buffers.Text;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Alfred.App.Sync;

internal static class PkceAuthenticator
{
    private const string AuthorizeEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    private const int DefaultExpirySeconds = 3600;

    private static readonly Uri TokenEndpoint = new("https://login.microsoftonline.com/common/oauth2/v2.0/token");
    private static readonly HttpClient Http = new();

    public static async Task<TokenSet> AuthorizeAsync(string clientId, IReadOnlyList<string> scopes, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(scopes);

        string codeVerifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var request = new AuthorizationRequest(
            clientId,
            string.Join(' ', scopes),
            $"http://localhost:{FindFreePort()}/",
            Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16)),
            codeVerifier,
            Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier))));

        string authorizationCode = await ReceiveAuthorizationCodeAsync(request, cancellationToken);
        return await ExchangeCodeAsync(request, authorizationCode, cancellationToken);
    }

    public static async Task<TokenSet?> RefreshAsync(string clientId, TokenSet current, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(current);

        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["client_id"] = clientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = current.RefreshToken,
        };

        using HttpResponseMessage response = await PostTokenFormAsync(form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        string payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return CreateTokenSet(payload, current.RefreshToken, current.Account);
    }

    private static async Task<string> ReceiveAuthorizationCodeAsync(AuthorizationRequest request, CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(request.RedirectUri);
        listener.Start();
        OpenBrowser(BuildAuthorizationUrl(request));
        return await WaitForAuthorizationCodeAsync(listener, request.State, cancellationToken);
    }

    private static async Task<string> WaitForAuthorizationCodeAsync(HttpListener listener, string expectedState, CancellationToken cancellationToken)
    {
        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync().WaitAsync(cancellationToken);
            string? error = context.Request.QueryString["error"];
            if (error is not null)
            {
                await WriteBrowserResponseAsync(context.Response, HttpStatusCode.OK, cancellationToken);
                string? description = context.Request.QueryString["error_description"];
                throw new InvalidOperationException($"Authorization failed: {error} {description}");
            }

            string? code = context.Request.QueryString["code"];
            if (code is null)
            {
                await WriteBrowserResponseAsync(context.Response, HttpStatusCode.NotFound, cancellationToken);
                continue;
            }

            await WriteBrowserResponseAsync(context.Response, HttpStatusCode.OK, cancellationToken);

            if (!string.Equals(context.Request.QueryString["state"], expectedState, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Authorization response state does not match the request.");
            }

            return code;
        }
    }

    private static async Task WriteBrowserResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, CancellationToken cancellationToken)
    {
        string message = statusCode == HttpStatusCode.OK ? "You can close this window." : "Not found.";
        byte[] page = Encoding.UTF8.GetBytes($"<!DOCTYPE html><html><body>{message}</body></html>");
        response.StatusCode = (int)statusCode;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = page.Length;
        await response.OutputStream.WriteAsync(page, cancellationToken);
        response.Close();
    }

    private static async Task<TokenSet> ExchangeCodeAsync(AuthorizationRequest request, string authorizationCode, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["client_id"] = request.ClientId,
            ["grant_type"] = "authorization_code",
            ["code"] = authorizationCode,
            ["redirect_uri"] = request.RedirectUri,
            ["code_verifier"] = request.CodeVerifier,
            ["scope"] = request.Scope,
        };

        using HttpResponseMessage response = await PostTokenFormAsync(form, cancellationToken);
        string payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Token request failed with {(int)response.StatusCode}: {payload}");
        }

        return CreateTokenSet(payload, fallbackRefreshToken: null, fallbackAccount: null);
    }

    private static async Task<HttpResponseMessage> PostTokenFormAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(form);
        return await Http.PostAsync(TokenEndpoint, content, cancellationToken);
    }

    private static TokenSet CreateTokenSet(string payload, string? fallbackRefreshToken, string? fallbackAccount)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("access_token", out JsonElement accessTokenElement) || accessTokenElement.GetString() is not string accessToken)
        {
            throw new InvalidOperationException("Token response is missing access_token.");
        }

        string? refreshToken = root.TryGetProperty("refresh_token", out JsonElement refreshTokenElement)
            ? refreshTokenElement.GetString()
            : null;
        refreshToken ??= fallbackRefreshToken;
        if (refreshToken is null)
        {
            throw new InvalidOperationException("Token response is missing refresh_token. Request the offline_access scope.");
        }

        int expiresInSeconds = root.TryGetProperty("expires_in", out JsonElement expiresElement) && expiresElement.ValueKind == JsonValueKind.Number
            ? expiresElement.GetInt32()
            : DefaultExpirySeconds;

        string account = ReadAccount(root) ?? fallbackAccount ?? string.Empty;
        return new TokenSet(accessToken, refreshToken, DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds), account);
    }

    private static string? ReadAccount(JsonElement root)
    {
        if (!root.TryGetProperty("id_token", out JsonElement idTokenElement) || idTokenElement.GetString() is not string idToken)
        {
            return null;
        }

        string[] segments = idToken.Split('.');
        if (segments.Length < 2)
        {
            return null;
        }

        try
        {
            byte[] claimsBytes = Base64Url.DecodeFromChars(segments[1]);
            using JsonDocument claims = JsonDocument.Parse(claimsBytes);
            return ReadClaim(claims.RootElement, "preferred_username")
                ?? ReadClaim(claims.RootElement, "email")
                ?? ReadClaim(claims.RootElement, "name");
        }
        catch (Exception failure) when (failure is FormatException or JsonException)
        {
            return null;
        }
    }

    private static string? ReadClaim(JsonElement claims, string claimName)
        => claims.TryGetProperty(claimName, out JsonElement claim) && claim.ValueKind == JsonValueKind.String
            ? claim.GetString()
            : null;

    private static string BuildAuthorizationUrl(AuthorizationRequest request)
    {
        return AuthorizeEndpoint
            + "?client_id=" + Uri.EscapeDataString(request.ClientId)
            + "&response_type=code&response_mode=query"
            + "&redirect_uri=" + Uri.EscapeDataString(request.RedirectUri)
            + "&scope=" + Uri.EscapeDataString(request.Scope)
            + "&state=" + Uri.EscapeDataString(request.State)
            + "&code_challenge=" + Uri.EscapeDataString(request.CodeChallenge)
            + "&code_challenge_method=S256";
    }

    private static void OpenBrowser(string authorizationUrl)
    {
        var startInfo = new ProcessStartInfo(authorizationUrl) { UseShellExecute = true };
        using Process? browser = Process.Start(startInfo);
    }

    private static int FindFreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private sealed record AuthorizationRequest(
        string ClientId,
        string Scope,
        string RedirectUri,
        string State,
        string CodeVerifier,
        string CodeChallenge);
}
