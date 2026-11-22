# Contributing to ChefBot

Thanks for helping. This page covers how the code is organized and what a change needs.

## Build and test

Everything runs in containers, so only Podman is required (`docker build` works the same for the image):

```sh
podman build --target test .      # restore, build and run the tests
deploy/podman/install.sh          # build the image and run the app and its database as Quadlet services
deploy/podman/uninstall.sh        # stop and remove them again, keeping the data (--delete-data removes it)
```

The Quadlet units in `deploy/podman/` are the supported way to run ChefBot. A new setting that people should
be able to change goes in `chefbot.env.example`; a new volume or port goes in the units, with a note in the
README.

To see changes as you make them, with the .NET 10 SDK and Podman installed:

```sh
./dev.sh             # dev database in a container, app on this machine with hot reload
./dev.sh test        # rerun the tests on every save
./dev-fresh.sh       # like ./dev.sh, but deletes the dev database first, so setup runs again
```

The dev copy uses its own database (`chefbot-dev-db`) and never touches an installed ChefBot. It runs on
<http://localhost:8080> and <https://localhost:8443> with its own self-signed certificate, or on 5080 and 5443
when those ports are taken. The web page also runs on <http://localhost:5173>, where it reloads as you save;
use that address while working on the page. `./dev.sh help` lists the other commands. The schema
is only created, never upgraded, so after changing an entity run `./dev.sh reset`.

With the .NET 10 SDK installed you can also work directly:

```sh
dotnet build ChefBot.slnx
dotnet run --project tests/ChefBot.Core.Tests     # TUnit tests are an executable
dotnet test --solution ChefBot.slnx               # the same tests through dotnet test
```

## Where things go

| Project | Holds | May depend on |
|---|---|---|
| `ChefBot.Core` | Everything below the API, in layers (see below). Only `Managers/Contracts/` and `CoreRegistration` are public | ASP.NET Core, Npgsql EF Core, Wolverine's EF Core and PostgreSQL storage |
| `ChefBot.Api` | The clients of the core: Wolverine HTTP endpoints (`Endpoints/`), message handlers and the timer that queues the scheduled jobs (`Messaging/`), the MCP server's tools (`Mcp/`, each calling one manager method like an endpoint), sign-in (cookies, and API tokens for `/mcp`), HTTPS (`Https/`, with its own request and response records, since HTTPS belongs to the host rather than the core), startup wiring; serves the web page | `ChefBot.Core`'s public contracts |
| `ChefBot.Web` | The web page: Svelte 5 components built with Vite into `ChefBot.Api/wwwroot` | The HTTP API |

`ChefBot.Core` is split by what changes for different reasons. Calls only go down; nothing calls sideways:

```
Clients (Api endpoints and message handlers)     see only Managers/Contracts/
   ▼
Managers/          one per area of use: orchestrate a use case, own the transaction, publish messages
   ▼          ▼
Engines/  ─►  ResourceAccess/   engines do the work (verbs); accessors read and save
                 ▼
            PostgreSQL (EF Core), the webhook, recipe pages and photos on the web, the backup folder,
            config/sample-recipes.json, config/stores.json, config/plan-styles.json

Utilities/   the ingredient catalog, text and units, clock, settings options, password hashing: any layer
```

Each layer keeps what its callers use in its own `Contracts/` folder: `Contracts/Services/` holds the interfaces
that are its entry points, and `Contracts/Messages/` the plain records and enums passed through them. Only the
managers' contracts are public; the engines' and accessors' are internal.

