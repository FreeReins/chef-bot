using AngleSharp.Dom;
using ChefBot.Core.Engines.Contracts.Services;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Reads a recipe page on any site from the schema.org data most recipe sites include for search engines: JSON-LD
/// first, then microdata. Registered last, so a reader for a particular site comes first.
/// </summary>
internal sealed class SchemaOrgPage(SchemaRecipe schema) : IRecipePageReader
{
    public string Name => "Any site (schema.org)";

    public bool Handles(Uri url) => true;

    public RecipeText? Read(Uri url, IDocument page, ClassifierWords words) => schema.FromPage(page, words);
}
