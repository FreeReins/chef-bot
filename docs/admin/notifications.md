# Notifications

ChefBot can tell you when a plan is ready, when to start the slow cooker, when food is about to expire, and when
there's shopping to do. It posts each reminder to a **webhook**: an address that something else, such as Home
Assistant, turns into phone notifications or announcements.

## Set it up

1. Make a webhook in the app that will receive the reminders. For Home Assistant, see the example below.
2. In ChefBot, an admin opens **Settings → Notifications** and enters the **Webhook address**.
3. Choose **Send daily reminders at** and which reminders to send.
4. Press **Send a test**, and check that it arrives.

**Send today's reminders now** sends the daily reminders straight away, to check the other end.

## The reminders

| Reminder | `type` | Sent | `data` holds |
|---|---|---|---|
| A new plan is ready | `plan_ready` | When a week is staged or replanned | `planId`, `weekStart`, `trigger`, `style`, `dinners` (day, title, slowCooker), `snacks`, `sweets`, `drinks`, `toBuy` |
| Start the slow cooker | `slow_cooker` | At the daily time, on the day of a slow cooker dinner | `meals`, `ingredients` |
| Food to use soon | `expiring_food` | At the daily time, when pantry food is about to expire | `items` (name, expiresOn, daysLeft, usedByPlan) |
| Confirm the plan or finish shopping | `shopping` | At the daily time, the day before a week starts and on its first day | `status`, `toBuy` |
| Today's meals | `meals_today` | At the daily time (off to start with) | `meals` (slot, title, optional, leftover) |
| New achievements | `achievement` | When the household reaches a new badge tier, first step or chef rank | `achievements` (id, kind, tier, name) |
| Test | `test` | From **Send a test** | |

## What ChefBot sends

Every reminder is a JSON `POST`:

```json
{
  "type": "slow_cooker",
  "title": "Start the slow cooker",
  "message": "Tonight is Slow Cooker Beef Stew. Get the slow cooker going this morning.",
  "sentAt": "2026-11-10T13:30:00+00:00",
  "data": { "date": "2026-11-10", "meals": ["Slow Cooker Beef Stew"], "ingredients": ["stew meat", "carrot"] }
}
```

`title` and `message` are ready to show as they are. `type` and `data` let an automation word its own reminder, or do
something else entirely.

## Home Assistant

This automation forwards every reminder to your phones and also announces the slow cooker reminder on a speaker.
Add it in Home Assistant (**Settings → Automations → Create → Edit in YAML**), changing the entity names to yours:

```yaml
alias: "ChefBot reminders"
triggers:
  - trigger: webhook
    webhook_id: chefbot
    allowed_methods: [POST]
    local_only: true
actions:
  - action: notify.notify
    data:
      title: "{{ trigger.json.title }}"
      message: "{{ trigger.json.message }}"
  - if: "{{ trigger.json.type == 'slow_cooker' }}"
    then:
      - action: tts.speak
        target:
          entity_id: tts.home_assistant_cloud
        data:
          media_player_entity_id: media_player.kitchen
          message: "Remember to start {{ trigger.json.data.meals | join(' and ') }} in the slow cooker."
```

The webhook address for this example is `http://<your Home Assistant>:8123/api/webhook/chefbot`.

## Other apps

Anything that accepts a JSON webhook works: Node-RED, n8n, ntfy (through a small bridge), Gotify, Discord or Slack
through an automation tool. Use `title` and `message` for the text.

## Good to know

- Notifications go one way. ChefBot posts to the webhook. Home Assistant can't call ChefBot's API by itself, but it
  can add to the [shopping list](to-do-apps.md) or show the [calendar](../using/calendar.md).
- Webhook addresses can be on your home network.
- A webhook that's down is tried again over the next half hour. A reminder that still can't be delivered is logged
  under **Settings → Logs**.

## If something goes wrong

See [Notifications and integrations](../troubleshooting/notifications-and-integrations.md).