| Folder | Holds | Rules |
|---|---|---|
| `Managers/Contracts/Services/` | The public manager interfaces: `IPlanManager`, `IPantryManager`, `IRecipeManager`, `IAccountManager`, `IMaintenanceManager` | Clients depend on these and nothing else in Core |
| `Managers/Contracts/Messages/` | The managers' requests and responses (these are the HTTP JSON bodies), the commands and events sent through the message bus, and the enums they use | Plain records. Changing one changes the API |
| `Managers/Contracts/` | `ChefBotException` and the exceptions built on it (`NotFoundException`, `RuleViolationException`, ...), which the API turns into problem responses; `ContractJsonContext` | |
| `Managers/` | The manager classes and `Mapping.cs` (data contracts to messages) | Call engines and accessors, never another manager. Commit once through `IMessageAccess.CommitAsync`, which also sends what they published |
| `Engines/` | `PlanningEngine`, `ShoppingEngine`, `SuggestionEngine`, `TransformationEngine` (recipe files, recipe web pages, typed-in recipes and pantry items), `ValidationEngine`, `SecurityEngine`, `NotificationEngine`, the recipe file formats and web page readers (`Transformation/`, site-specific readers in `Transformation/Sites/`), the `SchemaRecipe` and `Microdata` helpers they share, and the calculations several of them use (`MealRequirements`, `PantrySnapshot`, `PantryFreshness`, `WeekScorer`, `WeekSelector`, `RecipeMatcher`, `CookingSteps`). Those calculations are plain functions and value types, not engines, so using one doesn't make one engine call another | Never call a manager or another engine. Only `ValidationEngine` reads through accessors, for rules that depend on what is stored; the others take everything as arguments and can be tested without a database |
| `Engines/Contracts/Services/` | `IRecipeFileFormat`, which each kind of recipe file implements, and `IRecipePageReader`, which each way of reading recipe web pages implements | |
| `Engines/Contracts/Messages/` | The records engines take and return: `PlanRequest`, `PlanDraft`, `ShoppingDraft`, `Suggestions`, `RequiredIngredient`, ... | Plain records |
| `ResourceAccess/Contracts/Services/` | The accessor interfaces: `IRecipeAccess`, `IPlanAccess`, ..., `IMessageAccess` (the Wolverine outbox that publishes messages and commits) | |
| `ResourceAccess/Contracts/Messages/` | The plain data contracts the accessors take and return (`Recipe`, `MealPlan`, `PantryItem`, ...) | Plain data, no EF Core |
| `ResourceAccess/` | The accessor implementations, and the EF Core entities and `ChefBotDbContext` (`Persistence/`) | Return copies, never tracked entities. Never call each other. The entities don't leave this folder. The database accessors read through the change tracker, so a row saved earlier in the same request is seen before the commit. Backups are the entities serialized as JSON, so renaming an entity property changes the backup format: keep old names readable |
| `Utilities/` | `IngredientCatalog`, `TextNormalizer`, `Units`, `WeekLayout`, `DailyTime`, the settings options, `PlanWriteLock` | Depend on no layer |

`tests/ChefBot.Core.Tests/ArchitectureTests.cs` checks these rules, so a call in the wrong direction fails the
tests.

Rules of thumb:

- An endpoint or handler binds the request, calls one manager method and returns what it gives back. Admin-only
  endpoints carry `[Authorize(Policy = Sessions.AdminPolicy)]`; nothing checks for admins inside the core.
- A manager loads through accessors, hands the data to engines, saves through accessors and commits once.
  Managers that need to react to each other do it with messages: one publishes an event or command through
  `IMessageAccess.PublishAsync`, and a handler in `ChefBot.Api/Messaging/` calls the other. For example,
  `PantryManager` publishes `PantryChanged` and its handler calls `IPlanManager.RefreshOpenPlansAsync`.
- Messages are kept in PostgreSQL (the `wolverine` schema) and only sent once the transaction that published
  them commits. Handlers log expected problems, such as a photo that can't be downloaded, rather than throwing; there is no
  retry logic to keep in mind.
- One way to do each thing. Ingredient needs come only from `MealRequirements`. Every change to a plan goes
  through `PlanManager`, which ends each change by placing the leftover lunches and rebuilding the shopping list,
  so they can't drift. Plan changes run one at a time (`PlanWriteLock`), which is enough because ChefBot runs as
  a single instance per database.
- Recipe file imports are pluggable. To read a new kind of file, implement `IRecipeFileFormat` (from
  `Engines/Contracts/Services/`) in `ChefBot.Core/Engines/Transformation/` and register it in `CoreRegistration.cs`; `TransformationEngine` picks
  it by file name ending and the web page's file picker lists it automatically.
