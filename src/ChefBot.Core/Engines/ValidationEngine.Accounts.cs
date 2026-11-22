using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <content>Checks on accounts, passwords and API tokens.</content>
internal sealed partial class ValidationEngine
{
    public async Task SetupIsOpenAsync(CancellationToken cancellationToken)
    {
        if (await accounts.AnyUsersAsync(cancellationToken))
        {
            throw new ConflictException("ChefBot is already set up. Sign in, or ask an admin for an invite link.");
        }
    }

    /// <summary>Checks the details for a new account and returns its normalized username.</summary>
    public async Task<string> NewAccountAsync(NewAccount account, CancellationToken cancellationToken)
    {
        var username = Usernames.Normalize(account.Username);
        if (!ValidUsername.IsMatch(username))
        {
            throw new RuleViolationException("A username needs 3 to 32 characters: letters, numbers, dots, dashes or underscores.");
        }

        Password(account.Password);
        if (await accounts.FindUserByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new ConflictException($"The username {username} is already taken.");
        }

        return username;
    }

    public static string Password(string? password) =>
        (password ?? string.Empty).Length >= MinimumPasswordLength
            ? password!
            : throw new RuleViolationException($"A password needs at least {MinimumPasswordLength} characters.");

    /// <summary>A trimmed API token name: not blank, and at most <see cref="ApiTokenNameLength"/> characters.</summary>
    public static string ApiTokenName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new RuleViolationException("Give the token a name, such as the assistant it is for.");
        }

        return trimmed.Length <= ApiTokenNameLength
            ? trimmed
            : throw new RuleViolationException($"A token's name can have at most {ApiTokenNameLength} characters.");
    }

    public static void CanRemove(Guid id, Guid actingUserId)
    {
        if (id == actingUserId)
        {
            throw new RuleViolationException("You can't remove your own account.");
        }
    }
}
