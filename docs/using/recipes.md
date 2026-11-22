# Recipes

ChefBot keeps its own recipe library: ingredients, steps, prep and cook times, description, tags, notes, source link
and photos. Everyone in the household shares it. This page covers adding, finding, rating and downloading recipes.

## Add a recipe

On the **Recipes** tab, press **Add recipe** and choose how:

### From a link

1. Copy the address of a recipe page from your browser.
2. Choose **Add from a link**, paste it into **Address of a recipe page**, and press **Add**.

ChefBot reads the recipe from the page and downloads its photos in the background, so they still show if the site
goes away later. Most recipe sites work, because they describe their recipes for search engines in a standard way
(schema.org). If a site doesn't, ChefBot says so. See [Recipe import problems](../troubleshooting/recipe-imports.md).

### Type one in

Choose **Type one in**. Write each ingredient on its own line, such as `2 cups kale` or `1 onion, diced`. ChefBot
works out the amount, unit and food from each line, so it can add up the shopping list.

### Import files from another app

Choose **Import files** and pick one or more files:

| File | From |
|---|---|
| A Mealie backup `.zip` | Mealie: **Settings → Backups**. Photos come along |
| `database.json` from a Mealie backup | Mealie, without photos |
| A Mealie recipe export | Mealie |
| schema.org recipe JSON | Websites, Tandoor and other apps |
| ChefBot JSON | ChefBot, or a file you made by hand |

Recipes whose names are already in your library are skipped, so you can import a newer backup and only get what's
new. A skipped recipe that has no photos gets the backup's photos.

### From the samples

**Browse sample recipes** lists 25 sample recipes. Tick the ones you want.

## How recipes are sorted into courses

Each recipe is a main, breakfast, snack, sweet, drink, or **not a meal** (sides, sauces and the like, which are
never planned on their own). Imported recipes are sorted by their tags, categories and tools: a recipe tagged
"dessert" becomes a sweet, and one tagged "slow cooker" is marked as made in a slow cooker. Change the course on the
recipe itself at any time. An admin can change the words used for sorting under
[Settings → Advanced](../admin/settings.md#advanced).

## Find a recipe

Search by name, ingredient or tag. Filter by course, tags, **My favourites**, **Anyone's favourites**, **Slow cooker
only**, or how long a recipe takes. Show them as **Cards** or a **List**.

## On a recipe

- **Start cooking:** large text, one step at a time, with timers. See [Cooking mode](cooking-mode.md).
- **Favourite** (the heart): plan styles can prefer favourites. See [Plan styles](plan-styles.md#favourites).
- **Reviews:** give it 1 to 5 stars and a note each time it's cooked. The average shows next to its name.
- **Add a photo:** JPEG, PNG, WebP or GIF, up to 5 MB, at most 10 per recipe. The first photo is the one shown in the
  recipe list.
- **Edit**, **Print recipe**, **Copy as text**, **Share recipe**.
- **Archive recipe:** takes it out of the library and future plans. Its history is kept.

## Download recipes

From a recipe, or with **Download all** on the Recipes tab, download recipes as:

| Format | Good for |
|---|---|
| JSON (schema.org) | Importing into Mealie, Tandoor, another ChefBot, or most recipe apps |
| PDF | Printing or keeping. Text only, on US Letter pages |
| Markdown | Notes apps such as Obsidian |
| Plain text | Anywhere |

## Change the ingredient list

ChefBot knows ingredients from its catalog: each one's group, shelf life and season. See
[Customising the catalogs](../admin/customising-catalogs.md).
