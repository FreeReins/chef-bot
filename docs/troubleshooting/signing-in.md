# Signing-in problems

## "That username and password don't match an account"

Check the username (it's not your name) and the password, including capitals. Forgot it? Ask an admin to press
**Reset password** next to your name on the **People** tab and send you the link.

## An account is locked

ChefBot doesn't say when an account is locked: a locked account gets the same "don't match an account" answer,
followed by how long lockout lasts. If the password is surely right, wait those minutes, sign in from a device you've
used before (lockout doesn't apply there), or ask an admin to press **Unlock** next to your name on the People tab.
Admins can change the rules under **Settings → Login**. See [Lockout](../admin/people-and-accounts.md#lockout).

## "Too many tries from this network in a short time"

One network address can try signing in, setup, invites and reset links 10 times a minute. Wait a minute. If everyone
behind a reverse proxy gets it at once, ChefBot is counting the proxy's address: set `Https__TrustedProxies`
([Behind a reverse proxy](../admin/reverse-proxy.md)).

## "ChefBot only accepts changes sent from its own page"

ChefBot refuses changes that a browser says came from another web address, so a page elsewhere on your network can't
act as you. Open ChefBot at its own address. Behind a reverse proxy, make sure it passes the original `Host` header.

## The admin is locked out or forgot the password

Use the recovery setting: see [Lost the admin password](../admin/people-and-accounts.md#lost-the-admin-password).

## "This link can't be used any more"

Invite and reset links work once. Invites expire after 7 days and resets after 24 hours. Ask an admin for a new one.

## Everyone was signed out

That happens when:

- **a backup was restored:** sign in with the passwords from the time of the backup;
- **the data volume was lost or replaced,** for example a new container without `/data` on a named volume. Keep it
  on a volume. See [Keep the volume](../getting-started/install-docker-compose.md#keep-the-volume);
- **you changed your password, or pressed Sign out everywhere:** your other devices sign out;
- **an admin reset your password:** the reset link signs you out everywhere and revokes your API tokens.

## Signed out over and over

- **Behind a reverse proxy:** set `Https__TrustedProxies`, so ChefBot knows you're on HTTPS. See
  [Behind a reverse proxy](../admin/reverse-proxy.md).
- **Switching between addresses** (`localhost`, an IP address, a name): each address has its own sign-in in the
  browser. Use the same one.
