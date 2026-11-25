# Security

ChefBot is built for a home network, and guards the things that matter there: passwords, sessions, and requests it
makes on someone else's behalf. This page describes each part. To report a problem privately, see
[SECURITY.md](https://github.com/FreeReins/chef-bot/blob/main/SECURITY.md).

## Accounts and passwords

- Passwords are hashed with ASP.NET Core Identity's hasher (`IdentityPasswordHasher`), and need at least 8 characters.
- **Lockout:** after a number of wrong passwords in a row (5 to start with), an account is locked for a while (15
  minutes). Sign-ins sent at once count too.
- **Known devices:** a browser that signed in to an account carries an encrypted `chefbot.device` cookie
  (`KnownDevices`) for a year. Lockout doesn't apply to that account from it and doesn't count its mistakes, so
  someone guessing can't keep the owner out.
- **One answer for every refusal:** an unknown username, a wrong password and a locked account get the same message,
  and every path checks one password hash (`IPasswordHasher.VerifyNothing` against a decoy), so neither the words nor
  the timing show which usernames exist.
- **Per-address limits** (`SignInLimits`): sign-in, setup, accept-invite, reset-password and the link lookup take 10
  calls a minute from one address (an IPv6 /64), counted after trusted-proxy headers.
- **Invite and reset links** are random tokens stored as SHA-256 hashes. Invites last 7 days and resets 24 hours, and
  each works once.

## Sessions

- A cookie, `chefbot.session`: HttpOnly and `SameSite=Strict`, marked Secure when the request came over HTTPS. It
  lasts 30 days, renewed as it is used.
- Cookies are encrypted with ASP.NET Core Data Protection keys kept in `keys` in the data folder (`/data/keys` in the container), so sessions survive restarts.
  Losing the keys signs everyone out.
- Changing a password, resetting one, **Sign out everywhere** (`POST /api/account/sign-out-everywhere`) or restoring
  a backup ends older sessions, by bumping the account's `SessionVersion`.
- **Changes from other pages** (`CrossSiteRequests`): any method but GET, HEAD and OPTIONS is refused when
  `Sec-Fetch-Site` isn't `same-origin` or `none`, or, without it, `Origin` names another host. `SameSite=Strict`
  already stops other sites; this also stops another app on a neighbouring subdomain. `/mcp` is left out: it signs
  in with a token.

## Roles

There are two roles: admin and everyone else. Admin-only endpoints carry
`[Authorize(Policy = Sessions.AdminPolicy)]`. The core never checks roles, so a rule can't be forgotten in one of two
places.

## API tokens

- Only for `/mcp`, never for `/api`. Stored as SHA-256 hashes, shown once.
- A token acts as the person who made it. It stops when it expires (optional, 1 to 3650 days), is revoked, its
  owner's password is reset, or the account is removed. A password change revokes tokens only when asked.
- Its last use is recorded at most every 5 minutes.
- A **read-only** token is shown and allowed only the MCP tools marked `ReadOnly = true` (`ReadOnlyTokens`, an MCP
  request filter), so a new tool that isn't marked is refused by default.
- Left out of backups, so a leaked backup can't be used to reach MCP.

## HTTPS

- Self-signed by default (`Https/`), with a two-year certificate renewed in its last 30 days.
- **Always use HTTPS** redirects plain HTTP. `Strict-Transport-Security` is sent only with a real certificate.
- Behind a proxy, forwarded headers are believed only from `Https:TrustedProxies`.

## Browser rules

Every response carries a strict Content-Security-Policy: scripts, styles, images and API calls only from ChefBot
itself, no inline scripts, no framing. Also `X-Content-Type-Options: nosniff` (a stored photo is only ever shown as an
image) and `Referrer-Policy: same-origin`. The Swagger page at `/docs` and the help pages at `/help` get their own,
looser policy, because their bundled scripts aren't ChefBot's.

Page files are compressed, but API answers aren't: compressing them over HTTPS next to the sign-in cookie would expose
them to BREACH-style attacks.

## Requests ChefBot makes for someone

Recipe pages and photos come from addresses someone typed in. `PublicWeb.Handler` refuses private, loopback and
reserved addresses, after resolving the name, so a link can't reach the database or the home network. Any new
download of an address someone else chose must go through it.

The webhook and to-do apps are set by an admin, and may be on the home network.

## Secrets at rest

- The database password is a Podman secret, given to the app as `PGPASSWORD`.
- To-do app tokens are in the database and in backups, and are never sent back to the page.
- AI providers' API keys are sealed with Data Protection (`SecretBox`, prefix `dp1:`) under the session keys folder.
  Plain keys from before are sealed the first time they're read. A key sealed by another install reads as empty and
  logs a warning.
- Backups hold password hashes, so downloaded copies should be kept private. Outside Windows they're created `0600`
  in a `0700` folder.
