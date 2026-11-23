using ChefBot.Core.Managers;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class InboxTests
{
    private static readonly NewAccount Jamie = new("Jamie", "Jamie H", "correct horse battery");

    private static async Task<(AccountManager Accounts, TestCore Core, Guid UserId)> SignedInAsync()
    {
        var core = new TestCore();
        var accounts = core.AccountManager();
        var admin = await accounts.CreateFirstAdminAsync(Jamie, default);
        core.Messages.Published.Clear();
        return (accounts, core, admin.User.Id);
    }

    [Test]
    public async Task A_note_is_kept_unread_with_the_newest_first()
    {
        var (accounts, core, me) = await SignedInAsync();
        await accounts.AddInboxNoteAsync(me, new InboxNoteRequest("  Recipe saved.  "), default);
        core.Clock.Now = core.Clock.Now.AddMinutes(1);
        var inbox = await accounts.AddInboxNoteAsync(me, new InboxNoteRequest("Photo added."), default);

        await Assert.That(string.Join(", ", inbox.Entries.Select(entry => entry.Text))).IsEqualTo("Photo added., Recipe saved.");
        await Assert.That(inbox.Unread).IsEqualTo(2);
        await Assert.That(inbox.Entries[0].Source).IsEqualTo(InboxSource.Page);
        await Assert.That(core.Messages.Published.OfType<InboxChanged>().Count()).IsEqualTo(2);
    }

    [Test]
    public async Task Only_the_newest_messages_are_kept()
    {
        var (accounts, core, me) = await SignedInAsync();
        for (var i = 0; i < AccountManager.InboxSize + 3; i++)
        {
            core.Clock.Now = core.Clock.Now.AddSeconds(1);
            await accounts.AddInboxNoteAsync(me, new InboxNoteRequest($"Note {i}"), default);
        }

        var inbox = await accounts.GetInboxAsync(me, default);
        await Assert.That(inbox.Entries.Count).IsEqualTo(AccountManager.InboxSize);
        await Assert.That(inbox.Entries[^1].Text).IsEqualTo("Note 3");
    }

    [Test]
    public async Task Opening_the_inbox_reads_everything_and_clearing_empties_it()
    {
        var (accounts, _, me) = await SignedInAsync();
        await accounts.AddInboxNoteAsync(me, new InboxNoteRequest("Recipe saved."), default);

        var read = await accounts.MarkInboxReadAsync(me, default);
        await Assert.That(read.Unread).IsEqualTo(0);
        await Assert.That(read.Entries.Single().Read).IsTrue();

        var cleared = await accounts.ClearInboxAsync(me, default);
        await Assert.That(cleared.Entries).IsEmpty();
        await Assert.That((await accounts.GetInboxAsync(me, default)).Entries).IsEmpty();
    }

    [Test]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task A_blank_note_is_refused(string? text)
    {
        var (accounts, _, me) = await SignedInAsync();
        await Assert.That(await TestData.Caught<RuleViolationException>(() => accounts.AddInboxNoteAsync(me, new InboxNoteRequest(text!), default))).IsNotNull();
    }

    [Test]
    public async Task A_note_that_is_too_long_is_refused()
    {
        var (accounts, _, me) = await SignedInAsync();
        var text = new string('a', 501);
        await Assert.That(await TestData.Caught<RuleViolationException>(() => accounts.AddInboxNoteAsync(me, new InboxNoteRequest(text), default))).IsNotNull();
    }

    [Test]
    public async Task A_scheduled_plan_reaches_everyone_once()
    {
        var (accounts, core, me) = await SignedInAsync();
        var invite = await accounts.CreateInviteAsync(me, default);
        var sam = await accounts.AcceptInviteAsync(new AcceptInviteRequest(invite.Token, "sam", "Sam", "another long password"), default);
        var messageId = Guid.NewGuid();

        await accounts.AnnouncePlanStagedAsync(new PlanStaged(Guid.NewGuid(), "schedule"), messageId, default);
        await accounts.AnnouncePlanStagedAsync(new PlanStaged(Guid.NewGuid(), "schedule"), messageId, default);

        await Assert.That((await accounts.GetInboxAsync(me, default)).Unread).IsEqualTo(1);
        await Assert.That((await accounts.GetInboxAsync(sam.User.Id, default)).Entries.Single().Source).IsEqualTo(InboxSource.Plan);
    }

    [Test]
    public async Task A_plan_staged_by_a_person_is_not_announced()
    {
        var (accounts, core, me) = await SignedInAsync();
        await accounts.AnnouncePlanStagedAsync(new PlanStaged(Guid.NewGuid(), "manual"), Guid.NewGuid(), default);

        await Assert.That((await accounts.GetInboxAsync(me, default)).Entries).IsEmpty();
        await Assert.That(core.Messages.Published.OfType<InboxChanged>()).IsEmpty();
    }

    [Test]
    public async Task Removing_a_person_empties_their_inbox()
    {
        var (accounts, core, me) = await SignedInAsync();
        var invite = await accounts.CreateInviteAsync(me, default);
        var sam = await accounts.AcceptInviteAsync(new AcceptInviteRequest(invite.Token, "sam", "Sam", "another long password"), default);
        await accounts.AddInboxNoteAsync(sam.User.Id, new InboxNoteRequest("Recipe saved."), default);

        await accounts.RemoveUserAsync(sam.User.Id, me, default);

        await Assert.That(core.Inbox.Rows).IsEmpty();
    }
}
