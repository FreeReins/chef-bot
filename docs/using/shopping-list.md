# The shopping list

The **Shopping** tab shows what the week's plan needs, added up and split between your stores. It also holds anything
you add by hand. Once the plan is confirmed, it's your list for the shop.

## What's on it

- **Amounts are added up** across the week's meals and scaled to the number of people.
- **A meal with chosen servings** (added from a recipe's page with **Choose servings**) is bought for that many
  instead. A recipe is bought in half batches, so 6 servings of a recipe for 4 buys one and a half times its amounts.
- **It follows the plan by itself.** After a recipe is added from its page, or the pantry changes, the list catches
  up a moment later in the background, and open pages show the new list without a reload.
- **One line per ingredient.** "2 lemons" and "2 tbsp lemon juice" from two recipes are one **lemon** line. Where
  ChefBot knows how much one item gives (a lemon, a lime, a clove of garlic, an egg), measured amounts are turned into
  whole items and rounded up, so that line reads "lemon 3". Amounts it can't add up are listed together on the line,
  such as "1 cup + 1 bunch".
- **Varieties keep their name.** A recipe asking for shallots lists **shallot**, not onion, with "or onion" beside it
  in case your store has none. The same goes for cherry tomatoes, romaine, kidney beans, spaghetti and the like.
  Onions in the pantry don't count as shallots.
- **Staples are left off,** such as salt, pepper and oil.
- **The pantry is taken off:** what you already have isn't on the list. Amounts are only compared in the same unit,
  so having some of an ingredient in another unit counts as having enough. See
  [Limits worth knowing](../reference/limits.md).
- **Each item shows where it comes from** (to buy, pantry or staple) and which meals use it.
- **Items are grouped by area of the store** within each store, in the order most people walk a supermarket:
  **Fruit and vegetables**, **Bakery**, **Meat and seafood**, **Dairy, eggs and chilled**, **Rice, grains and pasta**,
  **Canned and dry goods**, **Oils, sauces and condiments**, **International**, **Spices and baking**, **Frozen**,
  **Drinks**, then **Everything else** for anything ChefBot doesn't know. Canned tomatoes are under
  **Canned and dry goods**, for example. Copying, sharing and printing keep the same groups, and the CSV has an
  **Area** column, plus **Also** for amounts that couldn't be added up and **Or instead** for a variety's general kind.

## Shop

- Tick each item as you buy it, or press **Mark all as bought**. Before the plan is confirmed, that button only marks
  the items you added by hand.
- When the list covers more than one store, each store's card has its own **Mark all as bought**, for when you're
  done at one shop and still have another to go.
- Bought items go into the pantry with an expiry date.
- The list works with no signal. See [Offline on your phone](offline-on-your-phone.md).
- **Copy list**, **Share list**, **Print** and **Download CSV** give the list to other apps or paper.

## Add things by hand

Use **Add to the list** for anything else: milk, paper towels, whatever you need. An amount and a unit are optional.
Adding the same thing again adds to its amount, or lists the new amount beside it when the units can't be added.
Once the plan is confirmed, something you added by hand that the plan needs too shares the plan's line, and ticking
it buys both. Hand-added items need no plan. Remove them when you change your mind.

## Stores

An admin chooses the household's stores under **Settings → Stores**, or in setup. Each item goes to:

1. the first store that lists its ingredient;
2. otherwise, for in-season produce, a seasonal market while it's open (a farmers market from May to October, for
   example);
3. otherwise your main store.

Items with no store have a card for each area of the store instead.

To buy an ingredient somewhere else from now on, use the menu beside the item and move it to another store. Later
lists remember it.

## Send it to a to-do app

If an admin has set up a to-do app (see [To-do apps](../admin/to-do-apps.md)), press **Send to** with its name. What's
left to buy is added as one item per thing, such as "fish fillets, 600 g", with its store and meals.

- Nothing is ticked off in ChefBot when you send.
- Sending again adds the items again, so send once you've finished changing the plan.

## If something goes wrong

- An item you have is still on the list: check the pantry item's unit. See
  [Planning problems](../troubleshooting/planning.md).
- Sending to a to-do app fails: see
  [Notifications and integrations](../troubleshooting/notifications-and-integrations.md).
