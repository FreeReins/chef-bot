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
| `ChefBot.Core` | Entities, planning and shopping logic, use-case services, interfaces for storage and outside systems, and their implementations: PostgreSQL (EF Core) in `Persistence/`, the Mealie reader in `Mealie/`, the webhook notifier in `Notifications/` | ASP.NET Core and the Npgsql EF Core provider |
| `ChefBot.Api` | Wolverine HTTP endpoints, message handlers, the weekly schedule, startup wiring; serves the web page | `ChefBot.Core` |
| `ChefBot.Web` | The web page: Svelte 5 components built with Vite into `ChefBot.Api/wwwroot` | The HTTP API |

Rules of thumb:

- Decisions belong in `ChefBot.Core` as plain classes that take their inputs as arguments and never touch
  `Persistence/`, `Mealie/` or the webhook. `WeekPlanner`,
  `ShoppingListBuilder`, `AlternativeFinder` and `CreativeMealGenerator` read nothing and save nothing,
  which is why they can be tested without a database.
- Services (`PlanningService`, `MealService`, ...) load, call the logic, and save once.
- Endpoints and handlers stay thin: bind the request, call one service, map the result.
- Recipe file imports are pluggable. To read a new kind of file, implement `IRecipeFileFormat` in
  `ChefBot.Core/Recipes/Files/` and register it in `ServiceRegistration.cs`; `RecipeFileReader` picks it by file
  name ending and the web page's file picker lists it automatically.
- One way to do each thing. Ingredient needs come from `MealRequirements`; anything that changes a plan
  goes through `PlanRefresher` so leftovers and the shopping list can't drift.

## Tests

Tests use [TUnit](https://tunit.dev). New logic in `ChefBot.Core` needs tests next to the existing ones.
Assertions are awaited: `await Assert.That(actual).IsEqualTo(expected);`.

## Database changes

The schema is created with `EnsureCreated` on first start, which is simple but cannot upgrade an existing
database. Before the first release that changes a table, switch to EF Core migrations:
add `Microsoft.EntityFrameworkCore.Design`, run `dotnet ef migrations add Initial`, and replace
`EnsureCreatedAsync` with `MigrateAsync` in `DependencyInjection.cs`.

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
