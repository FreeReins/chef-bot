using ChefBot.Core.Planning;

namespace ChefBot.Api.Contracts;

// Request bodies that are specific to the HTTP API. Requests that map one-to-one onto a core
// type use that type directly: RecipeDraft, NewPantryItem and PurchaseDetails.

/// <summary>Stage a plan. Send <c>{}</c> for the upcoming week.</summary>
/// <param name="WeekStart">The Sunday to plan from. Defaults to the upcoming week.</param>
/// <param name="Replace">Start a week over even though it was already confirmed.</param>
public sealed record StagePlanRequest(DateOnly? WeekStart = null, bool Replace = false);

/// <summary>Replace a meal with a recipe or with a free-form meal. Send exactly one of the two.</summary>
public sealed record ReplaceMealRequest(Guid? RecipeId = null, CustomMealRequest? Custom = null);

/// <summary>A free-form meal, for example one of the creative suggestions.</summary>
public sealed record CustomMealRequest(string Title, IReadOnlyList<string> Ingredients);

public sealed record MealStatusRequest(MealStatus Status);

/// <summary>Mark every open shopping item as bought. Send <c>{}</c> for today.</summary>
public sealed record PurchaseAllRequest(DateOnly? PurchasedOn = null);

/// <summary>Correct a pantry item. Null leaves a value unchanged.</summary>
public sealed record UpdatePantryItemRequest(decimal? Quantity = null, DateOnly? ExpiresOn = null);

/// <summary>A recipe file to import: its name, which decides the format, and its text as read by the browser.</summary>
public sealed record ImportFileRequest(string FileName, string Content);

public sealed record SignInRequest(string Username, string Password);

/// <summary>Create an account from an invite link.</summary>
public sealed record AcceptInviteRequest(string Token, string Username, string DisplayName, string Password);

/// <summary>Choose a new password using a reset link.</summary>
public sealed record ResetPasswordRequest(string Token, string Password);

/// <summary>Change your own password while signed in.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Sign-in rules.</summary>
/// <param name="MaxFailedSignIns">Wrong passwords in a row before an account is locked. Zero turns lockout off.</param>
/// <param name="LockoutMinutes">How long a locked account stays locked.</param>
/// <param name="RequireHttps">Send plain HTTP visitors to HTTPS.</param>
public sealed record SecurityRequest(int MaxFailedSignIns, int LockoutMinutes, bool RequireHttps);

/// <summary>The names and addresses a new self-signed certificate should cover.</summary>
public sealed record NewCertificateRequest(IReadOnlyList<string> Hostnames);

/// <summary>When backups run and how many are kept. Zero means no limit.</summary>
public sealed record BackupSettingsRequest(bool Enabled, string Time, int KeepCount, int KeepDays);

/// <summary>The sample recipes to add, by name.</summary>
public sealed record AddSamplesRequest(IReadOnlyList<string> Names);

/// <summary>How to reach Mealie.</summary>
/// <param name="Url">Mealie's address, for example http://192.168.1.20:9000. Empty stops using Mealie.</param>
/// <param name="ApiToken">A new API token. Empty or null keeps the saved one.</param>
public sealed record MealieSettingsRequest(string? Url, string? ApiToken = null);