- Recipe web pages are pluggable the same way. `SchemaOrgPage` reads any site from its schema.org data; a site
  that needs more gets its own `IRecipePageReader` (see [Adding a recipe site](#adding-a-recipe-site)). Pages
  and photos are downloaded through `PublicWeb.Handler`, which refuses private network addresses, so a link
  can't reach the database or the home network. Keep any new download of an address someone else chose on it.
- Something new to store: add the entity in `ResourceAccess/Persistence/`, the data contract in
  `ResourceAccess/Contracts/Messages/`, map them in a new `EntityMapping.<Name>.cs`, and add a line to
  `BackupAccess.Tables` (in `BackupAccess.Tables.cs`) so backups include it. `SchemaTests` fails when a table is
  neither backed up nor listed as left out, or an enum column isn't stored by name.
- No source file is over 400 lines; `FileSizeTests` checks, and lists the few files still allowed over. A class
  that outgrows it is split into partial files by area, `<Type>.<Area>.cs` beside `<Type>.cs` (`PlanManager.Shopping.cs`,
  `ValidationEngine.Recipes.cs`). `<Type>.cs` keeps the constructor, the class's summary and helpers several areas
  share; each other part starts with a `/// <content>` line saying what it covers.

## Adding a recipe site

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

## Tests

Tests use [TUnit](https://tunit.dev). New logic in `ChefBot.Core` needs tests next to the existing ones.
Assertions are awaited: `await Assert.That(actual).IsEqualTo(expected);`.

- Engines are tested directly, with the small catalog and recipe library in `TestData.cs`.
- Managers are tested with the in-memory accessors (`FakeRecipes`, `FakePlans`, ..., `NoBackups`), wired up by
  `TestCore` (`new TestCore().PlanManager()`). They hand out copies the way the real ones do, and
  `FakeMessages` records what was published.
- The test project can see Core's internal types (`InternalsVisibleTo`).

## Database changes

The schema is created with `EnsureCreated` on first start, which is simple but cannot upgrade an existing
database. It only creates tables in a database that has none, so `PrepareChefBotAsync` runs it before the
message bus starts and creates its own tables in the `wolverine` schema. Before the first release that changes
a table, switch to EF Core migrations: add `Microsoft.EntityFrameworkCore.Design`, run
`dotnet ef migrations add Initial`, and replace `EnsureCreatedAsync` with `MigrateAsync` in `CoreRegistration.cs`.

## Code made at build time

ChefBot doesn't use reflection-based JSON or compile code while running. That keeps startup quick and is the
groundwork for Native AOT.

**JSON.** Every JSON body, message and file is read and written through a source-generated `JsonSerializerContext`:

- `ContractJsonContext` (`ChefBot.Core/Managers/Contracts/ContractJsonContext.cs`) covers the records in
  `Managers/Contracts/Messages/`.
  The API uses it for request and response bodies, and the message bus uses it for the commands and events it stores.
- `ApiJsonContext` (`ChefBot.Api/ApiJsonContext.cs`) covers the bodies defined in the API project.
- `McpJsonContext` (`ChefBot.Api/Mcp/McpJsonContext.cs`) covers the MCP tools' own parameters; their results are
  `ContractJsonContext` records.
- Each accessor that reads a file or talks to another service has its own private context next to its JSON types:
  backups, the webhook, the sample recipes, the known stores and the ingredient catalog.

A new request, response, command or event needs a `[JsonSerializable]` line in its context. Records used inside a
listed one are included automatically. `JsonShapeTests` fails when something in `Managers/Contracts/Messages/` is missing.
It also checks that every shape reads JSON the way reflection does. Two things behave differently from reflection:

- When a type has `init` properties, the generator sets all of them, so a field the JSON leaves out loses its
  `= ...` default and becomes null or zero. Prefer positional records. For a list that may be missing, write
  `{ get; init => field = value ?? []; } = [];`, as `CatalogEntry` does.
- The values in `Notification.Data` must be types listed on `NotificationAccess.WebhookJson`.

**Wolverine's generated code.** Wolverine generates the code that calls each endpoint and handler. Debug builds
generate and compile it at startup with Roslyn, so changing an endpoint or handler needs no extra step. Release
builds compile in code written beforehand to `src/ChefBot.Api/Internal/Generated/` and leave Roslyn out. That
folder isn't in git, and the image build writes it from the source. To run a Release build on your own machine:

```sh
rm -rf src/ChefBot.Api/Internal/Generated           # codegen write never deletes files for removed endpoints
dotnet run --project src/ChefBot.Api -- codegen write   # starts the app far enough to write the code; no database needed
dotnet build ChefBot.slnx -c Release
```

If a Release build is missing the code for an endpoint or handler, it stops at startup with an error that names
it. Run the commands above again to fix it.

## The web page

`src/ChefBot.Web` is a [Svelte 5](https://svelte.dev) page, written in plain JavaScript and built with Vite.
Node is only needed to build it: `./dev.sh` and the image build use Node from a container when this machine
has none.

- `src/lib/state.svelte.js` holds everything the page shows (`app`), with `run()`, toasts and the confirmation
  dialog. The actions that load or change it are in one module per area: `plan`, `shopping`, `recipes`, `setup`,
  `settings` and `session` (`src/lib/<area>.svelte.js`). `app.svelte.js` re-exports them all, so components
  import from it; the modules in `src/lib/` import from each other directly. Svelte redraws what changed.
- Offline mode: `public/sw.js` is a service worker that keeps the page itself (index.html and `/assets`, never
  `/api`). `src/lib/offline.svelte.js` saves what the Shopping and Pantry tabs show in localStorage and shows it
  when ChefBot can't be reached (`app.offline`). `src/lib/outbox.svelte.js` queues the shopping changes made
  meanwhile and sends them in order once ChefBot answers. `api()` fails with `error.offline` when a request never
  reached ChefBot. An action that should work offline catches that and makes its change through the outbox, as
  the actions in `shopping.svelte.js` do. `tests/offline.spec.js` covers it, using `api.offline` in the stub.
- Every view has its own address (`/recipes/{id}`, `/settings/backups`, ...) so it can be bookmarked. The list
  is at the top of `src/lib/addresses.js`. Move between views with plain `<a href>` links or `go(path)` from
  there, not by setting `app.tab`; the server answers any path that isn't a file or `/api` with `index.html`.
- `src/App.svelte` is the header, tabs and footer. Each screen is a component: `screens/` for setup and
  sign-in, `tabs/` for the tabs, `settings/` for the Settings sections, and `components/` for pieces used in
  more than one place (the sample picker, file import, setup progress bar).
- `src/styles.css` holds the colours and the shared building blocks: buttons, cards, rows, chips, forms and
  lists. Styles only one component uses go in that component's `<style>` block, where Svelte scopes them.
- Each colour in `styles.css` is a token written as `light-dark(light, dark)`, so it is defined once for both
  themes. The page follows the device until someone picks a theme with the sun and moon button
  (`lib/theme.svelte.js`), which sets `data-theme` on `<html>`. Use the tokens rather than new hex values:
  `--good`/`--warn`/`--bad`/`--info` with their `-bg` tints for status, `--accent-2` for small warm highlights,
  and `--area-<tab>` for a tab's own colour on icons and borders, never on body text.
- Settings save by themselves rather than with a Save button: `autosave()` from `lib/autosave.svelte.js` debounces
  the save, checks it can be sent, and saves anything waiting when the section closes; `components/SaveStatus.svelte`
  says how it went. Forms that send something once, such as signing in or adding an item, keep their button and
  wrap the work in `pending(event, ...)` from `lib/pending.js`, so a second click doesn't send it again.
- `./dev.sh web` builds the page once into `src/ChefBot.Api/wwwroot`, which is build output and not in git.
  Use it before a plain `dotnet run`, and to check that the page still builds.

### Accessibility tests

`./dev.sh web-test` checks every screen of the page with [axe](https://github.com/dequelabs/axe-core) (WCAG 2.2
A and AA, plus axe's best practices) in real Chromium, in light mode, dark mode and at phone width, and tests
the keyboard behaviour axe can't see. It takes about a minute and a half, prints a dot per test, and exits.
Arguments go to `playwright test`: `./dev.sh web-test --project desktop-dark -g schedule`, or `--reporter=list` to
see each test's name.

`./dev.sh web-shot /history` screenshots one address of the page, answered from the same stubs, in each project
into `src/ChefBot.Web/shots/` (`tests/screenshot.spec.js`, which normal runs skip).

- The tests are in `src/ChefBot.Web/tests/`: `a11y.spec.js` has one row per screen or state in `VIEWS`, and
  `keyboard.spec.js` checks focus and keys. To cover a new screen, add a row to `VIEWS`.
- No ChefBot or database runs. `fixtures.js` answers every `/api` request from `data.js`, one realistic
  household whose shapes match the C# response records. Use `api.set({ 'POST /api/invites': ... })` in a
  test to change an answer, and `reply(status, body)` for a failure. A request with no answer fails the test,
  so when the page starts calling a new endpoint, add a default answer for it.
- Colour contrast is checked, so fade things with the muted colours, not `opacity`: text under `opacity` loses
  contrast. Don't turn axe rules off. If one is genuinely wrong for an element, pass `disable` at that one call
  and say why.
- The tests always run in Playwright's container, whose version must match the exact `@playwright/test`
  version pinned in `package.json`. Bump both together. The container is Ubuntu, and `node_modules` in the
  source folder may come from the Alpine Node container, so the tests keep their own copy in the
  `chefbot-web-test-modules` volume. `podman volume rm chefbot-web-test-modules` starts it afresh.

## Style

- File-scoped namespaces, nullable reference types on, `var` where the type is obvious.
- One top-level type per file, named after the type: `PlanStatus.cs` holds `PlanStatus`, `IPlanAccess.cs` holds
  `IPlanAccess`. This goes for small records, enums and test fakes too; nested types stay inside their parent.
  Put a comment that covers several types on each of them, or in this guide. The build enforces this:
  `.editorconfig` makes StyleCop's SA1402 (one type per file) and SA1649 (file name matches the type) errors.
- Public types get an XML summary that says what they are for, not how they work.
- User-facing messages are plain sentences that say what happened and what to do next.
