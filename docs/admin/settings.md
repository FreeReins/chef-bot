# Settings reference

Almost everything is set in ChefBot's web page, under **Settings**. A few things that have to be known before ChefBot
starts (the database, HTTPS, ports) are set in the environment instead. This page lists both.

## In the web page

Settings save by themselves as you change them. **Admin** means only admins see or change that section.

### Login

| Setting | Default | Who |
|---|---|---|
| **Your password:** change your own. Your other devices are signed out | | Everyone |
| **Wrong passwords in a row before an account is locked** (0 turns lockout off) | 5 | Admin |
| **Minutes an account stays locked** | 15 | Admin |

See [People and accounts](people-and-accounts.md).

### Appearance

Light, dark, or follow the device. Kept in this browser only. The sun and moon button in the header switches too.

### HTTPS

Whether you're on HTTPS, the certificate's expiry date, **Download the certificate**, the names the certificate
covers (admin), and **Always use HTTPS** (admin). See [HTTPS and certificates](https-and-certificates.md).

### Planning

Anyone signed in can change these.

| Setting | Default | Meaning |
|---|---|---|
| **People you usually cook for** | 2 | Each breakfast, lunch and dinner is planned for this many, from 1 to 20. Weeks not yet confirmed are resized, with their shopping lists. Confirmed weeks keep their number |
| **Plan next week automatically** | On | Stages next week as a draft in your default plan style |
| **On**, **At** | Saturday, 09:00 | When the weekly plan is made, in local time. The panel shows when the next one is due |
| **Leftovers keep for (days)** | 3 | A weekday lunch uses the leftovers of a dinner cooked this recently, from 0 to 7. 0 plans no leftover lunches |

### Plan styles

What a week holds. See [Plan styles](../using/plan-styles.md). Anyone signed in can change them.

### Stores (admin)

