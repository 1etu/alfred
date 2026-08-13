namespace Alfred.App.Sync;

internal sealed record TokenSet(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresUtc,
    string Account);
