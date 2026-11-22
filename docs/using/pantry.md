# The pantry

The **Pantry** tab lists the food you have, when each item expires, and which planned meals will use it. ChefBot uses
it to plan meals around what you have and to leave it off the shopping list.

## How food gets into the pantry

- **Marking shopping as bought.** Each item goes in with an expiry date, worked out from the ingredient's shelf life,
  or the date you enter from the package.
- **By hand.** Use **Add an item**, with an optional amount, unit and expiry date.

## How food leaves it

- Marking a meal **Cooked** takes its ingredients out.
- Edit an item to correct its amount or date, or remove it.

Any change to the pantry updates the shopping lists of weeks that haven't ended, a moment later.

## Expiring food

Items about to expire are flagged: within 3 days to start with (an admin can change this under
[Settings → Advanced](../admin/settings.md#advanced)). ChefBot:

- plans meals that use expiring food first;
- puts suggestions that use it first when you **Swap** a meal;
- can send a reminder about it (see [Notifications](../admin/notifications.md));
- lists what no planned meal uses yet, so you can decide what to do with it.

They're listed first, under **Use soon**: the three that expire soonest, then **Show more** for the rest (and
**Show fewer** to fold them away again). Each row there has the same buttons as the rest of the pantry: correct its
amount or date, put it on the shopping list to buy more (**+**), or remove it.

## Swap a meal to use food up

On the Plan tab, press **Swap** on any meal. ChefBot shows:

- **Recipes that use what you have,** ranked by how much of the pantry they use, favouring food that expires
  soonest. The top one is marked **Best fit**. Each says how much comes from your pantry and how much is to buy,
  and is marked **Uses up** when it would use food that expires soon. Open **What's needed** to see the
  ingredients you have and the ones you'd buy.
- **Or make up a plate:** simple plates it invents from what you have, a whole grain, a protein and a vegetable or
  fruit. On the plan, a plate you choose has a lightbulb after its name, so you can tell it apart from a recipe.

Press **Use this**. The plan, leftover lunches and shopping list update together.
