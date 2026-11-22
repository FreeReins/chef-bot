# Adding a recipe site

This page is for developers. To ask for a site instead, [open an issue](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/issues/new/choose)
with a link to one of its recipes.

`SchemaOrgPage` reads the JSON-LD or microdata most recipe sites include for search engines, so most sites need
nothing. Add a reader when a site's data is missing or wrong: ingredients run together, the servings are
missing, or the recipe is only in the HTML.

1. Save the page in `tests/ChefBot.Core.Tests/Pages/<site>.html`, trimmed to the parts the reader looks at:
   the JSON-LD script and the HTML around the recipe. Leave out the site's own text, photos and scripts.
2. Write `src/ChefBot.Core/Engines/Transformation/Sites/<Site>Page.cs`, implementing `IRecipePageReader`:
   - `Name` is shown after an import ("read with: ..."), so use the site's name.
   - `Handles` matches the site's host, including subdomains such as `www.`.
   - `PageToFetch` is optional: return the site's print or AMP version when it is easier to read.
   - `Read` gets an AngleSharp `IDocument`. Start from `SchemaRecipe.FromPage(page)` and fix only what the
     site gets wrong, with `recipe with { ... }` and `page.QuerySelectorAll(...)`. Pass text taken from the
     HTML through `SchemaRecipe.PageText`. Return null when there is no recipe, so the person is told so.
   - It is a reader, not an engine: take `SchemaRecipe` in the constructor and nothing else.
3. Register it in `CoreRegistration.cs` before `SchemaOrgPage`.
4. Add a test in `WebRecipeTests.cs` that reads the saved page and checks the fields the reader fixes.
   `FakeSitePage` in the tests shows the shape.
