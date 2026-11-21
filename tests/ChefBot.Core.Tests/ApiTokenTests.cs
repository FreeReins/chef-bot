using ChefBot.Core.Engines;
using ChefBot.Core.Managers;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class ApiTokenTests
{
    private static readonly NewAccount Jamie = new("jamie", "Jamie", "correct horse battery");
    private static readonly NewAccount Sam = new("sam", "Sam", "another long password");

    private static async Task<(AccountManager Accounts, TestCore Core, Guid Admin, Guid Member)> CreateAsync()
    {
        var core = new TestCore();
        var accounts = core.AccountManager();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var invite = await accounts.CreateInviteAsync(admin.User.Id, default);
        var member = await accounts.AcceptInviteAsync(new AcceptInviteRequest(invite.Token, Sam.Username, Sam.DisplayName, Sam.Password), default);
        return (accounts, core, admin.User.Id, member.User.Id);
    }

    [Test]
    public async Task A_new_token_signs_in_as_its_owner()
    {
        var (accounts, _, admin, _) = await CreateAsync();

        var issued = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("  Claude Desktop "), default);
        var session = await accounts.FindApiTokenSessionAsync(issued.Token, default);

        await Assert.That(issued.Name).IsEqualTo("Claude Desktop");
        await Assert.That(issued.Token).StartsWith(SecurityEngine.ApiTokenPrefix);
        await Assert.That(session?.UserId).IsEqualTo(admin);
        await Assert.That(session!.IsAdmin).IsTrue();
    }

    [Test]
    public async Task Only_the_hash_of_a_token_is_stored()
    {
        var (accounts, core, admin, _) = await CreateAsync();

        var issued = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Claude"), default);

        var stored = core.Accounts.ApiTokens.Single();
        await Assert.That(stored.TokenHash).IsNotEqualTo(issued.Token);
        await Assert.That(stored.TokenHash).DoesNotContain(issued.Token);
    }

    [Test]
    public async Task Unknown_and_blank_tokens_sign_in_as_nobody()
    {
        var (accounts, _, admin, _) = await CreateAsync();
        await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Claude"), default);

        await Assert.That(await accounts.FindApiTokenSessionAsync("chefbot_not-a-real-token", default)).IsNull();
        await Assert.That(await accounts.FindApiTokenSessionAsync("  ", default)).IsNull();
    }

    [Test]
    public async Task People_see_only_their_own_tokens_newest_first()
    {
        var (accounts, core, admin, member) = await CreateAsync();
        await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Laptop"), default);
        core.Clock.Now += TimeSpan.FromMinutes(5);
        await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Phone"), default);
        await accounts.CreateApiTokenAsync(member, new CreateApiTokenRequest("Sam's"), default);

        var mine = await accounts.ListApiTokensAsync(admin, default);

        await Assert.That(string.Join(", ", mine.Select(token => token.Name))).IsEqualTo("Phone, Laptop");
    }

    [Test]
    public async Task A_revoked_token_stops_working()
    {
        var (accounts, _, admin, _) = await CreateAsync();
        var issued = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Claude"), default);

        await accounts.RevokeApiTokenAsync(admin, issued.Id, default);

        await Assert.That(await accounts.FindApiTokenSessionAsync(issued.Token, default)).IsNull();
        await Assert.That(await accounts.ListApiTokensAsync(admin, default)).IsEmpty();
    }

    [Test]
    public async Task Nobody_can_revoke_someone_elses_token()
    {
        var (accounts, _, admin, member) = await CreateAsync();
        var issued = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Claude"), default);

        var refused = await TestData.Caught<NotFoundException>(() => accounts.RevokeApiTokenAsync(member, issued.Id, default));

        await Assert.That(refused).IsNotNull();
        await Assert.That(await accounts.FindApiTokenSessionAsync(issued.Token, default)).IsNotNull();
    }

    [Test]
    public async Task Removing_an_account_removes_its_tokens()
    {
        var (accounts, core, admin, member) = await CreateAsync();
        var issued = await accounts.CreateApiTokenAsync(member, new CreateApiTokenRequest("Sam's"), default);

        await accounts.RemoveUserAsync(member, admin, default);

        await Assert.That(await accounts.FindApiTokenSessionAsync(issued.Token, default)).IsNull();
        await Assert.That(core.Accounts.ApiTokens).IsEmpty();
    }

    [Test]
    public async Task A_token_keeps_working_after_a_password_change()
    {
        var (accounts, _, admin, _) = await CreateAsync();
        var issued = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Claude"), default);

        await accounts.ChangePasswordAsync(admin, new ChangePasswordRequest(Jamie.Password, "a brand new password"), default);

        await Assert.That((await accounts.FindApiTokenSessionAsync(issued.Token, default))?.UserId).IsEqualTo(admin);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task A_token_needs_a_name(string name)
    {
        var (accounts, core, admin, _) = await CreateAsync();

        var refused = await TestData.Caught<RuleViolationException>(() => accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest(name), default));

        await Assert.That(refused).IsNotNull();
        await Assert.That(core.Accounts.ApiTokens).IsEmpty();
    }

    [Test]
    public async Task A_token_name_has_a_length_limit()
    {
        var (accounts, _, admin, _) = await CreateAsync();
        var name = new string('a', ValidationEngine.ApiTokenNameLength + 1);

        var refused = await TestData.Caught<RuleViolationException>(() => accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest(name), default));

        await Assert.That(refused).IsNotNull();
    }
}
