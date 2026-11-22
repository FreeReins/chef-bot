# The HTTP API

Everything the web page does goes through a JSON API, so anything ChefBot does can be automated: from a script, Home
Assistant, n8n or your own app. This page lists every call.

For AI assistants, use [MCP](../admin/ai-assistants.md) instead: it takes tokens and offers the same actions as tools.

## Try it

ChefBot describes its API at `/openapi/v1.json`, and `/docs` shows it as a Swagger page where you can try each call.
Both need you to be signed in. In ChefBot, **Settings → AI assistants** links there too.

## Calls

The web page is a thin client over a JSON API, so anything it does can be automated.
The full description is served at `/openapi/v1.json`, and `/docs` shows it as a Swagger page where you can
try each call. Both need you to be signed in.

| Method and path | What it does |
|---|---|
| `POST /api/plans/stage` | Plan a week as a draft. Body `{}` for the upcoming week in the default style, or `{"weekStart": "2026-11-08", "styleId": "...", "keep": ["<mealId>"], "replace": true}`: `keep` carries meals over from the week's current plan, as Shuffle does |
| `GET /api/plans` | The weeks with a plan that haven't ended, earliest first: `id`, `weekStart`, `weekEnd`, `status`, `styleName` |
| `GET /api/plans/current`, `GET /api/plans/{planId}` | The plan for this week (or else the next upcoming one), or a plan by id |
| `POST /api/plans/{planId}/confirm` | Make a staged plan the working plan |
| `GET /api/plans/{planId}/ingredients` | Everything the plan needs, plus the pantry with the meals that use each item (`usedFor`) |
| `GET /api/plans/{planId}/meals/{mealId}/alternatives` | Recipe and creative suggestions for one meal |
| `POST /api/plans/meals` | Add a recipe to a week, as **Add to plan** on a recipe's page does: `{"recipeId": "...", "weekStart": "2026-11-22", "date": "2026-11-25", "slot": "Dinner", "servings": 6, "replaces": null}`. `weekStart` is a Sunday and `date` a day of that week; `date` may be null for a `Snack`, `Sweet` or `Drink`. `servings` (1 to 50) fixes the size, so changing the number of people or leftover lunches doesn't resize it; null cooks for the plan's people (or as written, for snacks, sweets and drinks). `replaces` is a meal on the plan, in that slot and day, to swap for the recipe; null adds it alongside. A week with no plan gets a staged one holding just this meal. Answers the plan at once; its leftover lunches and shopping list catch up a moment later in the background (the `MealAdded` event), and `/api/live` sends `plan` when they have |
| `PUT /api/plans/{planId}/meals/{mealId}` | Replace a meal: `{"recipeId": "..."}` or `{"custom": {"title": "...", "ingredients": ["..."]}}` |
| `POST /api/plans/{planId}/meals/{mealId}/move` | Move a dinner or breakfast to another day of its week: `{"date": "2026-11-11"}`. A meal of the same kind already there swaps with it; leftover lunches and the shopping list follow |
| `POST /api/plans/{planId}/meals/{mealId}/status` | `{"status": "Cooked"}`, `"Skipped"` or `"Planned"` |
| `POST /api/plans/{planId}/shopping/{itemId}/purchase` | Mark bought. Body `{}` or `{"expiresOn": "2026-11-20", "quantity": 2}` |
| `POST /api/plans/{planId}/shopping/purchase-all` | Mark everything bought, including the items added by hand. Body `{}` |
| `POST /api/plans/{planId}/shopping/{itemId}/store` | Buy this ingredient at another store from now on: `{"store": "Costco"}`, or `{"store": ""}` for no particular store |
| `GET` `POST /api/shopping/items` | The items added to the shopping list by hand, which need no plan, or add one: `{"name": "milk", "quantity": 2, "unit": "l"}` (amount and unit optional; adding the same thing again adds to its amount) |
| `POST /api/shopping/items/{itemId}/purchase`, `POST /api/shopping/items/purchase-all`, `DELETE /api/shopping/items/{itemId}` | Mark a hand-added item (or all of them) bought, which puts it in the pantry, or take it off the list. Body `{}` or, for one item, `{"expiresOn": "2026-11-20", "quantity": 2}` |
| `POST /api/shopping/items/{itemId}/store` | Buy a hand-added item's ingredient at another store from now on, as for a plan's item |
| `GET /api/shopping/targets`, `PUT /api/shopping/targets` | The to-do apps the shopping list can be sent to: `{"id", "name", "kind", "url", "list", "hasToken"}`, with `kind` one of `HomeAssistant`, `Todoist` or `Webhook`. Tokens are never sent back. `PUT` (admin) replaces them all: `{"targets": [{"id": null, "name": "Kitchen list", "kind": "HomeAssistant", "url": "http://homeassistant.local:8123", "list": "todo.shopping_list", "token": "..."}]}`; a target sent with `"token": null` keeps its saved token |
| `POST /api/shopping/targets/{targetId}/test` | Add one test item to a to-do app (admin). Answers `{"sent", "total", "problem"}` |
| `GET /api/calendar/feed` | The calendar feed's address, `{"url", "createdAt"}`, or `{"url": null}` when it is off |
| `POST` `DELETE /api/calendar/feed` | Turn the calendar feed on with a new address (the old one stops working), or off (admin) |
| `GET /api/calendar/{token}/meals.ics` | The calendar feed as iCalendar text: each breakfast, lunch and dinner of the confirmed weeks from last week on, at the times set under **Settings → Calendar** (`/api/calendar/times`). Needs no sign-in, only the secret in the address. 404 when the feed is off or the secret is wrong |
| `POST /api/shopping/send` | Add what is left to buy to a to-do app: `{"targetId": "...", "planId": "..."}`, the plan's items not yet bought plus the hand-added ones (only those without `planId`). Nothing is ticked off in ChefBot, and sending again adds the items again. Answers `{"sent", "total", "problem"}`; items go one at a time, so a problem stops the rest |
| `GET /api/stores`, `PUT /api/stores` | The household's stores, the known stores and the ingredient names they can list. `PUT` replaces the stores (admin) |
| `GET /api/plan-styles`, `PUT /api/plan-styles` | The household's plan styles, the starter styles, and the recipe tags and ingredient names rules can use. `PUT` replaces the styles: `{"styles": [{"name": "Crockpot week", "isDefault": true, "cookingDays": ["Sunday", "Wednesday"], "leftoverLunches": true, "breakfasts": 0, "snacks": 2, "sweets": 0, "drinks": 1, "rules": [{"limit": "AtLeast", "count": 2, "slot": "Dinner", "match": "SlowCooker"}]}]}`. A rule's `limit` is `AtLeast`, `AtMost`, `Exactly` or `None`; `match` is `SlowCooker`, `Tag` or `Ingredient` (with `values`) or `ReadyWithin` (with `minutes`), or `Favourite` (someone's favourite; recipes planned recently don't count towards it); `day` puts matching dinners on a day first. A style's `favourites` is `Off`, `ALittle` (the default) or `ALot`: how much favourite recipes are preferred, more so the more people have them as a favourite, but never ahead of avoiding recent meals |
| `GET` `PUT /api/planning` | How many people plans are for, the weekly schedule and how long leftovers keep: `{"people": 2, "scheduleEnabled": true, "scheduleDay": "Saturday", "scheduleTime": "09:00", "maxLeftoverDays": 3}`. People from 1 to 20, leftovers from 0 to 7 days. `GET` adds `nextStaging` and `isSaved`. Weeks not yet confirmed are resized. Anyone signed in can change them |
| `GET` `PUT /api/calendar/times` | When meals appear in the calendar feed: `{"breakfastTime": "08:00", "lunchTime": "12:30", "dinnerTime": "18:30", "mealMinutes": 45, "slowCookerHours": 8}`. `PUT` is admin only |
| `GET` `PUT /api/system/advanced` | The advanced planning settings, with their defaults (admin): `{"avoidRepeatDays": 28, "expiringSoonDays": 3, "perishableWithinDays": 21, "words": {"breakfast": [...], "snack": [...], "sweet": [...], "drink": [...], "slowCooker": [...], "notAMeal": [...], "main": []}}`. A value left out, or null, goes back to its default |
| `GET /api/account/link` | What an invite or password reset link is for, before showing its form |
| `POST /api/plans/{planId}/refresh` | Rebuild a plan's shopping list from the pantry, as happens by itself a moment after the pantry changes |
| `GET` `POST /api/pantry`, `PATCH` `DELETE /api/pantry/{id}` | Read and correct the pantry |
| `GET` `POST /api/recipes`, `PUT` `DELETE /api/recipes/{id}` | Manage local recipes. A recipe has `name`, `course`, `servings`, `isSlowCooker` and `ingredients` (lines such as `"2 cups kale"`), and optionally `description`, `instructions` (a list of steps), `prepMinutes`, `cookMinutes`, `totalMinutes`, `sourceUrl`, `author`, `yieldText`, `notes` and `tags`. Responses add `images`, `ingredientDetails` (each line's quantity, unit and food) and `stepDetails` (for each step: the `timers` it gives in seconds, a `headStart` such as preheating the oven, boiling water or a long wait, and the positions of the `ingredients` it names) and `favouredBy`: the people who have it as a favourite, each `{"userId", "name"}`, oldest first, and `rating`: `{"average", "count"}`, the average stars of its reviews to one decimal place, or null when it has none, and `hasSuggestion`: true when AI suggestions for it are waiting |
| `GET /api/recipes/search?query=chicken&course=Main&page=1&pageSize=25` | One page of the library, in course order and then by name. Every parameter is optional: `query` keeps recipes whose name, ingredients or tags contain each of its words (any case), `pageSize` defaults to 25 (at most 100). `tag` (repeat it for several) keeps recipes with every tag given, `favourites=true` anyone's favourites, `favouredBy=<user id>` one person's, `slowCooker=true` slow cooker recipes, `readyWithin=30` recipes that say they are ready within that many minutes, and `withSuggestions=true` recipes with AI suggestions waiting. Answers `{"recipes": [...], "total", "page", "pageSize", "librarySize"}`, where each recipe has only `id`, `name`, `course`, `isSlowCooker`, `ingredients`, `tags`, `coverUrl` (its first photo, or null), `favouredBy`, `rating` and `hasSuggestion` |
| `GET /api/history?before=2026-05-01&months=6` | The household's food history, newest month first: recipes added and retired, cooking (with debuts, fan favourites, comebacks, milestone dinners and someone's favourite being cooked picked out), reviews, big shopping hauls and people joining. Both parameters are optional; `months` defaults to 6 (at most 24). Answers `{"chapters": [...], "olderBefore", "since", "totalDinners"}`, where each moment has an `at` timestamp in the household's time zone (midnight when only the day was recorded), and a cook has `favouredBy`: whose favourite the recipe was when it was cooked, and a `Reviewed` moment has `review`: `{"stars", "notes", "reviewedBy"}`; pass `olderBefore` as `before` for older months |
| `GET /api/achievements` | The household's kitchen achievements: `{"points", "rank", "rankPoints", "nextRankPoints", "starter": [...], "badges": [...], "unseen": [...]}`. Each achievement is `{"kind", "tier", "tiers", "progress", "goal", "optional"}`: `tier` 0 is not reached yet, `goal` is what `progress` has to reach for the next tier (null at the last), and an `optional` first step (inviting someone) earns no points. Ranks run from 0 to 5. `unseen` lists what was reached that the signed-in person hasn't been shown: `{"id", "kind", "tier", "earnedAt"}`, with `kind` null for a new rank (`tier` is then the rank) |
| `POST /api/achievements/seen` | `{"upTo": "<id from unseen>"}`: the signed-in person was shown the new achievements up to that one, on any device |
| `GET /api/live` | Server-sent events naming what changed, by anyone: `plan`, `shopping`, `pantry`, `recipes`, `achievements`, `backups` or `history` (and a `ping` every 25 seconds). Only the name is sent; load that part again with the API. After reconnecting, load everything |
| `GET /api/recipes/filters` | What the library can be filtered by: `{"tags": [{"name", "recipes"}], "people": [{"userId", "name"}]}`: every tag once with how many recipes have it, most used first, and the people who have favourites |
| `GET /api/recipes/{id}` | One recipe in full. 404 when it doesn't exist or has been archived |
| `GET /api/recipes/{id}/export?format=json` | One recipe as a file to download, named after it. `format` is optional: `json` (the default) gives a schema.org JSON-LD file (`.json`, `application/ld+json`), which Mealie, Tandoor and other recipe apps import, as does ChefBot's own import. It adds `course`, `isSlowCooker` and `notes`, which schema.org has no word for, and lists photos only when they came from the web, by their original address. `pdf` gives a PDF to print or keep (`.pdf`): text only, without photos, on US Letter pages; letters outside western European alphabets print as `?`. `markdown` gives Markdown (`.md`) and `text` plain text (`.txt`), both UTF-8. A format it doesn't know gives JSON-LD. 404 when the recipe doesn't exist or has been archived |
| `GET /api/recipes/export?format=json` | Every recipe that can be planned, in name order, in one file named `chefbot-recipes-<date>`. `format` works as above: JSON-LD lists the recipes as a `@graph`, the PDF starts each recipe on a new page, and Markdown and text put a rule between them |
| `PUT` `DELETE /api/recipes/{id}/favourite` | Add the recipe to the signed-in person's favourites, or take it off. Doing either twice changes nothing. Answers the recipe as in a search, with its `favouredBy`. 404 when the recipe doesn't exist or has been archived |
| `GET` `POST /api/recipes/{id}/reviews`, `PUT` `DELETE /api/recipes/{id}/reviews/{reviewId}` | A recipe's reviews. `POST` adds the signed-in person's review, `{"stars": 4, "notes": "Less salt next time."}`, with stars from 1 to 5 and optional notes of up to 2000 characters. Anyone can review a recipe as often as they like. `PUT` changes your own review, and `DELETE` removes it; an admin can delete anyone's. Each answers `{"rating": {"average", "count"}, "reviews": [{"id", "userId", "reviewedBy", "stars", "notes", "at", "edited"}]}`, newest first. 403 for someone else's review, 404 when the recipe doesn't exist or has been archived |
| `POST /api/recipes/{id}/images` | Add a photo: `{"data": "<base64>"}`, a JPEG, PNG, WebP or GIF of up to 5 MB, at most 10 per recipe |
| `GET` `DELETE /api/recipes/{id}/images/{imageId}` | Get a photo as an image file, or remove it |
| `POST /api/recipes/import/file` | Import a recipe file: `{"fileName": "recipes.json", "content": "..."}`. Names already in the library are skipped |
| `POST /api/recipes/import/archive?fileName=backup.zip` | Import a Mealie backup .zip, sent as the raw body (`Content-Type: application/zip`, up to 200 MB), with its photos. Names already in the library are skipped, but one with no photos gets the backup's. The answer adds `photosAdded` |
| `GET /api/recipes/import/formats` | The kinds of recipe file that can be imported, with their file name endings |
| `POST /api/recipes/import/web` | Add the recipe on a web page: `{"url": "https://example.com/recipes/soup"}`. Answers `{"recipe": {...}, "readWith": "Any site (schema.org)"}` naming the reader used. Photos download in the background. 409 when that page or name is already in the library, 502 when the page can't be downloaded |
| `POST /api/recipes/import/photo` | Read a recipe from photos with the AI model set up for **Reading recipe photos**: `{"photos": ["<base64>"]}`, one to four pages of one recipe, each a JPEG, PNG, WebP or GIF of up to 5 MB. Nothing is saved: answers `{"draft": {...}, "readWith": "Claude"}`, where `draft` has the fields `POST /api/recipes` takes. 400 when the photos can't be used, no AI model is set up or no recipe could be read, 502 when the model can't be reached or refuses |
| `POST` `GET` `DELETE /api/recipes/{id}/suggestion` | Ask the AI model set up for **Completing recipes** what a recipe is missing (`POST`, which replaces earlier suggestions), see the suggestions (`GET`, 404 when there are none), or set them aside (`DELETE`). Answers `{"recipeId", "modelName", "createdAt", "status", "stale", "changes": [{"id", "field", "current", "suggested", "line"}]}`. `status` is `Waiting`, `Applied`, `Dismissed` or `NothingToAdd`; `stale` is true when the recipe changed since. `field` is `Description`, `YieldText`, `PrepMinutes`, `CookMinutes`, `TotalMinutes`, `Tags`, `Instructions`, `IngredientAmount` (with `line`, from 0), `NewIngredient` or `Notes` (added to the end); `current` and `suggested` are lists of text. The recipe doesn't change. 400 when no AI model is set up, 502 when the model can't be reached or refuses |
| `POST /api/recipes/{id}/suggestion/apply` | Make the chosen changes: `{"changeIds": [1, 3]}`. Answers the recipe. 409 when the recipe changed after the suggestions were made, 404 when they were already applied or set aside |
| `GET` `POST /api/ai/sweeps/recipe-details` | The library sweep (admin): `GET` answers `{"sparse", "waiting", "modelName"}`; `POST` queues every recipe missing a description, times, tags, ingredient amounts or clear steps that has no suggestions for how it is now, and answers `{"queued", "modelName"}`. Suggestions are made one recipe at a time in the background, and `/api/live` sends `recipes` as each is ready |
| `GET` `PUT /api/ai/settings` | The AI models ChefBot uses (admin): `{"models": [{"id", "name", "provider", "model", "endpoint", "hasApiKey"}], "defaultModel": "Claude", "tasks": [{"task", "label", "description", "model"}]}`. `provider` is `Anthropic` or `OpenAiCompatible`; `task` is `RecipeFromPhoto` or `RecipeDetails`, and its `model` is null for the default. `PUT` replaces them all: `{"models": [{"id": null, "name": "Claude", "provider": "Anthropic", "model": "claude-opus-5-5", "endpoint": "", "apiKey": "..."}], "defaultModel": "Claude", "tasks": [{"task": "RecipeFromPhoto", "model": "Ollama"}]}`; a model sent with `"apiKey": null` keeps its saved key, and keys are never sent back |
| `POST /api/ai/models/{id}/test` | Ask a saved model a short question (admin). Answers `{"worked", "problem"}` |
| `GET /api/ai/features` | Which AI features have a model set up: `{"recipeFromPhoto", "recipeDetails"}` |
| `GET /api/session` | Whether setup is needed and who is signed in |
| `POST /api/setup/account`, `POST /api/account/sign-in`, `POST /api/account/sign-out` | First account, signing in and out |
| `GET` `POST /api/setup/samples` | Review the sample recipes (with their description and total time) and add chosen ones by name (admin) |
| `POST /api/invites`, `POST /api/users/{id}/password-reset` | Create an invite or a password reset link (admin) |
| `GET /api/links`, `POST /api/links/{id}/regenerate`, `DELETE /api/links/{id}` | List, replace or revoke unused links (admin) |
| `POST /api/account/accept-invite`, `POST /api/account/reset-password` | Use an invite or reset link |
| `POST /api/account/password` | Change your own password |
| `GET` `POST /api/tokens`, `DELETE /api/tokens/{id}` | List, make (`{"name": "Claude Code"}`, answers with the `token` once) or revoke your own API tokens for AI assistants |
| `GET /api/users`, `DELETE /api/users/{id}`, `POST /api/users/{id}/unlock` | See, remove and unlock accounts (admin) |
| `GET` `PUT /api/system/security` | Lockout rules and "always use HTTPS" (admin) |
| `GET /api/system/https`, `GET` `POST /api/system/https/certificate` | HTTPS status, download or recreate the certificate |
| `POST /api/plans/{planId}/regenerate`, `DELETE /api/plans/{planId}` | Rebuild a plan around the pantry in the style it was made in, or clear it |
| `GET` `PUT /api/system/notifications`, `POST .../test`, `POST .../send-today` | Reminder settings, and sending now (admin) |
| `GET` `PUT` `POST /api/system/backups` | Backup settings and list, or back up now (admin) |
| `GET` `DELETE /api/system/backups/{name}`, `GET .../{name}/summary`, `POST .../{name}/restore` | Download, delete, see what it holds or restore a backup (admin) |
| `GET /api/system/logs` | The last 1000 log entries since startup; `?after={sequence}` returns only newer ones (admin) |

Everything except the session, setup-account, sign-in, invite and reset endpoints needs a signed-in session cookie.
Endpoints marked *admin* answer `403` with the detail "Only an admin can do that." for other accounts.
Changing the pantry (`POST`, `PATCH` or `DELETE /api/pantry`) also updates the shopping lists of plans whose week
hasn't ended, a moment later, as `POST /api/plans/{planId}/refresh` does.
For scripts, sign in first and keep the cookie:

```sh
curl -c cookies.txt -X POST localhost:8080/api/account/sign-in -H 'Content-Type: application/json' \
  -d '{"username": "jamie", "password": "..."}'
```

API tokens only work on the MCP endpoint (see [AI assistants](../admin/ai-assistants.md)), not on `/api`.

Errors come back as [problem details](https://www.rfc-editor.org/rfc/rfc9457) with a readable `detail`.

Example:

```sh
curl -b cookies.txt -X POST localhost:8080/api/plans/stage -H 'Content-Type: application/json' -d '{}'
```
