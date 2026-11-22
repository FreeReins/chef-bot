# Data model

ChefBot keeps everything in one PostgreSQL database through EF Core. This page covers what's stored, how the schema
is made, and how backups relate to it.

## What's stored

| Area | Tables (entities in `ResourceAccess/Persistence/`) |
|---|---|
| Recipes | `Recipe`, `RecipeIngredient`, `RecipeImage`, `RecipeImageData`, `RecipeFavourite`, `RecipeReview` |
| Plans | `MealPlan`, `PlannedMeal`, `PlannedMealIngredient`, `ShoppingItem`, `PlanStyle` |
| Shopping and pantry | `PantryItem`, `ManualShoppingItem`, `GroceryStore`, `ShoppingTarget` |
| People | `User`, `AccessLink` (invites and resets), `ApiToken` |
| History and fun | `FoodMoment`, `AchievementUnlock` |
| Settings | `PlannerSettings`, `CalendarSettings`, `AdvancedSettings`, `NotificationSettings`, `BackupSettings`, `SecuritySettings`, `CalendarFeed`, `ScheduleState` |
| Messages | Wolverine's own tables, in the `wolverine` schema |

Settings rows start empty. Until one is saved, the defaults in its contract are used (`IsSaved` is false).

## The boundary

Entities never leave `ResourceAccess/`. Accessors map them to plain contracts in
`ResourceAccess/Contracts/Messages/` (`EntityMapping.<Name>.cs`) and hand out copies, never tracked entities. Managers
map those to the public records in `Managers/Contracts/Messages/` (`Mapping.cs`).

## Creating the schema

The schema is created with `EnsureCreated` on the first start, and is **not upgraded**. A change to an entity needs a
fresh database. For development, run `./dev.sh reset`. For users, it's the backup → fresh database → restore path in
[Upgrading](../admin/upgrading.md#when-a-version-changes-the-database). Before the first release that changes a table
for existing users, ChefBot should switch to EF Core migrations, as
[Database changes](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/CONTRIBUTING.md#database-changes) describes.

## Backups

A backup is the entities serialized as JSON and gzipped (`chefbot-backup-<time>.json.gz`). Every table is either
listed in `BackupAccess.Tables` or explicitly left out (API tokens are left out). `SchemaTests` fails when a new
table is neither. Renaming an entity property changes the backup format, so old names must stay readable.

## Adding a table

Follow [`.claude/skills/add-stored-entity/SKILL.md`](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/.claude/skills/add-stored-entity/SKILL.md): entity, contract,
mapping, DbContext, backups, accessor, registration and test fake.
