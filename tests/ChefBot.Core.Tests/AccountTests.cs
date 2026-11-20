using ChefBot.Core.Contracts;
using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Engines;
using ChefBot.Core.Managers;

namespace ChefBot.Core.Tests;

public class AccountTests
{
    private static readonly NewAccount Jamie = new("Jamie", "Jamie H", "correct horse battery");
    private static readonly NewAccount Sam = new("sam", "Sam", "another long password");

    private static SignInRequest SignIn(string username, string password) => new(username, password);

    private static AcceptInviteRequest Join(string token, NewAccount account) =>
        new(token, account.Username, account.DisplayName, account.Password);

    private static SecurityRequest Rules(int maxFailed, int lockoutMinutes) => new(maxFailed, lockoutMinutes, RequireHttps: false);

    private static (AccountManager Accounts, TestCore Core) Create()
    {
        var core = new TestCore();
        return (core.AccountManager(), core);
    }

    private static async Task<bool> Throws<TException>(Func<Task> action)
        where TException : Exception =>
        await TestData.Caught<TException>(action) is not null;

    [Test]
    public async Task The_first_account_is_an_admin_and_ends_setup()
    {
        var (accounts, core) = Create();

        await Assert.That((await accounts.DescribeSessionAsync(null, default)).SetupRequired).IsTrue();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);

