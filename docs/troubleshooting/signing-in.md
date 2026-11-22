# Signing-in problems

## "That username and password don't match an account"

Check the username (it's not your name) and the password, including capitals. Forgot it? Ask an admin to press
**Reset password** next to your name on the **People** tab and send you the link.

## An account is locked

> This account is locked after too many wrong passwords. Try again in … minutes, or ask an admin to unlock it.

Wait the minutes shown, or ask an admin to press **Unlock** next to your name on the People tab. Admins can change the
rules under **Settings → Login**. See [Lockout](../admin/people-and-accounts.md#lockout).

## The admin is locked out or forgot the password

Use the recovery setting: see [Lost the admin password](../admin/people-and-accounts.md#lost-the-admin-password).

## "This link can't be used any more"

Invite and reset links work once. Invites expire after 7 days and resets after 24 hours. Ask an admin for a new one.

## Everyone was signed out

That happens when:

- **a backup was restored:** sign in with the passwords from the time of the backup;
- **the data volume was lost or replaced,** for example a new container without `/data` on a named volume. Keep it
  on a volume. See [Keep the volume](../getting-started/install-docker-compose.md#keep-the-volume);
- **you changed your password:** your other devices sign out.

## Signed out over and over

- **Behind a reverse proxy:** set `Https__TrustedProxies`, so ChefBot knows you're on HTTPS. See
  [Behind a reverse proxy](../admin/reverse-proxy.md).
- **Switching between addresses** (`localhost`, an IP address, a name): each address has its own sign-in in the
  browser. Use the same one.
