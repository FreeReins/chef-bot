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
./dev.sh web-test                                                       # web page accessibility tests, ~30s; exits
```

A change is done when the build has no warnings and the tests pass. A change to `src/ChefBot.Web` is done when
`./dev.sh web` builds it with no warnings and `./dev.sh web-test` passes; a new screen or state gets a row in
`VIEWS` in `src/ChefBot.Web/tests/a11y.spec.js` (CONTRIBUTING.md's "Accessibility tests").

Do not run `./dev.sh`, `./dev.sh test` or `./dev-fresh.sh`: they watch for changes and never exit. Do not use
`podman build --target test` to check work; it runs the same tests much more slowly.

## Rules the code doesn't make obvious

- `ChefBot.Core` is layered; CONTRIBUTING.md's "Where things go" has the map. Calls only go down: Api endpoints
  and handlers call one manager (`Managers/Contracts/Services/I*Manager`); managers call engines (`Engines/`) and
  resource accessors (`ResourceAccess/`); only `ValidationEngine` among the engines calls accessors. Nothing calls
  sideways: no manager calls a manager, no engine calls an engine. `ArchitectureTests` enforces this.
- Each layer has `Contracts/Services/` for its interfaces and `Contracts/Messages/` for the records passed through
  them. Everything in Core is `internal` except `Managers/Contracts/` and `CoreRegistration`. Requests, responses,
  commands and events are records in `Managers/Contracts/Messages/`; they are the HTTP JSON, so keep their shapes
  stable.
- Accessors take and return the plain data contracts in `ResourceAccess/Contracts/Messages/`, never EF entities, and
  changes only count once passed back to a `Save` method. Managers commit once through `IMessageAccess.CommitAsync`.
- Managers react to each other through messages, not calls: publish with `IMessageAccess.PublishAsync` (sent only
  when the commit succeeds), and handle it in `src/ChefBot.Api/Messaging/<Message>Handler.cs` by calling the
  other manager. Keep handlers simple: log expected failures, don't add retry logic.
- Ingredient needs come only from `MealRequirements`. Anything that changes a plan goes through `PlanManager`,
  which ends with `ReconcileAsync` so leftovers and the shopping list stay in step.
- Admin-only endpoints use `[Authorize(Policy = Sessions.AdminPolicy)]`; managers don't check roles.
- The schema is created with `EnsureCreated` and is never upgraded. After changing an entity, tell the user to
  run `./dev.sh reset`; don't add migrations unless asked. A new stored entity also needs adding to `BackupAccess`.
- The web page is Svelte 5 in plain JavaScript (no TypeScript). Its state (`app`) and the actions shared
  between screens live in `src/ChefBot.Web/src/lib/app.svelte.js`; a form's own submit stays in its component.
  Wrap anything that calls the API in `run()` so failures show as a message.
- `src/ChefBot.Api/wwwroot/` is build output. Never edit it or add it to git.

## When you change...

- **Logic in `ChefBot.Core`**: add TUnit tests beside the existing ones in `tests/ChefBot.Core.Tests/`: engines
  directly, managers with the `Fake*` accessors through `TestCore` (`new TestCore().PlanManager()`). Assertions are awaited:
  `await Assert.That(actual).IsEqualTo(expected);`.
- **A public type** (in practice, anything in `Managers/Contracts/`): give it an XML `<summary>` saying what it is
  for.
- **A user-facing message**: write a plain sentence that says what happened and what to do next.
- **A setting people should change**: add it to `deploy/podman/chefbot.env.example`.
- **A volume or port**: update the units in `deploy/podman/` and add a note to `README.md`.
- **The HTTP API**: update the API section of `README.md`.
- **A JSON body, message or file type**: list it in its source-generated `JsonSerializerContext`. CONTRIBUTING.md's
  "Code made at build time" says which context, and `JsonShapeTests` checks it.
- **An endpoint or handler**: Debug builds need nothing extra. Release builds need regenerated Wolverine code; see
  that same section.

## Reading the code cheaply

- `README.md` is for people running ChefBot. Skip it unless you are editing it.
- In Core, start from the manager interface in `src/ChefBot.Core/Managers/Contracts/Services/`: it lists every
  operation of its area. Open the manager, then only the engines and accessors it uses.
- The web page is one component per screen: `src/ChefBot.Web/src/tabs/<Tab>.svelte` (Recipes.svelte,
  Plan.svelte, ...), `screens/` for setup and sign-in, `settings/<Section>Panel.svelte`. Open only the one
  you need.
- `node_modules/` is installed packages.
- `config/ingredients.json` is a large catalog. Search it; don't read it whole.
- `bin/`, `obj/` and `TestResults/` are build output.

## Style

File-scoped namespaces, nullable reference types, `var` where the type is obvious.

One top-level type per file, named after the type (`PlanStatus.cs`, `IPlanAccess.cs`), in the folder its layer
belongs to. This includes small records, enums and test fakes. Nested types stay inside their parent. A comment
about several types goes on each of them or in CONTRIBUTING.md, not loose in a file. The build fails otherwise
(StyleCop SA1402 and SA1649, set to errors in `.editorconfig`).

A file's namespace matches its folder (IDE0130), and unused usings are warnings (IDE0005). The Claude Code hook in
`.claude/hooks/dotnet-format.sh` removes unused usings after each edit and reports a namespace that doesn't match.

## Improving this file

To measure and tune this file with Microsoft waza, follow
[.claude/skills/tune-agents-md/SKILL.md](.claude/skills/tune-agents-md/SKILL.md).