        await Assert.That(admin.User.IsAdmin).IsTrue();
        await Assert.That(admin.Session.IsAdmin).IsTrue();
        await Assert.That(admin.User.Username).IsEqualTo("jamie");
        await Assert.That(core.Accounts.User("jamie").PasswordHash.Contains(Jamie.Password)).IsFalse();
        await Assert.That((await accounts.DescribeSessionAsync(admin.User.Id, default)).SetupRequired).IsFalse();
    }

    [Test]
    public async Task Setup_can_only_be_done_once()
    {
        var (accounts, _) = Create();
        await accounts.CreateFirstAdminAsync(Jamie, default);

        await Assert.That(await Throws<ConflictException>(() => accounts.CreateFirstAdminAsync(Sam, default))).IsTrue();
    }

    [Test]
    public async Task Signing_in_needs_the_right_password()
    {
        var (accounts, _) = Create();
        await accounts.CreateFirstAdminAsync(Jamie, default);

        var user = await accounts.SignInAsync(SignIn(" JAMIE ", Jamie.Password), default);

        await Assert.That(user.User.Username).IsEqualTo("jamie");
        await Assert.That(await Throws<NotSignedInException>(() => accounts.SignInAsync(SignIn("jamie", "wrong password"), default))).IsTrue();
        await Assert.That(await Throws<NotSignedInException>(() => accounts.SignInAsync(SignIn("nobody", Jamie.Password), default))).IsTrue();
    }

    [Test]
    public async Task Weak_passwords_bad_usernames_and_duplicates_are_refused()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var invite = await accounts.CreateInviteAsync(admin.User.Id, default);

        await Assert.That(await Throws<RuleViolationException>(() => accounts.AcceptInviteAsync(Join(invite.Token, Sam with { Password = "short" }), default))).IsTrue();
        await Assert.That(await Throws<RuleViolationException>(() => accounts.AcceptInviteAsync(Join(invite.Token, Sam with { Username = "s m" }), default))).IsTrue();
        await Assert.That(await Throws<ConflictException>(() => accounts.AcceptInviteAsync(Join(invite.Token, Sam with { Username = "JAMIE" }), default))).IsTrue();
    }

    [Test]
    public async Task An_invite_creates_one_ordinary_account_and_is_used_up()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var invite = await accounts.CreateInviteAsync(admin.User.Id, default);

        var sam = await accounts.AcceptInviteAsync(Join(invite.Token, Sam), default);

        await Assert.That(sam.User.IsAdmin).IsFalse();
        await Assert.That(sam.Session.IsAdmin).IsFalse();
        await Assert.That((await accounts.ListUsersAsync(default)).Count).IsEqualTo(2);
        await Assert.That((await accounts.ListLinksAsync(default)).Count).IsEqualTo(0);
        await Assert.That(await Throws<NotFoundException>(() => accounts.AcceptInviteAsync(Join(invite.Token, Sam with { Username = "sam2" }), default))).IsTrue();
    }

    [Test]
    public async Task Links_expire_and_can_be_revoked()
    {
        var (accounts, core) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var expiring = await accounts.CreateInviteAsync(admin.User.Id, default);
        var revoked = await accounts.CreateInviteAsync(admin.User.Id, default);

        await accounts.RevokeLinkAsync(revoked.Id, default);
        await Assert.That(await Throws<NotFoundException>(() => accounts.DescribeLinkAsync(revoked.Token, default))).IsTrue();

        core.Clock.Now += SecurityEngine.InviteLifetime;
        await Assert.That(await Throws<NotFoundException>(() => accounts.DescribeLinkAsync(expiring.Token, default))).IsTrue();
        await Assert.That(await Throws<NotFoundException>(() => accounts.DescribeLinkAsync("not-a-real-token", default))).IsTrue();

        // Listing the links cleans up the expired one.
        await Assert.That((await accounts.ListLinksAsync(default)).Count).IsEqualTo(0);
        await Assert.That(core.Accounts.Links.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Regenerating_a_link_replaces_it_and_stops_the_old_one()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var first = await accounts.CreateInviteAsync(admin.User.Id, default);

        var second = await accounts.RegenerateLinkAsync(first.Id, admin.User.Id, default);

        await Assert.That(second.Token == first.Token).IsFalse();
        await Assert.That(second.Purpose).IsEqualTo(LinkPurpose.Invite);
        await Assert.That((await accounts.ListLinksAsync(default)).Count).IsEqualTo(1);
        await Assert.That(await Throws<NotFoundException>(() => accounts.AcceptInviteAsync(Join(first.Token, Sam), default))).IsTrue();
        await Assert.That((await accounts.AcceptInviteAsync(Join(second.Token, Sam), default)).User.Username).IsEqualTo("sam");
    }

    [Test]
    public async Task Too_many_wrong_passwords_lock_the_account_for_a_while()
    {
        var (accounts, core) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        await accounts.UpdateSecurityAsync(Rules(maxFailed: 3, lockoutMinutes: 10), default);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await Assert.That(await Throws<NotSignedInException>(() => accounts.SignInAsync(SignIn("jamie", "wrong password"), default))).IsTrue();
        }

        // Locked: even the right password is refused.
        await Assert.That(core.Accounts.User("jamie").LockedUntil > core.Clock.Now).IsTrue();
        await Assert.That(await Throws<NotSignedInException>(() => accounts.SignInAsync(SignIn("jamie", Jamie.Password), default))).IsTrue();

        core.Clock.Now += TimeSpan.FromMinutes(11);
        await Assert.That((await accounts.SignInAsync(SignIn("jamie", Jamie.Password), default)).User.Id == admin.User.Id).IsTrue();
        await Assert.That(core.Accounts.User("jamie").FailedSignIns).IsEqualTo(0);
    }

    [Test]
    public async Task A_successful_sign_in_clears_earlier_mistakes_and_an_admin_can_unlock()
    {
        var (accounts, core) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        await accounts.UpdateSecurityAsync(Rules(maxFailed: 2, lockoutMinutes: 10), default);

        await Throws<NotSignedInException>(() => accounts.SignInAsync(SignIn("jamie", "wrong password"), default));
        await accounts.SignInAsync(SignIn("jamie", Jamie.Password), default);
        await Assert.That(core.Accounts.User("jamie").FailedSignIns).IsEqualTo(0);

        await Throws<NotSignedInException>(() => accounts.SignInAsync(SignIn("jamie", "wrong password"), default));
        await Throws<NotSignedInException>(() => accounts.SignInAsync(SignIn("jamie", "wrong password"), default));
        await Assert.That(core.Accounts.User("jamie").LockedUntil > core.Clock.Now).IsTrue();

        await accounts.UnlockUserAsync(admin.User.Id, default);
        await Assert.That((await accounts.SignInAsync(SignIn("jamie", Jamie.Password), default)).User.Id == admin.User.Id).IsTrue();
    }

    [Test]
    public async Task Lockout_can_be_switched_off_and_its_limits_are_checked()
    {
        var (accounts, core) = Create();
        await accounts.CreateFirstAdminAsync(Jamie, default);
        await accounts.UpdateSecurityAsync(Rules(maxFailed: 0, lockoutMinutes: 10), default);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Throws<NotSignedInException>(() => accounts.SignInAsync(SignIn("jamie", "wrong password"), default));
        }

        await Assert.That(core.Accounts.User("jamie").LockedUntil is null).IsTrue();
        await Assert.That(await Throws<RuleViolationException>(() => accounts.UpdateSecurityAsync(Rules(-1, 10), default))).IsTrue();
        await Assert.That(await Throws<RuleViolationException>(() => accounts.UpdateSecurityAsync(Rules(5, 0), default))).IsTrue();
    }

    [Test]
    public async Task Changing_the_sign_in_rules_tells_the_web_server()
    {
        var (accounts, core) = Create();

        await accounts.UpdateSecurityAsync(new SecurityRequest(5, 15, RequireHttps: true), default);

        await Assert.That(core.Messages.Published.OfType<SecuritySettingsChanged>().Single().RequireHttps).IsTrue();
    }

    [Test]
    public async Task A_reset_link_sets_a_new_password_once_and_unlocks_the_account()
    {
        var (accounts, core) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        core.Accounts.User("jamie").LockedUntil = core.Clock.Now.AddHours(1);
        var reset = await accounts.CreatePasswordResetAsync(admin.User.Id, admin.User.Id, default);

        var details = await accounts.DescribeLinkAsync(reset.Token, default);
        var after = await accounts.ResetPasswordAsync(new ResetPasswordRequest(reset.Token, "a brand new password"), default);

        await Assert.That(details.Purpose).IsEqualTo(LinkPurpose.PasswordReset);
        await Assert.That(details.Username).IsEqualTo("jamie");
        await Assert.That((await accounts.SignInAsync(SignIn("jamie", "a brand new password"), default)).User.Id == admin.User.Id).IsTrue();
        await Assert.That(after.Session.SessionVersion > admin.Session.SessionVersion).IsTrue();
        await Assert.That(await Throws<NotFoundException>(() => accounts.ResetPasswordAsync(new ResetPasswordRequest(reset.Token, "yet another password"), default))).IsTrue();
    }

    [Test]
    public async Task A_new_reset_link_replaces_the_old_one_and_cannot_be_used_to_join()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var first = await accounts.CreatePasswordResetAsync(admin.User.Id, admin.User.Id, default);
        var second = await accounts.CreatePasswordResetAsync(admin.User.Id, admin.User.Id, default);

        await Assert.That(await Throws<NotFoundException>(() => accounts.ResetPasswordAsync(new ResetPasswordRequest(first.Token, "a brand new password"), default))).IsTrue();
        await Assert.That(await Throws<NotFoundException>(() => accounts.AcceptInviteAsync(Join(second.Token, Sam), default))).IsTrue();
        await Assert.That(await Throws<RuleViolationException>(() => accounts.ResetPasswordAsync(new ResetPasswordRequest(second.Token, "short"), default))).IsTrue();
    }

    [Test]
    public async Task The_recovery_setting_issues_a_reset_link_for_an_existing_account_only()
    {
        var (accounts, _) = Create();
        await accounts.CreateFirstAdminAsync(Jamie, default);

        await Assert.That(await accounts.CreateRecoveryLinkAsync(" Jamie ", default) is not null).IsTrue();
        await Assert.That(await accounts.CreateRecoveryLinkAsync("nobody", default) is null).IsTrue();
    }

    [Test]
    public async Task Changing_your_own_password_needs_the_current_one()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);

        await Assert.That(await Throws<RuleViolationException>(() =>
            accounts.ChangePasswordAsync(admin.User.Id, new ChangePasswordRequest("not it", "a brand new password"), default))).IsTrue();
        await accounts.ChangePasswordAsync(admin.User.Id, new ChangePasswordRequest(Jamie.Password, "a brand new password"), default);

        await Assert.That((await accounts.SignInAsync(SignIn("jamie", "a brand new password"), default)).User.Id == admin.User.Id).IsTrue();
    }

    [Test]
    public async Task Nobody_removes_themselves_and_a_removed_account_has_no_session()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var invite = await accounts.CreateInviteAsync(admin.User.Id, default);
        var sam = await accounts.AcceptInviteAsync(Join(invite.Token, Sam), default);

        await Assert.That(await Throws<RuleViolationException>(() => accounts.RemoveUserAsync(admin.User.Id, admin.User.Id, default))).IsTrue();

        await accounts.RemoveUserAsync(sam.User.Id, admin.User.Id, default);
        await Assert.That((await accounts.ListUsersAsync(default)).Count).IsEqualTo(1);
        await Assert.That(await accounts.FindSessionAsync(sam.User.Id, default) is null).IsTrue();
        await Assert.That((await accounts.FindSessionAsync(admin.User.Id, default))!.IsAdmin).IsTrue();
    }
}
