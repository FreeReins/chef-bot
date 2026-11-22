---
name: add-stored-entity
description: "Add a new stored table to ChefBot: entity, contract, mapping, DbContext, backups, accessor, registration, test fake. USE FOR: new table, new entity, persist a log or journal. DO NOT USE FOR: a column on an existing entity, endpoints (add-core-operation), web screens (add-web-screen)."
---

# Add a stored entity

Edit these instead of reading them first; each step names the line to copy.
`<Name>` is the type, `<Names>` the table and DbSet.

1. `src/ChefBot.Core/ResourceAccess/Persistence/<Name>Entity.cs`: plain properties, `Guid Id`.
2. `ResourceAccess/Contracts/Messages/<Name>.cs`: `internal sealed class`, `Id = Guid.CreateVersion7()`, XML summary.
   A new enum gets `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` so backups write it by name.
3. `Persistence/ChefBotDbContext.cs`: a `DbSet` after the others, and `modelBuilder.Entity<...>` with `ToTable`
   and indexes before `StoreEnumsByName`. Enum columns are stored by name automatically.
4. New `Persistence/EntityMapping.<Name>.cs`, copied from `EntityMapping.FoodMoment.cs`: `ToContract` and `ToEntity`.
5. `ResourceAccess/BackupAccess.Tables.cs`: a line at the end of `Tables`
   (`new BackupTable<<Name>Entity>("<names>", BackupJson.Default.List<Name>Entity)`, with a comment
   "Empty in backups made before ..."), and `[JsonSerializable(typeof(List<<Name>Entity>))]` on `BackupJson` in
   `BackupAccess.cs`. Children reached through a parent's `Include` need neither.
6. `Contracts/Services/I<Name>Access.cs` and `ResourceAccess/<Name>Access.cs`, shaped like `PantryAccess`.
   Removing rows added in the same request: `Load` the query, then remove from `db.<Names>.Local`.
7. `CoreRegistration.cs`: `services.AddScoped<I<Name>Access, <Name>Access>();` with the other accessors.
8. `tests/ChefBot.Core.Tests/Fake<Name>.cs` with a public `Rows` list, a property on `TestCore`, and
   `services.AddSingleton<I<Name>Access>(<Name>)` in `TestCore.Register`.

## Example

`FoodMoment` (the food history): `FoodMomentEntity`, `FoodMoment`, `FoodMomentKind`, `IHistoryAccess`,
`HistoryAccess`, `FakeHistory`.

## Finish

Build and run the tests (AGENTS.md), then tell the user to run `./dev.sh reset`. The schema comes from
`EnsureCreated`, so don't add a migration. Add the table to `docs/developers/data-model.md`, and a line under
"Versions that needed this" in `docs/admin/upgrading.md`, since existing installs need a fresh database.

## Troubleshooting

- SA1402/SA1649 build error: one type per file, named after the type.
- `SchemaTests` fails: the table is missing from `BackupAccess.Tables` (step 5). A table that must not be backed
  up goes in that test's `NotBackedUp` with the reason.
