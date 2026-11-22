# Notifications and integrations

## Reminders don't arrive

1. Press **Send a test** under **Settings → Notifications**. ChefBot says whether the webhook answered.
2. **"The webhook couldn't be reached"**: ChefBot's machine can't reach that address. With containers, `localhost`
   means the ChefBot container itself, not your computer: use the other machine's network address or name.
3. **"The webhook answered HTTP 404"**: the address is wrong. For Home Assistant, it's
   `http://<home assistant>:8123/api/webhook/<webhook_id>`, and the automation must be saved and on.
4. **Test works but daily reminders don't come:** check that the reminder is switched on, the daily time, and that
   there's something to remind about (a slow cooker dinner today, food expiring, a week starting).

## Home Assistant gets the reminder but nothing happens

The automation receives `trigger.json`. Check its trace in Home Assistant (**Automations → the automation →
Traces**). `local_only: true` means the request must come from your network.

## Sending to a to-do app fails

Press **Send a test item** under **Settings → To-do apps**. The message says what went wrong:

- **401 or 403:** the token is wrong or has expired. Make a new one and type it in.
- **Home Assistant, not found:** check the list's entity, such as `todo.shopping_list`.
- **Todoist:** check the project id, or leave it empty for the Inbox.

## The calendar doesn't update

See [When the calendar doesn't update](../using/calendar.md#when-the-calendar-doesnt-update).

## AI assistants

- **401 Unauthorized:** the token is wrong, revoked, or its account was removed. Tokens aren't kept in backups, so
  make a new one after a restore.
- **Can't connect:** the assistant must reach ChefBot at the MCP address. Cloud assistants can't reach a ChefBot only
  on your home network.
- **Certificate errors:** the machine running the assistant must trust ChefBot's certificate. See
  [Trust the certificate](../admin/https-and-certificates.md#trust-the-certificate-on-your-devices).
- **The `/api` endpoints say 401 with a token:** tokens only work on `/mcp`. Scripts use a sign-in cookie. See
  [The API](../reference/api.md).
