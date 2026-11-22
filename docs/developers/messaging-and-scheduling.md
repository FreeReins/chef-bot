# Messaging and scheduling

Areas of ChefBot react to each other through messages, never direct calls. This keeps managers independent, and it
means slow or failing outside services (a webhook, a photo site) never hold up a request.

## Events and commands

- **Events** say something already happened: `MealCooked`, `RecipesAdded`, `PantryChanged`, `PlanStaged`. Each
  subscriber handles an event on its own, so one event with three handlers is handled three times.
- **Commands** ask for one thing to be done: `FetchRecipeImages`, `CheckWeeklySchedule`.

Records live in `Managers/Contracts/Messages/`, each with `[MessageIdentity("chefbot.<name>.v1")]` and a line in
`ContractJsonContext`. They're stored, so their shapes are a contract: they only gain fields, and a breaking change
gets a `.v2` identity.

## Who listens

| Handler | Reacts to | Does |
|---|---|---|
| `PantryChangedHandler` | `PantryChanged` | Rebuilds the open plans' shopping lists after a hand edit to the pantry |
| `PlanningSettingsChangedHandler` | `PlanningSettingsChanged` | Rebuilds open shopping lists after the advanced settings change |
| `SecuritySettingsChangedHandler` | `SecuritySettingsChanged` | Applies a changed "Always use HTTPS" to the running server |
| `GroceryStoresChangedHandler` | `GroceryStoresChanged` | Re-splits open shopping lists between stores |
| `HistoryProjectionHandler` | kitchen events | Writes the food history |
| `AchievementProgressHandler` | kitchen events | Works out newly earned achievements |
| `AchievementsUnlockedNotificationHandler` | `AchievementsUnlocked` | Sends the achievement notification |
| `PlanStagedNotificationHandler` | `PlanStaged` | Sends "a new plan is ready" |
| `FetchRecipeImagesHandler` | `FetchRecipeImages` | Downloads imported photos |
| `LiveTopicsHandler` | most events | Tells open pages what changed, through `/api/live` |
| `CheckWeeklyScheduleHandler`, `SendDueRemindersHandler`, `RunDueBackupHandler` | scheduled commands | The timed jobs |
| `SendReminderHandler` | one reminder | Delivers it to the webhook, retried by the bus while the webhook is down |

## Queues

Each handler runs on a named queue (`MessageQueues`, set with `[StickyHandler]`):

| Queue | Runs |
|---|---|
| `plans` | One at a time, next to plan writes from requests |
| `history`, `achievements`, `maintenance` | One at a time |
| `images`, `notifications` | A couple at a time |
| `live` | In memory only: a page that misses a nudge reloads everything when it reconnects |

## Safe to repeat

A message can arrive twice, so every handler must be safe to run again: work things out again from what is saved, or
check a marker. Handlers log expected problems instead of throwing. Retries are never written by hand:
`CoreRegistration` retries database hiccups and unreachable outside services (thrown as `TransientDeliveryException`)
over about half an hour, and anything else goes to `wolverine.wolverine_dead_letters`.

## Scheduled jobs

Next week's plan, the daily reminders and the daily backup are **scheduled messages** in the database:

```mermaid
flowchart LR
  start["Startup: StartupJobs"] -->|"look now (DueAt = null)"| job
  job["Job handler"] -->|"due? do the work"| work["Plan / remind / back up"]
  job -->|"save next due time, schedule next message"| db[("wolverine schema")]
  db -->|"at due time"| job
```

- Each run schedules the next one, and saves when it's due.
- A scheduled message due at another time than the saved one is stale and does nothing, so changing a setting or
  restarting never leaves two waiting.
- At startup each job looks once, so a run missed while ChefBot was off happens straight away.

The rules are in [CONTRIBUTING.md](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/CONTRIBUTING.md#where-things-go), under "Rules of thumb".
