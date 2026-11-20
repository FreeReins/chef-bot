namespace ChefBot.Core.Contracts.Messages;

/// <summary>What a Mealie import changed.</summary>
/// <param name="Added">Recipes seen for the first time.</param>
/// <param name="Updated">Imported recipes refreshed from the source.</param>
/// <param name="KeptLocal">Recipes left alone because they were edited or archived in ChefBot.</param>
/// <param name="Archived">Imported recipes that no longer exist in the source.</param>
/// <param name="Skipped">Recipes the source couldn't provide this time.</param>
public sealed record ImportResult(int Added, int Updated, int KeptLocal, int Archived, int Skipped);
