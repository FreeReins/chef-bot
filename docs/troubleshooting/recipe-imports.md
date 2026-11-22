# Recipe import problems

## A link won't import

ChefBot reads the recipe description most recipe sites include for search engines (schema.org). What its message
means:

| Message | What to do |
|---|---|
| "ChefBot couldn't find a recipe on that page" | Use the link to one recipe, not a list or a search page. Some sites have no description to read: type the recipe in |
| "ChefBot couldn't reach …" | Check the link opens in your browser and that ChefBot's machine can reach the internet |
| "Check the link works in your browser; if it does, the site may not let ChefBot in" | The site turns away programs, or needs a sign-in. Type the recipe in |
| "ChefBot can't read recipes from …" | That site isn't supported. Type the recipe in |
| "… is on a private network, and ChefBot only downloads from the public internet" | ChefBot never downloads from addresses on your own network, for safety. Use a public website's link |
| "That link is a … file, not a web page" | Use the link to the recipe's page, not to a photo or PDF |
| "That page is already in your recipes" | It was imported before. Nothing was added |

A site that doesn't work but should? A reader can be added for it: see
[Adding a recipe site](../developers/adding-a-recipe-site.md), or
[open an issue](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/issues/new/choose) with the link.

## A photo won't read

Reading photos uses the AI model set up under
[Settings → AI assistants](../admin/ai-assistants.md#ai-models-chefbot-uses). What its message means:

| Message | What to do |
|---|---|
| "This needs an AI model" | An admin sets one up under **Settings → AI assistants** |
| "ChefBot couldn't find a recipe in that photo" | Take a sharper photo with the whole recipe in view and good light, or type it in |
| "Choose up to 4 photos" | Read one recipe at a time, at most four pages |
| "That file isn't a photo ChefBot can show" | Use a JPEG, PNG, WebP or GIF image. Your browser may not be able to shrink some formats, such as HEIC, first |
| "… didn't accept the API key" | An admin enters a new key under **Settings → AI assistants**, then presses **Test** |
| "… doesn't know the model …" | Check the model's name, and for an OpenAI-compatible service that its address ends in `/v1` |
| "ChefBot couldn't reach …" | The service is down or the address is wrong. For Ollama, check it's running and listening on your network, not only on its own machine |
| "… is busy or out of credit" | Try again later, or check the account with the provider |
| "… refused the question" | The model may not accept images or JSON answers. Choose a vision model for **Reading recipe photos** |
| "… didn't answer in a form ChefBot can read" | Try again, or choose a more capable model |

Whatever was read, check it before saving: models can misread handwriting and amounts.

## No suggestions came back

- **"… had nothing to add":** the model found nothing missing. The recipe is kept as it is.
- **The suggestions no longer fit:** the recipe was edited after they were made. Press **Ask for suggestions** again.
- **The library sweep skipped some recipes:** it only asks about recipes missing a description, times, tags,
  ingredient amounts or clear steps, and not again until a recipe changes. A model that refuses or isn't set up is
  noted in **Settings → Logs**; one that is busy or unreachable is tried again later by itself.

## Photos are missing after importing from a link

Photos download in the background, after the recipe is added. Give it a minute and reload. If they still don't
appear, the site refused them, and **Settings → Logs** says so. Add your own with **Add a photo**.

## A file import skipped recipes

Recipes whose names are already in your library are skipped, so nothing is duplicated. Rename the one in ChefBot first
to import the other as well.

## A Mealie backup won't import

- Import the `.zip` exactly as Mealie made it, from the **Recipes** tab.
- The file can be up to 200 MB. Behind a reverse proxy, allow uploads that large (nginx: `client_max_body_size 200m;`).

## Recipes went into the wrong course

Change the course on the recipe. To sort future imports differently, an admin changes the words under
[Settings → Advanced](../admin/settings.md#advanced).

## Amounts look wrong on the shopping list

Each ingredient line is read as an amount, a unit and a food. A line like "salt to taste" has no amount. Edit the
recipe so each line starts with its amount, such as `2 cups kale`.
