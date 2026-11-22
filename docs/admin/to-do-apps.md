# To-do apps

The Shopping tab can send what's left to buy to a to-do app, one item per thing, such as "fish fillets, 600 g", with
its store and meals where the app keeps a description. An admin sets the apps up under **Settings → To-do apps**.

## Add one

1. Open **Settings → To-do apps** and add an app.
2. Give it a **Name**, such as "Kitchen list". That's what the Shopping tab's **Send to** button shows.
3. Choose its **Kind** and fill in the fields below.
4. Save, then press **Send a test item** to check the connection.

### Home Assistant

Reaches any to-do list Home Assistant has: its own Shopping List, Local To-do, or one it syncs with Bring!, Todoist,
Google Tasks and others.

| Field | What to enter |
|---|---|
| **Home Assistant address** | Such as `http://homeassistant.local:8123` |
| **To-do list** | The list's entity, such as `todo.shopping_list` (**Settings → Devices & services → Entities** in Home Assistant) |
| **Access token** | A long-lived access token from your Home Assistant profile page, under **Security** |

ChefBot calls Home Assistant's `todo.add_item` for each item.

### Todoist

| Field | What to enter |
|---|---|
| **Access token** | Your API token, from Todoist's **Settings → Integrations → Developer** |
| **Project id** | Optional. Empty for the Inbox. The id is the number at the end of the project's web address |

### Webhook

Posts the whole list at once, for an automation to handle:

```json
{"type": "shopping_list", "sentAt": "2026-11-10T13:30:00+00:00", "items": [{"title": "fish fillets, 600 g", "description": "…"}]}
```

| Field | What to enter |
|---|---|
| **Webhook address** | Where to post it |
| **Bearer token** | Optional. Sent as `Authorization: Bearer <token>` |

## Good to know

- Tokens are kept in the database and in backups, and are never shown again once saved. Leave a token field empty
  to keep the saved one.
- Sending doesn't tick anything off in ChefBot, and sending again adds the items again. Send once you've finished
  changing the plan.
- Items go one at a time, so a problem part-way stops the rest. ChefBot says how many were sent.

## If something goes wrong

See [Notifications and integrations](../troubleshooting/notifications-and-integrations.md).
