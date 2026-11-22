# Recipes

ChefBot keeps its own recipe library: ingredients, steps, prep and cook times, description, tags, notes, source link
and photos. Everyone in the household shares it. This page covers adding, finding, scaling, planning, rating and downloading recipes.

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

### From a photo

Read a recipe from a cookbook page, a recipe card or a screenshot. This needs an AI model, which an admin sets up
under [Settings → AI assistants](../admin/ai-assistants.md#ai-models-chefbot-uses). Without one, the tab says so.

1. Choose **From a photo**.
2. Under **Photos of one recipe**, take or choose up to four photos: the pages of one recipe, in order.
3. Press **Read recipe**. It can take up to a minute.
4. The recipe form opens with what was read. Check it against the photo and fix anything misread.
5. Leave **Keep the first photo with the recipe** ticked to keep the photo, and press **Save recipe**.

Nothing is saved until you press **Save recipe**. The photos are made smaller on your device before they're sent. If
a photo won't read, see [Recipe import problems](../troubleshooting/recipe-imports.md#a-photo-wont-read).

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
only**, how long a recipe takes, or **Has suggestions** (when an AI model is set up). Show them as **Cards** or a **List**.

## On a recipe

- **Start cooking:** large text, one step at a time, with timers, at the servings you chose. See
  [Cooking mode](cooking-mode.md).
- **Add to plan:** put it on a day of this week or a later one. See [Add a recipe to your plan](#add-a-recipe-to-your-plan).
- **Favourite** (the heart): plan styles can prefer favourites. See [Plan styles](plan-styles.md#favourites).
- **Reviews:** give it 1 to 5 stars and a note each time it's cooked. The average shows next to its name.
- The **⋯** menu has **Edit recipe**, **Photos**, **Improve with AI** (when an AI model is set up), **Share recipe** (or **Copy as text**), **Print recipe**, the
  downloads, and **Archive recipe**.
- **Photos:** JPEG, PNG, WebP or GIF, up to 5 MB, at most 10 per recipe. The first photo is the one shown in the
  recipe list. A recipe without a photo shows **Add a photo** where the photo goes.
- **Archive recipe:** takes it out of the library and future plans. Its history is kept.

## Make a recipe more complete

An AI model can suggest what a recipe is missing. An admin sets one up under
[Settings → AI assistants](../admin/ai-assistants.md#ai-models-chefbot-uses).

1. On the recipe, open the **⋯** menu and choose **Improve with AI**.
2. Press **Ask for suggestions**. It can take up to a minute.
3. Each suggestion shows what the recipe has **Now** next to what is **Suggested**. Untick any you don't want.
4. Press **Apply**, or **Keep the recipe as it is** to set them all aside.

The model only fills gaps. It never replaces something the recipe already has, with one exception: steps that are
short or missing can be replaced with clearer ones, if you tick that. It can suggest:

- a description, prep, cook and total times, what the recipe makes, and tags, when the recipe has none
- clearer steps, when they are short or missing
- an amount for an ingredient line that has none, such as `salt` becoming `1 tsp salt`
- ingredients the steps use that the list leaves out
- tips, added to the end of the notes

When suggestions are waiting, **Suggestions are ready to review** shows under the recipe's name. An admin can ask for
suggestions on every recipe at once: see
[Complete sparse recipes](../admin/ai-assistants.md#complete-sparse-recipes). If the recipe is edited before the
suggestions are applied, they no longer fit; ask again.

## Make it for more or fewer people

The ingredients are shown for the servings the recipe makes. To change that:

1. Above the ingredients, press **−** or **+**, or type a number into **Servings**. **½×**, **1×**, **2×** and
   **3×** jump straight to half, as written, double and triple.
2. Every amount changes to match. Cups, spoons and whole items show as kitchen fractions such as 1½ or ¾; grams and
   millilitres are rounded to easy amounts.
3. Press **Back to 4** (or whatever the recipe makes) to see it as written again.

The choice stays in the page's address, so a reload, a bookmark or a shared link keeps it, and **Start cooking**
uses it too. A line ChefBot couldn't read an amount from, such as "salt to taste", stays as written and is marked
with **\***. Check those yourself. Scaling only changes what the page shows; the recipe itself isn't changed.

## Add a recipe to your plan

1. On the recipe, press **Add to plan**.
2. Choose the **Week**: **This week**, **Next week**, or a later week that already has a plan.
3. Choose the **Meal**. It starts on the one that suits the recipe: dinner for a main, breakfast for a breakfast,
   and so on.
4. Choose the **Day**. Days that already have that meal are marked with a dot. Snacks, sweets and drinks can also go
   on **Any day**.
5. Under **How many**, keep **For the household** to cook for the number of people your plans are for, or pick
   **Choose servings** and set a number. If you scaled the recipe on its page, that number is already chosen.
6. Press **Add to** (the button names the day and meal).

If the day already has that meal, ChefBot says what is there and asks whether to **Replace it** or **Add alongside
it**. A meal already marked cooked or skipped can't be replaced, so the recipe goes alongside it.

If the week has no plan yet, ChefBot starts a draft with just this meal. You can add more by hand, or plan the rest
of the week on the **Plan** tab. See [The weekly routine](weekly-routine.md).

The meal shows on the **Plan** tab straight away. The shopping list catches up a moment later, in the background: it
adds what the recipe needs at that size and leaves out what your pantry already has. Open pages update by
themselves. See [The shopping list](shopping-list.md#whats-on-it).

A meal with chosen servings keeps them: changing how many people your plans are for, or leftover lunches, don't
resize it.

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
