using System.Net.Http;

namespace Alfred.App.Sync;

internal sealed class MicrosoftAccount
{
    private static readonly string[] Scopes = ["Calendars.ReadWrite", "offline_access", "openid", "profile"];
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);

    private TokenSet? _tokens = TokenStore.Load();

    public string? AccountName => _tokens?.Account;

    public bool IsSignedIn => _tokens is not null;

    public async Task SignInAsync(string clientId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        TokenSet tokens = await PkceAuthenticator.AuthorizeAsync(clientId, Scopes, cancellationToken);
        TokenStore.Save(tokens);
        _tokens = tokens;
    }

    public void SignOut()
    {
        _tokens = null;
        TokenStore.Clear();
    }

    public async Task<string?> GetAccessTokenAsync(string clientId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        TokenSet? tokens = _tokens;
        if (tokens is null)
        {
            return null;
        }

        if (tokens.ExpiresUtc - DateTimeOffset.UtcNow > RefreshMargin)
        {
            return tokens.AccessToken;
        }

        TokenSet? refreshed = await TryRefreshAsync(clientId, tokens, cancellationToken);
        if (refreshed is null)
        {
            return null;
        }

        TokenStore.Save(refreshed);
        _tokens = refreshed;
        return refreshed.AccessToken;
    }

    private static async Task<TokenSet?> TryRefreshAsync(string clientId, TokenSet tokens, CancellationToken cancellationToken)
    {
        try
        {
            return await PkceAuthenticator.RefreshAsync(clientId, tokens, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
