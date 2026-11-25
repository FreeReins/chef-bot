# Planning problems

The plan's notes, shown on the Plan tab, say when ChefBot couldn't do exactly what the style asked, and why. Most
problems come down to having too few recipes of one kind.

## "There are no recipes to plan with yet"

Add some on the **Recipes** tab: import a file, paste a link, or add sample recipes. See [Recipes](../using/recipes.md).

## "Only 2 of 4 dinners could be planned"

There aren't enough recipes of that course that fit. Add more, or plan fewer days in the plan style. Check that your
recipes have the right course: an imported recipe tagged "side" isn't planned as a dinner. See
[How recipes are sorted](../using/recipes.md#how-recipes-are-sorted-into-courses).

## "The rule … couldn't be met … so it was relaxed this week"

Your recipes can't meet a rule in the plan style, for example three slow cooker dinners with one slow cooker recipe.
Add more recipes that match, or lower the number in the rule.

- Ingredient rules match whole words, so "chicken" matches "chicken thighs" but not "chickpeas".
- Tag rules need the tag on the recipe exactly.
- "Ready within" rules need the recipe's times filled in.
- Cookbook rules need enough recipes in that cookbook. See [Cookbooks](../using/recipes.md#cookbooks).
- A note about recipes "in a cookbook that has been removed" means the rule's cookbook was deleted before the plan
  was made. ChefBot takes a deleted cookbook out of every rule, so planning the week again clears it.

## "Some repeats were allowed"

Few recipes haven't been planned recently (in the last 28 days, to start with). Add recipes, or lower **Don't repeat a
recipe planned within** under [Settings → Advanced](../admin/settings.md#advanced).

## "No breakfast recipes were found"

The style plans breakfasts, but none of your recipes is a breakfast. Add some, change a recipe's course, or set
breakfasts to 0 in the plan style.

## No leftover lunches

- The plan style has leftover lunches off.
- **Leftovers keep for** is 0 under **Settings → Planning**.
- There's no dinner recent enough before that lunch.

## Something you have is still on the shopping list

- **Different units.** ChefBot doesn't convert between volume and weight: it doesn't know 1 cup of rice in grams.
  When the pantry and the recipe use different units, having some counts as having enough. If it's still on the
  list, check the pantry item's ingredient name.
- **A different variety.** The recipe asks for shallots and the pantry has onions: varieties are bought by name. The
  line says "or onion" if you'd rather use what you have.
- **Not enough.** The week's meals need more than you have.

## "… has moved since you chose it"

The meal you chose to replace was moved, swapped or removed while **Add to plan** was open, perhaps on another
device. Close the dialog, open **Add to plan** again, and choose the day and meal from the plan as it is now.

## A recipe added from its page isn't on the shopping list yet

The shopping list catches up a moment after **Add to plan**, in the background. Open pages update by themselves. If the
list still looks out of date, reload the page. If it never catches up, check the logs for errors from
`MealAddedHandler`. See [Monitoring and logs](../admin/monitoring-and-logs.md).

## The weekly plan wasn't made

- Check **Plan next week automatically** under **Settings → Planning**, and the day and time shown as **Next**.
- Each week is planned once. If you cleared it, it stays cleared. Press **Plan** to make one.
- If ChefBot was stopped at that time, the plan is made when ChefBot starts again, as long as the week
  hasn't started.

## Plans, reminders or backups happen at the wrong time

Set `TZ` to your time zone. See [Settings in the environment](../admin/settings.md#settings-in-the-environment).
