using ChefBot.Core.Abstractions;
using ChefBot.Core.Accounts;
using ChefBot.Core.Common;

namespace ChefBot.Core.Tests;

public class AccountTests
{
    private static readonly NewAccount Jamie = new("Jamie", "Jamie H", "correct horse battery");
    private static readonly NewAccount Sam = new("sam", "Sam", "another long password");

    [Test]
    public async Task The_first_account_is_an_admin_and_ends_setup()
    {
        var (accounts, _) = Create();

        await Assert.That(await accounts.IsSetupRequiredAsync(default)).IsTrue();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);

        await Assert.That(admin.IsAdmin).IsTrue();
        await Assert.That(admin.Username).IsEqualTo("jamie");
        await Assert.That(admin.PasswordHash.Contains(Jamie.Password)).IsFalse();
        await Assert.That(await accounts.IsSetupRequiredAsync(default)).IsFalse();
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

        var user = await accounts.SignInAsync(" JAMIE ", Jamie.Password, default);

        await Assert.That(user.Username).IsEqualTo("jamie");
        await Assert.That(await Throws<NotSignedInException>(() => accounts.SignInAsync("jamie", "wrong password", default))).IsTrue();
        await Assert.That(await Throws<NotSignedInException>(() => accounts.SignInAsync("nobody", Jamie.Password, default))).IsTrue();
    }

    [Test]
    public async Task Weak_passwords_bad_usernames_and_duplicates_are_refused()
    {
        var (accounts, _) = Create();
        await accounts.CreateFirstAdminAsync(Jamie, default);
        var invite = await accounts.CreateInviteAsync(Guid.NewGuid(), default);

        await Assert.That(await Throws<RuleViolationException>(() => accounts.AcceptInviteAsync(invite.Token, Sam with { Password = "short" }, default))).IsTrue();
        await Assert.That(await Throws<RuleViolationException>(() => accounts.AcceptInviteAsync(invite.Token, Sam with { Username = "s m" }, default))).IsTrue();
        await Assert.That(await Throws<ConflictException>(() => accounts.AcceptInviteAsync(invite.Token, Sam with { Username = "JAMIE" }, default))).IsTrue();
    }

    [Test]
    public async Task An_invite_creates_one_ordinary_account_and_is_used_up()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var invite = await accounts.CreateInviteAsync(admin.Id, default);

        var sam = await accounts.AcceptInviteAsync(invite.Token, Sam, default);

        await Assert.That(sam.IsAdmin).IsFalse();
        await Assert.That((await accounts.ListUsersAsync(default)).Count).IsEqualTo(2);
        await Assert.That((await accounts.ListLinksAsync(default)).Count).IsEqualTo(0);
        await Assert.That(await Throws<NotFoundException>(() => accounts.AcceptInviteAsync(invite.Token, Sam with { Username = "sam2" }, default))).IsTrue();
    }

    [Test]
    public async Task Links_expire_and_can_be_revoked()
    {
        var (accounts, clock) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var expiring = await accounts.CreateInviteAsync(admin.Id, default);
        var revoked = await accounts.CreateInviteAsync(admin.Id, default);

        await accounts.RevokeLinkAsync(revoked.Id, default);
        await Assert.That(await Throws<NotFoundException>(() => accounts.DescribeLinkAsync(revoked.Token, default))).IsTrue();

        clock.Now += AccountService.InviteLifetime;
        await Assert.That(await Throws<NotFoundException>(() => accounts.DescribeLinkAsync(expiring.Token, default))).IsTrue();
        await Assert.That(await Throws<NotFoundException>(() => accounts.DescribeLinkAsync("not-a-real-token", default))).IsTrue();
    }

    [Test]
    public async Task Regenerating_a_link_replaces_it_and_stops_the_old_one()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var first = await accounts.CreateInviteAsync(admin.Id, default);

        var second = await accounts.RegenerateLinkAsync(first.Id, admin.Id, default);

        await Assert.That(second.Token == first.Token).IsFalse();
        await Assert.That(second.Purpose).IsEqualTo(LinkPurpose.Invite);
        await Assert.That((await accounts.ListLinksAsync(default)).Count).IsEqualTo(1);
        await Assert.That(await Throws<NotFoundException>(() => accounts.AcceptInviteAsync(first.Token, Sam, default))).IsTrue();
        await Assert.That((await accounts.AcceptInviteAsync(second.Token, Sam, default)).Username).IsEqualTo("sam");
    }

    [Test]
    public async Task Too_many_wrong_passwords_lock_the_account_for_a_while()
    {
        var (accounts, clock) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        await accounts.UpdateSecuritySettingsAsync(maxFailedSignIns: 3, lockoutMinutes: 10, requireHttps: false, default);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await Assert.That(await Throws<NotSignedInException>(() => accounts.SignInAsync("jamie", "wrong password", default))).IsTrue();
        }

        // Locked: even the right password is refused.
        await Assert.That(admin.IsLocked(clock.Now)).IsTrue();
        await Assert.That(await Throws<NotSignedInException>(() => accounts.SignInAsync("jamie", Jamie.Password, default))).IsTrue();

        clock.Now += TimeSpan.FromMinutes(11);
        await Assert.That((await accounts.SignInAsync("jamie", Jamie.Password, default)).Id == admin.Id).IsTrue();
        await Assert.That(admin.FailedSignIns).IsEqualTo(0);
    }

    [Test]
    public async Task A_successful_sign_in_clears_earlier_mistakes_and_an_admin_can_unlock()
    {
        var (accounts, clock) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        await accounts.UpdateSecuritySettingsAsync(maxFailedSignIns: 2, lockoutMinutes: 10, requireHttps: false, default);

        await Throws<NotSignedInException>(() => accounts.SignInAsync("jamie", "wrong password", default));
        await accounts.SignInAsync("jamie", Jamie.Password, default);
        await Assert.That(admin.FailedSignIns).IsEqualTo(0);

        await Throws<NotSignedInException>(() => accounts.SignInAsync("jamie", "wrong password", default));
        await Throws<NotSignedInException>(() => accounts.SignInAsync("jamie", "wrong password", default));
        await Assert.That(admin.IsLocked(clock.Now)).IsTrue();

        await accounts.UnlockUserAsync(admin.Id, default);
        await Assert.That((await accounts.SignInAsync("jamie", Jamie.Password, default)).Id == admin.Id).IsTrue();
    }

    [Test]
    public async Task Lockout_can_be_switched_off_and_its_limits_are_checked()
    {
        var (accounts, clock) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        await accounts.UpdateSecuritySettingsAsync(maxFailedSignIns: 0, lockoutMinutes: 10, requireHttps: false, default);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Throws<NotSignedInException>(() => accounts.SignInAsync("jamie", "wrong password", default));
        }

        await Assert.That(admin.IsLocked(clock.Now)).IsFalse();
        await Assert.That(await Throws<RuleViolationException>(() => accounts.UpdateSecuritySettingsAsync(-1, 10, false, default))).IsTrue();
        await Assert.That(await Throws<RuleViolationException>(() => accounts.UpdateSecuritySettingsAsync(5, 0, false, default))).IsTrue();
    }

    [Test]
    public async Task A_reset_link_sets_a_new_password_once_and_unlocks_the_account()
    {
        var (accounts, clock) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        admin.LockedUntil = clock.Now.AddHours(1);
        var sessionsBefore = admin.SessionVersion;
        var reset = await accounts.CreatePasswordResetAsync(admin.Id, admin.Id, default);

        var details = await accounts.DescribeLinkAsync(reset.Token, default);
        await accounts.ResetPasswordAsync(reset.Token, "a brand new password", default);

        await Assert.That(details.Purpose).IsEqualTo(LinkPurpose.PasswordReset);
        await Assert.That(details.Username).IsEqualTo("jamie");
        await Assert.That((await accounts.SignInAsync("jamie", "a brand new password", default)).Id == admin.Id).IsTrue();
        await Assert.That(admin.SessionVersion > sessionsBefore).IsTrue();
        await Assert.That(await Throws<NotFoundException>(() => accounts.ResetPasswordAsync(reset.Token, "yet another password", default))).IsTrue();
    }

    [Test]
    public async Task A_new_reset_link_replaces_the_old_one_and_cannot_be_used_to_join()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var first = await accounts.CreatePasswordResetAsync(admin.Id, admin.Id, default);
        var second = await accounts.CreatePasswordResetAsync(admin.Id, admin.Id, default);

        await Assert.That(await Throws<NotFoundException>(() => accounts.ResetPasswordAsync(first.Token, "a brand new password", default))).IsTrue();
        await Assert.That(await Throws<NotFoundException>(() => accounts.AcceptInviteAsync(second.Token, Sam, default))).IsTrue();
        await Assert.That(await Throws<RuleViolationException>(() => accounts.ResetPasswordAsync(second.Token, "short", default))).IsTrue();
    }

    [Test]
    public async Task Changing_your_own_password_needs_the_current_one()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);

        await Assert.That(await Throws<RuleViolationException>(() => accounts.ChangePasswordAsync(admin.Id, "not it", "a brand new password", default))).IsTrue();
        await accounts.ChangePasswordAsync(admin.Id, Jamie.Password, "a brand new password", default);

        await Assert.That((await accounts.SignInAsync("jamie", "a brand new password", default)).Id == admin.Id).IsTrue();
    }

    [Test]
    public async Task Only_admins_manage_people_and_nobody_removes_themselves()
    {
        var (accounts, _) = Create();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        var invite = await accounts.CreateInviteAsync(admin.Id, default);
        var sam = await accounts.AcceptInviteAsync(invite.Token, Sam, default);

        await Assert.That((await accounts.RequireAdminAsync(admin.Id, default)).Id == admin.Id).IsTrue();
        await Assert.That(await Throws<NotAllowedException>(() => accounts.RequireAdminAsync(sam.Id, default))).IsTrue();
        await Assert.That(await Throws<NotSignedInException>(() => accounts.RequireAdminAsync(null, default))).IsTrue();
        await Assert.That(await Throws<RuleViolationException>(() => accounts.RemoveUserAsync(admin.Id, admin.Id, default))).IsTrue();

        await accounts.RemoveUserAsync(sam.Id, admin.Id, default);
        await Assert.That((await accounts.ListUsersAsync(default)).Count).IsEqualTo(1);
    }

    private static (AccountService Accounts, FixedClock Clock) Create()
    {
        var clock = new FixedClock();
        var accounts = new AccountService(new FakeUsers(), new FakeLinks(), new TestData.FakeSettings(), new NoDatabase(), new PlainHasher(), clock);
        return (accounts, clock);
    }

    private static async Task<bool> Throws<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
            return false;
        }
        catch (TException)
        {
            return true;
        }
    }

    // In-memory stand-ins for the database and the password hasher.

    private sealed class FixedClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class NoDatabase : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class PlainHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password.Length}:{password.GetHashCode()}";

        public bool Verify(string hash, string password) => hash == Hash(password);
    }

    private sealed class FakeUsers : IUserRepository
    {
        private readonly List<User> _users = new();

        public Task<bool> AnyAsync(CancellationToken cancellationToken) => Task.FromResult(_users.Count > 0);

        public Task<User?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

        public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Username == username));

        public Task<List<User>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_users.ToList());

        public void Add(User user) => _users.Add(user);

        public void Remove(User user) => _users.Remove(user);
    }

    private sealed class FakeLinks : IAccessLinkRepository
    {
        private readonly List<AccessLink> _links = new();

        public Task<AccessLink?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_links.FirstOrDefault(link => link.Id == id));

        public Task<AccessLink?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(_links.FirstOrDefault(link => link.TokenHash == tokenHash));

        public Task<List<AccessLink>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_links.ToList());

        public void Add(AccessLink link) => _links.Add(link);

        public void Remove(AccessLink link) => _links.Remove(link);
    }

}
