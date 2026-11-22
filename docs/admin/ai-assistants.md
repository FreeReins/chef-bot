# AI assistants (MCP)

ChefBot is an [MCP](https://modelcontextprotocol.io) server, so AI assistants such as Claude can work with your
kitchen. You can ask one to plan next week, swap Tuesday's dinner for something with chicken, mark tonight's meal
cooked, or add milk to the shopping list.

## What an assistant can do

Search and import recipes, read the week's plan, plan a week, swap, move or mark meals cooked, and look at or change
the pantry and the shopping list. Admin settings (people, stores, plan styles, backups and so on) aren't offered.

## Connect one

1. Open **Settings → AI assistants**.
2. Under **Name for a new token**, enter the assistant it's for, and press **Make a token**.
3. Copy the token. It's shown only once.
4. Give the assistant the **MCP address**, `https://<ChefBot's address>:8443/mcp`, and the token, sent as the header
   `Authorization: Bearer <token>`.

### Claude Code

ChefBot shows the exact command after making a token. It looks like this:

```sh
claude mcp add --transport http chefbot https://chefbot.local:8443/mcp --header "Authorization: Bearer chefbot_..."
```

### Other assistants

Any client that supports MCP over Streamable HTTP with a custom header takes the same address and header.

## Tokens

- A token acts as the person who made it. Each person makes and revokes their own.
- It keeps working after a password change, and stops when it's revoked or its account is removed.
- **Revoke** a token on the same page when you stop using an assistant.
- Tokens aren't kept in backups, so after a restore, make new ones.
- Tokens only work on `/mcp`, not on the rest of the API.

## The assistant has to reach ChefBot

- An assistant running on your computer (such as Claude Code) can reach ChefBot on your home network.
- An assistant running in the cloud can't, unless ChefBot is reachable from the internet. See
  [Behind a reverse proxy](reverse-proxy.md).
- With ChefBot's own certificate, the machine running the assistant has to trust it. See
  [Trust the certificate](https-and-certificates.md#trust-the-certificate-on-your-devices).

## If something goes wrong

See [Notifications and integrations](../troubleshooting/notifications-and-integrations.md#ai-assistants).
