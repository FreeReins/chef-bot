# AGENTS.md

ChefBot is a self-hosted .NET 10 meal planner: Wolverine HTTP API, EF Core on PostgreSQL, and a Svelte 5 page
in `src/ChefBot.Web/`. [CONTRIBUTING.md](CONTRIBUTING.md) is the full guide; read its "Where things go" before
adding a class. Before changing the web page, read [src/ChefBot.Web/AGENTS.md](src/ChefBot.Web/AGENTS.md).

## Check your work

```sh
dotnet build ChefBot.slnx -v q -nologo                                  # about 1s when warm
dotnet run --project tests/ChefBot.Core.Tests                           # all tests, under 1s
dotnet run --project tests/ChefBot.Core.Tests -- --treenode-filter "/*/*/ShoppingTests/*"   # one class
```

A change is done when the build has no warnings and the tests pass. Don't run `./dev.sh`, `./dev.sh test`,
`./dev-fresh.sh` or `dotnet watch`: they never exit. Don't check work with `podman build --target test`.

## Checklists

For a new stored table, a new manager operation or endpoint, or a new web screen, follow
`.claude/skills/add-stored-entity`, `add-core-operation` or `add-web-screen` (`SKILL.md` in each).

## Rules the code doesn't make obvious

- `ChefBot.Core` is layered and calls only go down: endpoints and handlers call one manager
  (`Managers/Contracts/Services/I*Manager`); managers call engines and accessors; only `ValidationEngine` among
  engines calls accessors. No manager calls a manager, no engine calls an engine. `ArchitectureTests` enforces it.
- Everything in Core is `internal` except `Managers/Contracts/` and `CoreRegistration`. Records in
  `Managers/Contracts/Messages/` are the HTTP JSON: keep their shapes stable, give public types an XML summary.
- Accessors take and return the contracts in `ResourceAccess/Contracts/Messages/`, never EF entities. Managers
  commit once through `IMessageAccess.CommitAsync`.
- Managers react to each other only through messages: `IMessageAccess.PublishAsync`, handled in
  `src/ChefBot.Api/Messaging/<Message>Handler.cs` (or `<Message><Subscriber>Handler.cs` when an event has several),
  which calls the other manager. Each handler names its queue with `[StickyHandler(MessageQueues.…)]` and must be
  safe to run twice. Handlers log expected failures and never retry by hand: retries and dead-lettering come from
  the error policies in `CoreRegistration`. Every message gets a `[MessageIdentity("chefbot.<name>.v1")]`, is
  listed in `ContractJsonContext`, and only gains fields; a breaking change is a new `.v2`.
- Ingredient needs come only from `MealRequirements`. Anything that changes a plan goes through `PlanManager`,
  which ends with `ReconcileAsync` so leftovers and the shopping list stay in step.
- Admin-only endpoints use `[Authorize(Policy = Sessions.AdminPolicy)]`; managers don't check roles.
- The schema comes from `EnsureCreated` and is never upgraded: after changing an entity, tell the user to run
  `./dev.sh reset`. Don't add migrations unless asked. A new stored entity also goes in `BackupAccess.Tables`.
- A setting people should change goes in `deploy/podman/chefbot.env.example`. A volume or port: update the
  units in `deploy/podman/` and add a note to `README.md`. An HTTP API change: update the API section of `README.md`.
- User-facing messages are plain sentences saying what happened and what to do next.

## Tests

TUnit, beside the existing ones in `tests/ChefBot.Core.Tests/`: engines directly, managers with the `Fake*`
accessors through `TestCore` (`new TestCore().PlanManager()`). Assertions are awaited:
`await Assert.That(actual).IsEqualTo(expected);`.

## Reading the code cheaply

- Skip `README.md` unless editing it. Skip `node_modules/`, `bin/`, `obj/` and `TestResults/`.
- In Core, start from the manager interface in `Managers/Contracts/Services/`, then open only the engines and
  accessors that manager uses. Big classes are split by area into `<Type>.<Area>.cs` partial files
  (`PlanManager.Shopping.cs`): open only the area you need. Files stay under 400 lines (`FileSizeTests`).
- The web page is one component per screen: `src/ChefBot.Web/src/tabs/<Tab>.svelte` (Recipes.svelte, ...),
  `screens/` for setup and sign-in, `settings/<Section>Panel.svelte`.
- `config/ingredients.json` is a large catalog. Search it; don't read it whole.

## Style

File-scoped namespaces, nullable reference types, `var` where the type is obvious. One top-level type per file,
named after it, in its layer's folder, with the namespace matching the folder. The build and
`.claude/hooks/dotnet-format.sh` report mistakes.

To measure and tune this file, follow [.claude/skills/tune-agents-md/SKILL.md](.claude/skills/tune-agents-md/SKILL.md).