The stores the shopping list is split between. See [The shopping list](../using/shopping-list.md#stores).

### Calendar

The calendar feed's address, which everyone can copy. An admin turns it on, makes a new address, turns it off, and
sets the **Meal times**:

| Setting | Default | Meaning |
|---|---|---|
| **Breakfast**, **Lunch**, **Dinner** | 08:00, 12:30, 18:30 | When each meal starts in the calendar |
| **Each meal lasts (minutes)** | 45 | From 5 to 240 |
| **Slow cooker reminder (hours before dinner)** | 8 | From 0 to 12. 0 turns it off |

See [The meal plan in your calendar](../using/calendar.md).

### AI assistants

The MCP address, and your own tokens for AI assistants. See [AI assistants](ai-assistants.md).

### To-do apps (admin)

Where the Shopping tab can send the list: Home Assistant, Todoist or a webhook. See [To-do apps](to-do-apps.md).

### Notifications (admin)

| Setting | Default | Meaning |
|---|---|---|
| **Webhook address** | empty | Where reminders are posted. Empty turns notifications off |
| **Send daily reminders at** | 07:30 | When the daily reminders go out |
| **A new plan is ready** | On | Whenever a week is staged or replanned |
| **Start the slow cooker** | On | On the morning of a slow cooker dinner |
| **Food to use soon** | On | When pantry food is about to expire |
| **Confirm the plan or finish shopping** | On | The day before a week starts, and on its first day |
| **Today's meals** | Off | A daily summary of what's planned |
| **New achievements** | On | When the household earns a badge tier or a new rank |

See [Notifications](notifications.md).

### Backups (admin)

| Setting | Default | Meaning |
|---|---|---|
| **Back up automatically every day** | On | |
| **At** | 03:00 | |
| **Keep this many backups** | 14 | 0 keeps them all |
| **Delete backups older than this many days** | 0 | 0 keeps them whatever their age |

See [Backups and restore](backups-and-restore.md).

### Logs (admin)

The last 1000 log entries since ChefBot started, with a filter and search. See
[Monitoring and logs](monitoring-and-logs.md).

### Advanced

Admins only. ChefBot's own values suit most homes. Each group has **Reset to defaults**.

| Setting | Default | Meaning |
|---|---|---|
| **Don't repeat a recipe planned within (days)** | 28 | From 0 to 365. 0 lets a recipe come back any week |
| **Plan pantry food first when it expires within (days)** | 3 | From 0 to 30. Also when the pantry and reminders call food "expiring soon" |
| **Share ingredients between meals when they keep for (days or less)** | 21 | From 1 to 365. Plans prefer recipes that use up the same fresh food |

**Sorting imported recipes:** an imported recipe's tags, categories and tools are matched as whole words against
these lists, in this order. The first match gives its course. New words apply to recipes imported from then on.

| List | Default words |
|---|---|
| Breakfast | breakfast, brunch |
| Snack | snack, snacks, appetizer, appetizers |
| Sweet | dessert, desserts, sweet, sweets, treat, treats, cookies, cake |
| Drink | drink, drinks, beverage, beverages, cocktail, cocktails, mocktail, mocktails, smoothie, smoothies, lemonade |
| Not a meal | side, sides, side dish, sauce, condiment, bread, baking |
| Main | empty: every recipe that isn't anything else is a main. Otherwise only recipes with one of these words are |
| Slow cooker | slow cooker, crockpot, crock pot (also matched against the recipe's name) |

## Settings in the environment

These are read when ChefBot starts. Set them in `~/.config/chefbot/chefbot.env` with the Podman setup, then run
`systemctl --user restart chefbot`. With Docker Compose, put them under the ChefBot service's `environment:` and run
`docker compose up -d`. Names use `__` (two underscores) between their parts.

| Setting | Default | Meaning |
|---|---|---|
| `ConnectionStrings__ChefBot` | set by the Podman units | The PostgreSQL connection string, such as `Host=db;Port=5432;Database=chefbot;Username=chefbot;Password=…`. With Podman, the password comes from the `chefbot-db-password` secret |
| `TZ` | the host's, with Podman | Your time zone, such as `Europe/London`. Weeks, reminders and backups follow local time |
| `Https__Mode` | `SelfSigned` | `SelfSigned`, `Certificate` or `Off`. See [HTTPS](https-and-certificates.md) |
| `Https__Hostnames` | empty | Names and addresses the first self-signed certificate covers, separated by commas |
| `Https__PublicPort` | `8443` | The HTTPS port people type, when it differs from 8443 on the host |
| `Https__HttpPort`, `Https__Port` | `8080`, `8443` | The ports ChefBot listens on inside the container |
| `Https__CertificatePath`, `Https__KeyPath`, `Https__CertificatePassword` | empty | Your own certificate, in `Certificate` mode. See [Your own certificate](own-certificate.md) |
| `Https__TrustedProxies` | empty | Reverse proxies whose forwarded headers are believed. See [Behind a reverse proxy](reverse-proxy.md) |
| `Accounts__RecoverUsername` | empty | Logs a password reset link for this account at startup. See [Lost the admin password](people-and-accounts.md#lost-the-admin-password) |
| `Planner__DefaultServings` | `4` | Servings assumed for an imported recipe that doesn't say |
| `Planner__Weights__*` | see below | How strongly each thing counts when choosing a week. Rarely changed |
| `Data__IngredientCatalog`, `Data__KnownStores`, `Data__PlanStyles`, `Data__SampleRecipes` | `config/…json` | Where the catalogs are read from. Mounting your own file over the built-in one is simpler. See [Customising the catalogs](customising-catalogs.md) |
| `Data__Backups`, `Data__SessionKeys` | `backups`, `keys` | The backup and keys folders inside the container (`/app/backups`, `/app/keys`) |
| `Logging__LogLevel__Default` | `Information` | `Debug` for more detail when tracking down a problem, `Warning` for less |

### Planner weights

Points added to or taken from a candidate week. Positive is better. The planner compares whole weeks, so these only
matter relative to each other.

| Setting | Default | Counts |
|---|---|---|
| `Planner__Weights__SharedPerishable` | `3` | Per extra meal that uses the same perishable ingredient |
| `Planner__Weights__StrandedPerishable` | `-1` | A perishable only one meal uses, that isn't already in the pantry |
| `Planner__Weights__Seasonal` | `3` | A recipe whose seasonal produce is all in season (all out of season counts the negative) |
| `Planner__Weights__OnHand` | `1.5` | Per ingredient already in the pantry |
| `Planner__Weights__ExpiringSoon` | `4` | Extra, per pantry ingredient about to expire |
| `Planner__Weights__SameProtein` | `-3` | Per meal beyond two built on the same protein |
| `Planner__Weights__Favourite`, `FavouriteALot` | `2`, `6` | A favourite, for styles that lean on favourites a little or a lot |
| `Planner__Weights__MissingIngredient` | `-0.5` | Per ingredient a swap suggestion would add to the shopping list |
| `Planner__Weights__RegeneratePantryBoost` | `4` | How much more pantry food counts in **Replan from pantry** |

> [!NOTE]
> Older versions read `Planner__DinnerPortions`, `Schedule__*`, `Calendar__*`, `Classifier__*`,
> `Notifications__WebhookUrl`, `Planner__AvoidRepeatDays` and `Planner__ExpiringSoonDays` from the environment. They
> are no longer read: set them in the web page as described above, and remove them from `chefbot.env`.
