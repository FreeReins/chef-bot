# Limits worth knowing

What ChefBot doesn't do, or does differently than you might expect.

## Security and access

- **Built for a home network.** Passwords are hashed, accounts lock after repeated wrong passwords and HTTPS is built
  in, but the certificate is self-signed and there's no two-step sign-in. Don't expose ChefBot to the internet
  without a [reverse proxy](../admin/reverse-proxy.md) and a real certificate.
- **No email.** Invite and reset links are passed along by you.
- **API tokens only work on MCP.** Scripts that use `/api` sign in with a cookie. See [The API](api.md).
- **Notifications go one way.** ChefBot posts to a webhook. Home Assistant can't call ChefBot's API by itself.

## Households

- **One household per installation.** Everyone shares the recipes, plan and pantry. There are no per-person plans or
  pantries.
- **One ChefBot per database.** Plan changes are made one at a time inside the app, so don't run two copies against
  the same database.

## Planning and shopping

- **Amounts are only compared in units that measure the same thing.** ChefBot doesn't convert cups to grams. It knows
  how much a few whole items give (lemons, limes, garlic cloves, eggs), so "2 tbsp lemon juice" counts as a lemon.
  When it can't compare, having some of an ingredient counts as having enough.
- **Seasons are guesses** from the ingredient catalog, tuned for the Kansas City area. You can
  [change them](../admin/customising-catalogs.md).
- **Stores only know what you tell them.** There are no live prices or stock.
- **Not a nutrition tracker.** ChefBot doesn't count calories or nutrients.

## Recipes

- **Recipe links need a site that describes its recipes** in schema.org data, as most recipe sites do for search
  engines. Sites without it, behind a sign-in, or that turn away programs get a message saying so. Type those recipes
  in, or [add a reader for the site](../developers/adding-a-recipe-site.md).
- **Only the public internet.** ChefBot never downloads pages or photos from addresses on your own network.
- **Reading photos and suggestions need an AI model** that an admin sets up: Claude, OpenAI, or one that answers
  OpenAI's API, such as Ollama. Up to four photos, the pages of one recipe, are read at a time. What a model reads or
  suggests can be wrong, so it is always shown for checking before it's saved.
- **PDF downloads** use US Letter pages, have no photos, and print letters outside western European alphabets as `?`.

## Running it

- **Going back to an older version needs a restore.** Upgrades change the database by themselves, but an older
  version can't undo that. See [Upgrading](../admin/upgrading.md#go-back-to-an-older-version).
- **Scheduled jobs catch up within limits.** A weekly plan due while ChefBot was stopped is made when it starts again,
  as long as its week hasn't started. Daily reminders and backups run at startup if their time has passed that day.
