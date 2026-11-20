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
| `ChefBot.Core` | Everything below the API, in layers (see below). Only `Contracts/` and `CoreRegistration` are public | ASP.NET Core, Npgsql EF Core, Wolverine's EF Core and PostgreSQL storage |
| `ChefBot.Api` | The clients of the core: Wolverine HTTP endpoints (`Endpoints/`), message handlers and the timer that queues the scheduled jobs (`Messaging/`), sign-in, HTTPS, startup wiring; serves the web page | `ChefBot.Core`'s public contracts |
| `ChefBot.Web` | The web page: Svelte 5 components built with Vite into `ChefBot.Api/wwwroot` | The HTTP API |

`ChefBot.Core` is split by what changes for different reasons. Calls only go down; nothing calls sideways:

```
Clients (Api endpoints and message handlers)     see only Contracts/
   ▼
Managers/          one per area of use: orchestrate a use case, own the transaction, publish messages
   ▼          ▼
Engines/  ─►  ResourceAccess/   engines do the work (verbs); accessors read and save
                 ▼
            PostgreSQL (EF Core), Mealie, the webhook, the backup folder, config/sample-recipes.json

Utilities/   the ingredient catalog, text and units, clock, settings options, password hashing: any layer
```

| Folder | Holds | Rules |
|---|---|---|
| `Contracts/Services/` | The public manager interfaces: `IPlanManager`, `IPantryManager`, `IRecipeManager`, `IAccountManager`, `IMaintenanceManager` | Clients depend on these and nothing else in Core |
| `Contracts/Messages/` | The managers' requests and responses (these are the HTTP JSON bodies), the commands and events sent through the message bus, and the enums they use | Plain records. Changing one changes the API |
| `Contracts/Errors.cs` | `NotFoundException`, `RuleViolationException`, ... which the API turns into problem responses | |
| `Managers/` | The manager classes and `Mapping.cs` (data contracts to messages) | Call engines and accessors, never another manager. Commit once through `IUnitOfWork`, which also sends what they published |
| `Engines/` | `PlanningEngine`, `ShoppingEngine`, `SuggestionEngine`, `TransformationEngine` (recipe files, Mealie recipes, typed-in recipes and pantry items), `ValidationEngine`, `SecurityEngine`, `NotificationEngine`; `Shared.cs` holds the calculations several of them use (`MealRequirements`, `PantrySnapshot`, `WeekScorer`) | Never call a manager or another engine. Only `ValidationEngine` reads through accessors, for rules that depend on what is stored; the others take everything as arguments and can be tested without a database |
| `ResourceAccess/` | The accessor interfaces (`Accessors.cs`) and implementations, the plain data contracts they pass (`Contracts/`), and the EF Core entities and `ChefBotDbContext` (`Persistence/`) | Return copies, never tracked entities. Never call each other. The entities don't leave this folder |
| `Utilities/` | `IngredientCatalog`, `TextNormalizer`, `Units`, `WeekLayout`, `DailyTime`, the settings options, `PlanWriteLock` | Depend on no layer |

`tests/ChefBot.Core.Tests/ArchitectureTests.cs` checks these rules, so a call in the wrong direction fails the
tests.

Rules of thumb:

- An endpoint or handler binds the request, calls one manager method and returns what it gives back. Admin-only
  endpoints carry `[Authorize(Policy = Sessions.AdminPolicy)]`; nothing checks for admins inside the core.
- A manager loads through accessors, hands the data to engines, saves through accessors and commits once.
  Managers that need to react to each other do it with messages: one publishes an event or command through
  `IUnitOfWork.PublishAsync`, and a handler in `ChefBot.Api/Messaging/` calls the other. For example,
  `PantryManager` publishes `PantryChanged` and its handler calls `IPlanManager.RefreshOpenPlansAsync`.
- Messages are kept in PostgreSQL (the `wolverine` schema) and only sent once the transaction that published
  them commits. Handlers log expected problems, such as Mealie being down, rather than throwing; there is no
  retry logic to keep in mind.
- One way to do each thing. Ingredient needs come only from `MealRequirements`. Every change to a plan goes
  through `PlanManager`, which ends each change by placing the leftover lunches and rebuilding the shopping list,
  so they can't drift. Plan changes run one at a time (`PlanWriteLock`), which is enough because ChefBot runs as
  a single instance per database.
- Recipe file imports are pluggable. To read a new kind of file, implement `IRecipeFileFormat` in
  `ChefBot.Core/Engines/Transformation/` and register it in `CoreRegistration.cs`; `TransformationEngine` picks
  it by file name ending and the web page's file picker lists it automatically.
- Something new to store: add the entity in `ResourceAccess/Persistence/`, the data contract in
  `ResourceAccess/Contracts/`, map them in `EntityMapping.cs`, and add it to `BackupAccess` so backups include it.

## Tests

Tests use [TUnit](https://tunit.dev). New logic in `ChefBot.Core` needs tests next to the existing ones.
Assertions are awaited: `await Assert.That(actual).IsEqualTo(expected);`.

- Engines are tested directly, with the small catalog and recipe library in `TestData.cs`.
- Managers are tested with the in-memory accessors in `Fakes.cs` (`new TestCore().PlanManager()`), which hand
  out copies the way the real ones do, and `FakeUnitOfWork`, which records what was published.
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

- `ContractJsonContext` (`ChefBot.Core/Contracts/ContractJson.cs`) covers the records in `Contracts/Messages/`.
  The API uses it for request and response bodies, and the message bus uses it for the commands and events it stores.
- `ApiJsonContext` (`ChefBot.Api/ApiJson.cs`) covers the bodies defined in the API project.
- Each accessor that reads a file or talks to another service has its own private context next to its JSON types:
  backups, Mealie, the webhook, the sample recipes and the ingredient catalog.

A new request, response, command or event needs a `[JsonSerializable]` line in its context. Records used inside a
listed one are included automatically. `JsonShapeTests` fails when something in `Contracts/Messages/` is missing.
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

- `src/lib/app.svelte.js` holds everything the page shows (`app`) and every action that loads or changes it.
  Components read `app` and call those actions; Svelte redraws what changed.
- Every view has its own address (`/recipes/{id}`, `/settings/mealie`, ...) so it can be bookmarked. The list
  is at the top of `src/lib/addresses.js`. Move between views with plain `<a href>` links or `go(path)` from
  there, not by setting `app.tab`; the server answers any path that isn't a file or `/api` with `index.html`.
- `src/App.svelte` is the header, tabs and footer. Each screen is a component: `screens/` for setup and
  sign-in, `tabs/` for the tabs, `settings/` for the Settings sections, and `components/` for pieces used in
  more than one place (the sample picker, file import, Mealie connection, setup progress bar).
- `src/styles.css` holds the colours and the shared building blocks: buttons, cards, rows, chips, forms and
  lists. Styles only one component uses go in that component's `<style>` block, where Svelte scopes them.
- `./dev.sh web` builds the page once into `src/ChefBot.Api/wwwroot`, which is build output and not in git.
  Use it before a plain `dotnet run`, and to check that the page still builds.

## Style

- File-scoped namespaces, nullable reference types on, `var` where the type is obvious.
- Public types get an XML summary that says what they are for, not how they work.
- User-facing messages are plain sentences that say what happened and what to do next.
