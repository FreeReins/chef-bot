# Plan styles

A plan style says what a week holds: the days you cook dinner, whether weekday lunches are leftovers, how many
breakfasts, snacks, sweets and drinks to plan, and rules for the recipes. Pick a style each time you plan, or let the
weekly plan use your default one.

Anyone signed in can change plan styles, under **Settings → Plan styles**.

## The starter styles

| Style | What it plans |
|---|---|
| **Classic week** | Four dinners with leftovers for weekday lunches, one slow cooker meal on Tuesday, breakfasts, snacks and a sweet |
| **Slow cooker week** | Three slow cooker dinners with leftovers, plus snacks and drinks |
| **No slow cooker** | Four dinners with leftovers, none in the slow cooker |
| **Quick weeknights** | A fresh dinner every weeknight, most ready in half an hour |
| **Meat-free** | Dinners with no meat |
| **Dinners only** | Just dinners |

Keep the ones you like, change them, or make your own. **Add a starter style** brings back a copy of any starter.
Until a household saves any styles, weeks are planned in **Classic week**.

## What a style holds

- **Name** and **What it is for:** shown when you choose a style.
- **Days you cook dinner.**
- **Leftover lunches:** each dinner is cooked big enough for the next days' lunches. How long leftovers keep is set
  under **Settings → Planning**.
- **Also plan:** how many breakfasts, snacks, sweets and drinks.
- **Lean on favourites:** not at all, a little (the default) or a lot. See [Favourites](#favourites).
- **Rules for the recipes.** See below.
- **Make default:** the style the weekly plan uses.

## Rules

Each rule is a limit (**at least**, **at most**, **exactly**, or **none**), a number, and what it matches:

| Matches | Example |
|---|---|
| Slow cooker recipes | "At least 1 slow cooker dinner, on Tuesday" |
| A tag | "Exactly 1 dinner tagged fish, on Friday" |
| An ingredient | "No meals made with beef or pork", "At least 2 dinners made with chicken" |
| Ready within some minutes | "At least 4 dinners ready within 30 minutes" |
| Someone's favourite | "At least 1 dinner that's someone's favourite" |
| A [cookbook](recipes.md#cookbooks) | "At least 2 dinners in the Weeknight cookbook" |

- A rule can put matching dinners on a particular day.
- Ingredient rules match whole words, so "chicken" also matches chicken thighs and chicken broth.
- **In the cookbook** is offered only once the household has a cookbook. Renaming the cookbook doesn't change the
  rule. Deleting the cookbook takes it out of the rule, and a rule left without a cookbook is removed.
- **When your recipes can't meet a rule**, say three slow cooker dinners with only one slow cooker recipe, the
  rule is relaxed for that week and the plan's notes say so. Add more recipes that match, or lower the number.

## Favourites

Anyone can heart a recipe to make it a favourite. A style that leans on favourites prefers them, and a recipe more
people have hearted counts for more.

Favourites never come round sooner than other recipes: recipes planned in the last 28 days (see
[Advanced settings](../admin/settings.md#advanced)) are still left out first. A "someone's favourite" rule gives way
for the week when all the favourites were planned recently.

## How many people

How many people each meal is for isn't part of a style. It's set once for the household, under
**Settings → Planning → People you usually cook for**. Changing it resizes weeks you haven't confirmed, with their
shopping lists. Confirmed weeks keep the number they were planned for.

## Change the starter styles for every new household

The starter styles come from `config/plan-styles.json`. To offer different ones, see
[Customising the catalogs](../admin/customising-catalogs.md).
