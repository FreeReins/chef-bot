# What ChefBot does

ChefBot plans a week of meals for your household, then takes care of everything that follows: the shopping list,
the pantry, and changing the plan when life gets in the way. It runs on your own computer or home server, so your
recipes and plans stay with you.

## The problems it solves

- **"What's for dinner?"** ChefBot plans the whole week for you, in the style you like: which days you cook, whether
  weekday lunches are leftovers, how many breakfasts and snacks, and rules such as "no beef" or "at least two
  quick dinners".
- **Food going off in the fridge.** Plans favour recipes that share fresh ingredients, so half a bag of spinach gets
  used in a second meal. They also use up what's already in your pantry, starting with what expires soonest.
- **Writing the shopping list.** Quantities are added up and scaled to the people eating. Staples and what you
  already have are left off, and the list is split between the stores you shop at.
- **Remembering what you bought.** Ticking an item off puts it in the pantry with an expiry date.
- **Plans changing mid-week.** Swap a meal and ChefBot suggests recipes that use what you have, starting with food
  that's about to expire. The leftovers and the shopping list update to match.
- **Recipes scattered everywhere.** Keep them all in one place: type them in, paste a link to a recipe website, or
  import a Mealie backup.

## A week with ChefBot

1. **Saturday morning** ChefBot plans next week as a draft and can send you a notification. Or press **Plan**
   yourself whenever you like.
2. **You review it.** Drag meals between days, swap the ones you don't fancy, keep the ones you like and shuffle
   the rest.
3. **You confirm it.** The shopping list is ready.
4. **You shop.** Tick items off as you go, even with no signal in the shop. Everything bought goes into the pantry.
5. **You cook.** Open a recipe and press **Start cooking** for big text, one step at a time, with timers. Mark the
   meal cooked and its ingredients come out of the pantry.

[The weekly routine](../using/weekly-routine.md) goes through each step in detail.

## Who it's for

- **Households of any size.** A single person gets the same features as a family of six. Everyone in the
  household shares the same recipes, plan and pantry, and each has their own sign-in.
- **People who like to run their own services.** ChefBot is two containers: the app and its PostgreSQL database.
  It runs anywhere Docker or Podman does.
- **Home Assistant users.** Reminders go to a webhook, the shopping list can go to any Home Assistant to-do list,
  and the meal plan can appear in your calendar.

## What it is not

- Not a cloud service. Nothing is sent anywhere unless you set it up: a webhook, a to-do app or a calendar.
- Not a nutrition or calorie tracker.
- Not a store price comparison. Stores only know what you tell them.

See [Limits worth knowing](../reference/limits.md) for the full list.

## Next

[Check what you need](requirements.md), then install ChefBot.
