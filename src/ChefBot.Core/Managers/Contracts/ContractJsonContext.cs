using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers.Contracts;

/// <summary>
/// The JSON shapes of everything in <see cref="Messages"/>, made by the source generator at build time instead
/// of by reflection while running. The API reads and writes request and response bodies with it, and the message
/// bus stores commands and events with it. Records used inside the listed ones, and their enums, are included
/// automatically; add a line here for each new request, response, command or event that is used on its own.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
// Plans
[JsonSerializable(typeof(StagePlanRequest))]
[JsonSerializable(typeof(ReplaceMealRequest))]
[JsonSerializable(typeof(CustomMealRequest))]
[JsonSerializable(typeof(MealStatusRequest))]
[JsonSerializable(typeof(PurchaseDetails))]
[JsonSerializable(typeof(PurchaseAllRequest))]
[JsonSerializable(typeof(PlanResponse))]
[JsonSerializable(typeof(AlternativesResponse))]
[JsonSerializable(typeof(IngredientsResponse))]
[JsonSerializable(typeof(PlanStaged))]
[JsonSerializable(typeof(CheckWeeklySchedule))]
[JsonSerializable(typeof(StageScheduledWeek))]
// Pantry
[JsonSerializable(typeof(NewPantryItem))]
[JsonSerializable(typeof(UpdatePantryItemRequest))]
[JsonSerializable(typeof(PantryItemResponse))]
[JsonSerializable(typeof(IReadOnlyList<PantryItemResponse>))]
[JsonSerializable(typeof(PantryChanged))]
// Recipes
[JsonSerializable(typeof(RecipeDraft))]
[JsonSerializable(typeof(RecipeResponse))]
[JsonSerializable(typeof(IReadOnlyList<RecipeResponse>))]
[JsonSerializable(typeof(ImportFileRequest))]
[JsonSerializable(typeof(FileImportResult))]
[JsonSerializable(typeof(IReadOnlyList<RecipeFileFormatResponse>))]
[JsonSerializable(typeof(IReadOnlyList<SampleRecipeResponse>))]
[JsonSerializable(typeof(AddSamplesRequest))]
[JsonSerializable(typeof(AddSamplesResponse))]
[JsonSerializable(typeof(MealieSettingsRequest))]
[JsonSerializable(typeof(MealieSettingsResponse))]
[JsonSerializable(typeof(ImportMealieRecipes))]
[JsonSerializable(typeof(RecipeImportFinished))]
// Accounts
[JsonSerializable(typeof(NewAccount))]
[JsonSerializable(typeof(SignInRequest))]
[JsonSerializable(typeof(AcceptInviteRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(ChangePasswordRequest))]
[JsonSerializable(typeof(SecurityRequest))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(SessionInfo))]
[JsonSerializable(typeof(AccountSession))]
[JsonSerializable(typeof(IReadOnlyList<UserResponse>))]
[JsonSerializable(typeof(SessionResponse))]
[JsonSerializable(typeof(SecurityResponse))]
[JsonSerializable(typeof(IssuedLink))]
[JsonSerializable(typeof(IReadOnlyList<PendingLink>))]
[JsonSerializable(typeof(LinkDetails))]
[JsonSerializable(typeof(SecuritySettingsChanged))]
// Maintenance
[JsonSerializable(typeof(NotificationSettingsRequest))]
[JsonSerializable(typeof(NotificationSettingsResponse))]
[JsonSerializable(typeof(DeliveryResult))]
[JsonSerializable(typeof(ReminderRun))]
[JsonSerializable(typeof(BackupSettingsRequest))]
[JsonSerializable(typeof(BackupsResponse))]
[JsonSerializable(typeof(SendDueReminders))]
[JsonSerializable(typeof(RunDueBackup))]
public sealed partial class ContractJsonContext : JsonSerializerContext;
