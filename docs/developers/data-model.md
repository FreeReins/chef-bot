# Data model

ChefBot keeps everything in one PostgreSQL database through EF Core. This page covers what's stored, how the schema
is made, and how backups relate to it.

## What's stored

| Area | Tables (entities in `ResourceAccess/Persistence/`) |
|---|---|
| Recipes | `Recipe`, `RecipeIngredient`, `RecipeImage`, `RecipeImageData`, `RecipeFavourite`, `RecipeReview`, `RecipeSuggestion` (an AI model's suggestions, one per recipe, with its changes as `jsonb`), `Cookbook` (a named group of recipes; each recipe lists the cookbooks it is in as a `uuid[]` column, `CookbookIds`, so removing a cookbook takes its id off those recipes and leaves them in the library) |
| Plans | `MealPlan`, `PlannedMeal`, `PlannedMealIngredient`, `ShoppingItem`, `PlanStyle` |
| Shopping and pantry | `PantryItem`, `ManualShoppingItem`, `GroceryStore`, `ShoppingTarget` |
| People | `User`, `AccessLink` (invites and resets), `ApiToken`, `InboxEntry` (each person's messages behind the bell, the newest 50; `SourceId` is the event that added one, so handling it twice adds nothing) |
| History and fun | `FoodMoment`, `AchievementUnlock` |
| Settings | `PlannerSettings`, `CalendarSettings`, `AdvancedSettings`, `NotificationSettings`, `BackupSettings`, `SecuritySettings`, `CalendarFeed`, `ScheduleState`, `AiModel` and `AiRoute` (the AI models and which does each task) |
| Messages | Wolverine's own tables, in the `wolverine` schema |

Settings rows start empty. Until one is saved, the defaults in its contract are used (`IsSaved` is false).

`PlannedMeal.PortionsChosen` marks a meal whose servings someone chose (from a recipe's page), so resizing the plan
doesn't change it. Backups from before it read it as false.

## The boundary

Entities never leave `ResourceAccess/`. Accessors map them to plain contracts in
`ResourceAccess/Contracts/Messages/` (`EntityMapping.<Name>.cs`) and hand out copies, never tracked entities. Managers
map those to the public records in `Managers/Contracts/Messages/` (`Mapping.cs`).

## Creating the schema

The schema comes from EF Core migrations in `ResourceAccess/Persistence/Migrations/`, applied at startup before the
message bus starts (`DatabaseUpgrade`). The first, `Baseline`, is the schema as it was when ChefBot switched from
`EnsureCreated`. A database made by `EnsureCreated` (ChefBot's tables but no migration recorded in `__EFMigrationsHistory`, which may be missing or empty) is marked as
being at `Baseline` and then gets only the migrations after it. A fresh database gets them all.

A change to an entity needs a new migration; `MigrationTests` fails until there is one. See
[Database changes](https://github.com/FreeReins/chef-bot/blob/main/CONTRIBUTING.md#database-changes).

## Backups

A backup is the entities serialized as JSON and gzipped (`chefbot-backup-<time>.json.gz`). Every table is either
listed in `BackupAccess.Tables` or explicitly left out (API tokens are left out). `SchemaTests` fails when a new
table is neither. Renaming an entity property changes the backup format, so old names must stay readable.

## Adding a table

Follow [`.claude/skills/add-stored-entity/SKILL.md`](https://github.com/FreeReins/chef-bot/blob/main/.claude/skills/add-stored-entity/SKILL.md): entity, contract,
mapping, DbContext, backups, accessor, registration and test fake.
