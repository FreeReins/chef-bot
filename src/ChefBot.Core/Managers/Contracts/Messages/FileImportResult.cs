namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What importing a recipe file did.</summary>
/// <param name="Added">Recipes new to the library.</param>
/// <param name="AlreadyInLibrary">Recipes left alone because one with the same name is already there.</param>
/// <param name="Skipped">Entries without a name or any ingredients.</param>
public sealed record FileImportResult(int Added, int AlreadyInLibrary, int Skipped);
