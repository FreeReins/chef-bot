# People and accounts

This page is for the admin: the person who set ChefBot up. It covers inviting people, resetting passwords, locking
and unlocking accounts, and getting back in if you lose the admin password.

Everyone in a ChefBot shares the same recipes, plan and pantry. There is one household per installation.

## Invite someone

1. Open the **People** tab and press **Create invite link**.
2. Send the link to the person by message or email. ChefBot doesn't send email itself.
3. They open it and create their own account.

An invite link works once and expires after 7 days. It's shown only once: if you didn't copy it, press **Replace
link** under **Pending links**. The old link stops working and a new one is shown. **Delete** cancels an unused link.

Inviting anyone is optional. ChefBot works the same for a household of one.

## Remove someone

Press the remove button next to their name on the People tab. Their account and sign-ins end. The recipes, plans and
pantry stay, because they belong to the household.

## Passwords

- **Change your own password** under **Settings → Login**. Your other devices are signed out.
- **Someone forgot theirs:** press **Reset password** next to their name on the People tab and send them the link.
  It works once and expires after 24 hours.

## Lockout

After 5 wrong passwords in a row, an account is locked for 15 minutes, and the People tab shows **Locked after too
many tries**.

- **Unlock someone early** with **Unlock** next to their name.
- **Change the rules** under **Settings → Login → Sign-in lockout**: how many wrong passwords in a row (0 turns lockout
  off), and how many minutes an account stays locked.
- Sign-ins sent all at once count too: only the first 5 have their passwords checked.

## Lost the admin password

When no admin can sign in, use the recovery setting:

1. Set `Accounts__RecoverUsername` to the admin's username:
   - **Podman:** add `Accounts__RecoverUsername=yourname` to `~/.config/chefbot/chefbot.env`, then run
     `systemctl --user restart chefbot`.
   - **Docker Compose:** add `Accounts__RecoverUsername: yourname` under the ChefBot service's `environment:`, then
     run `docker compose up -d`.
2. Find the reset line in the log:
   - **Podman:** `journalctl --user -u chefbot | grep "Password reset"`
   - **Docker Compose:** `docker compose logs chefbot | grep "Password reset"`

   It reads like `Password reset for yourname: open ChefBot and add /?reset=… to its address`.
3. Open ChefBot's address with `/?reset=…` on the end, such as `https://chefbot.local:8443/?reset=…`, and choose a
   new password. The link works once and expires after 24 hours.
4. **Remove the setting** and restart again, so a new link isn't logged at every start.

If the log says there's no such account, check the spelling of the username.

## After a restore

Restoring a backup brings back the accounts and passwords as they were when the backup was made, and signs everyone
out, the admin included. See [Backups and restore](backups-and-restore.md).

## If something goes wrong

See [Signing-in problems](../troubleshooting/signing-in.md).
