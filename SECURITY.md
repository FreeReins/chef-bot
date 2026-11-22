# Security policy

## Reporting a problem

Please report security problems privately, not in a public issue:
[open a private security advisory](https://github.com/FreeReins/chef-bot/security/advisories/new).

Include what you found, how to reproduce it, and what someone could do with it. You'll get a reply as soon as
possible. Once a fix is released, you'll be credited in the release notes unless you'd rather not be.

## Supported versions

Only the latest release gets security fixes. [Upgrading](docs/admin/upgrading.md) explains how to update.

## Scope

ChefBot is built for a home network. [Security](docs/developers/security.md) describes how it protects passwords,
sessions and the requests it makes, and [Limits worth knowing](docs/reference/limits.md) what it doesn't do, such as
two-step sign-in. Reports about running ChefBot exposed to the internet without a reverse proxy are still welcome, but
the advice will be to use one.
