# AGENTS.md

ChefBot is a self-hosted .NET 10 meal planner: Wolverine HTTP API, EF Core on PostgreSQL, and a Svelte 5
page in `src/ChefBot.Web/` that Vite builds into `src/ChefBot.Api/wwwroot/`. [CONTRIBUTING.md](CONTRIBUTING.md)
has the full contributor guide; read its "Where things go" section before adding a new class.

## Check your work

```sh
dotnet build ChefBot.slnx -v q -nologo                                  # about 1s when warm
dotnet run --project tests/ChefBot.Core.Tests                           # all tests, under 1s
dotnet run --project tests/ChefBot.Core.Tests -- --treenode-filter "/*/*/ShoppingTests/*"   # one class
./dev.sh web                                                            # build the web page; exits
```

A change is done when the build has no warnings and the tests pass. A change to `src/ChefBot.Web` is done when
`./dev.sh web` builds it with no warnings.

Do not run `./dev.sh`, `./dev.sh test` or `./dev-fresh.sh`: they watch for changes and never exit. Do not use
`podman build --target test` to check work; it runs the same tests much more slowly.

## Rules the code doesn't make obvious

- `ChefBot.Core` holds both the logic and its storage and outside-system code (`Persistence/`, `Mealie/`,
  `Notifications/WebhookNotificationSender.cs`). Planning, shopping and swap logic (`WeekPlanner`,
  `ShoppingListBuilder`, `AlternativeFinder`, `CreativeMealGenerator`) take inputs as arguments and never
  read or save.
- Services load, call that logic, and save once. Endpoints and handlers bind the request, call one service and
  map the result.
- Ingredient needs come only from `MealRequirements`. Anything that changes a plan goes through `PlanRefresher`,
  so leftovers and the shopping list stay in step.
- The schema is created with `EnsureCreated` and is never upgraded. After changing an entity, tell the user to
  run `./dev.sh reset`; don't add migrations unless asked.
- The web page is Svelte 5 in plain JavaScript (no TypeScript). Its state (`app`) and the actions shared
  between screens live in `src/ChefBot.Web/src/lib/app.svelte.js`; a form's own submit stays in its component.
  Wrap anything that calls the API in `run()` so failures show as a message.
- `src/ChefBot.Api/wwwroot/` is build output. Never edit it or add it to git.

## When you change...

- **Logic in `ChefBot.Core`**: add TUnit tests beside the existing ones in `tests/ChefBot.Core.Tests/`.
  Assertions are awaited: `await Assert.That(actual).IsEqualTo(expected);`.
- **A public type**: give it an XML `<summary>` saying what it is for.
- **A user-facing message**: write a plain sentence that says what happened and what to do next.
- **A setting people should change**: add it to `deploy/podman/chefbot.env.example`.
- **A volume or port**: update the units in `deploy/podman/` and add a note to `README.md`.
- **The HTTP API**: update the API section of `README.md`.

## Reading the code cheaply

- `README.md` is for people running ChefBot. Skip it unless you are editing it.
- The web page is one component per screen: `src/ChefBot.Web/src/tabs/<Tab>.svelte` (Recipes.svelte,
  Meals.svelte, ...), `screens/` for setup and sign-in, `settings/<Section>Panel.svelte`. Open only the one
  you need.
- `node_modules/` is installed packages.
- `config/ingredients.json` is a large catalog. Search it; don't read it whole.
- `bin/`, `obj/` and `TestResults/` are build output.

## Style

File-scoped namespaces, nullable reference types, `var` where the type is obvious.

## Improving this file

To measure and tune this file with Microsoft waza, follow
[.claude/skills/tune-agents-md/SKILL.md](.claude/skills/tune-agents-md/SKILL.md).
