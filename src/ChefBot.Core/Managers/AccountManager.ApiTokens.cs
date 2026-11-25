using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>API tokens, which assistants such as MCP clients sign in with.</content>
internal sealed partial class AccountManager
{
    public async Task<IReadOnlyList<ApiTokenResponse>> ListApiTokensAsync(Guid userId, CancellationToken cancellationToken) =>
        (await accounts.ListApiTokensAsync(userId, cancellationToken))
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => token.ToResponse())
            .ToList();

    public async Task<IssuedApiToken> CreateApiTokenAsync(Guid userId, CreateApiTokenRequest request, CancellationToken cancellationToken)
    {
        var name = ValidationEngine.ApiTokenName(request.Name);
        var days = ValidationEngine.ApiTokenDays(request.ExpiresInDays);
        var user = await validation.UserAsync(userId, cancellationToken);
        var issued = security.IssueApiToken(user.Id, name, days, request.ReadOnly, clock.GetUtcNow());
        await accounts.SaveApiTokenAsync(issued.Token, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        var token = issued.Token;
        return new IssuedApiToken(token.Id, token.Name, issued.Secret, token.CreatedAt, token.ExpiresAt, token.ReadOnly);
    }

    public async Task RevokeApiTokenAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var token = (await accounts.ListApiTokensAsync(userId, cancellationToken)).FirstOrDefault(token => token.Id == id)
            ?? throw new NotFoundException("That token no longer exists.");
        await accounts.RemoveApiTokenAsync(token.Id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<ApiTokenSession?> FindApiTokenSessionAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var now = clock.GetUtcNow();
        var stored = await accounts.FindApiTokenByHashAsync(security.HashToken(token), cancellationToken);
        if (stored is null || security.IsExpired(stored, now)
            || await accounts.FindUserAsync(stored.UserId, cancellationToken) is not { } user)
        {
            return null;
        }

        await accounts.MarkApiTokenUsedAsync(stored.Id, now, now - SecurityEngine.LastUsedPrecision, cancellationToken);
        return new ApiTokenSession(user.ToSession(), stored.ReadOnly);
    }

    private async Task RemoveApiTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (var token in await accounts.ListApiTokensAsync(userId, cancellationToken))
        {
            await accounts.RemoveApiTokenAsync(token.Id, cancellationToken);
        }
    }
}
