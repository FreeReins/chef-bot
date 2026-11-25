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
        await Assert.That(session?.Session.UserId).IsEqualTo(admin);
        await Assert.That(session!.Session.IsAdmin).IsTrue();
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

        await Assert.That((await accounts.FindApiTokenSessionAsync(issued.Token, default))?.Session.UserId).IsEqualTo(admin);
    }

    [Test]
    public async Task Changing_the_password_can_revoke_tokens_too()
    {
        var (accounts, core, admin, _) = await CreateAsync();
        var issued = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Claude"), default);

        await accounts.ChangePasswordAsync(admin, new ChangePasswordRequest(Jamie.Password, "a brand new password", RevokeApiTokens: true), default);

        await Assert.That(await accounts.FindApiTokenSessionAsync(issued.Token, default)).IsNull();
        await Assert.That(core.Accounts.ApiTokens).IsEmpty();
    }

    [Test]
    public async Task A_password_reset_revokes_the_accounts_tokens()
    {
        var (accounts, _, admin, member) = await CreateAsync();
        var samsToken = await accounts.CreateApiTokenAsync(member, new CreateApiTokenRequest("Sam's"), default);
        var adminsToken = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Jamie's"), default);
        var reset = await accounts.CreatePasswordResetAsync(member, admin, default);

        await accounts.ResetPasswordAsync(new ResetPasswordRequest(reset.Token, "taken back at last"), default);

        await Assert.That(await accounts.FindApiTokenSessionAsync(samsToken.Token, default)).IsNull();
        await Assert.That(await accounts.FindApiTokenSessionAsync(adminsToken.Token, default)).IsNotNull();
    }

    [Test]
    public async Task A_token_made_to_expire_stops_working_then()
    {
        var (accounts, core, admin, _) = await CreateAsync();
        var issued = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Trial", ExpiresInDays: 30), default);

        await Assert.That(issued.ExpiresAt).IsEqualTo(core.Clock.Now.AddDays(30));
        core.Clock.Now += TimeSpan.FromDays(30) - TimeSpan.FromMinutes(1);
        await Assert.That(await accounts.FindApiTokenSessionAsync(issued.Token, default)).IsNotNull();
        core.Clock.Now += TimeSpan.FromMinutes(1);
        await Assert.That(await accounts.FindApiTokenSessionAsync(issued.Token, default)).IsNull();
        await Assert.That((await accounts.ListApiTokensAsync(admin, default)).Single().ExpiresAt).IsEqualTo(issued.ExpiresAt);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(SecurityEngine.MaxApiTokenDays + 1)]
    public async Task A_token_lasts_a_sensible_number_of_days(int days)
    {
        var (accounts, core, admin, _) = await CreateAsync();

        var refused = await TestData.Caught<RuleViolationException>(
            () => accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Claude", ExpiresInDays: days), default));

        await Assert.That(refused).IsNotNull();
        await Assert.That(core.Accounts.ApiTokens).IsEmpty();
    }

    [Test]
    public async Task A_read_only_token_says_so()
    {
        var (accounts, _, admin, _) = await CreateAsync();
        var reader = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Reader", ReadOnly: true), default);
        var writer = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Writer"), default);

        await Assert.That(reader.ReadOnly).IsTrue();
        await Assert.That((await accounts.FindApiTokenSessionAsync(reader.Token, default))!.ReadOnly).IsTrue();
        await Assert.That((await accounts.FindApiTokenSessionAsync(writer.Token, default))!.ReadOnly).IsFalse();
        await Assert.That((await accounts.ListApiTokensAsync(admin, default)).Single(token => token.Name == "Reader").ReadOnly).IsTrue();
    }

    [Test]
    public async Task A_tokens_last_use_is_recorded_every_few_minutes()
    {
        var (accounts, core, admin, _) = await CreateAsync();
        var issued = await accounts.CreateApiTokenAsync(admin, new CreateApiTokenRequest("Claude"), default);
        await Assert.That((await accounts.ListApiTokensAsync(admin, default)).Single().LastUsedAt).IsNull();

        var first = core.Clock.Now;
        await accounts.FindApiTokenSessionAsync(issued.Token, default);
        core.Clock.Now += TimeSpan.FromMinutes(1);
        await accounts.FindApiTokenSessionAsync(issued.Token, default);
        await Assert.That((await accounts.ListApiTokensAsync(admin, default)).Single().LastUsedAt).IsEqualTo(first);

        core.Clock.Now += SecurityEngine.LastUsedPrecision;
        await accounts.FindApiTokenSessionAsync(issued.Token, default);
        await Assert.That((await accounts.ListApiTokensAsync(admin, default)).Single().LastUsedAt).IsEqualTo(core.Clock.Now);
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
