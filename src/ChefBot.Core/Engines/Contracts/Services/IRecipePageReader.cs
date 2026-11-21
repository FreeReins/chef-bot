using AngleSharp.Dom;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Services;

/// <summary>
/// One way of reading recipe web pages. <see cref="Transformation.SchemaOrgPage"/> reads any site that describes
/// its recipes in schema.org terms. When a site needs something different, add a reader for it in
/// <c>Engines/Transformation/Sites/</c> and register it in <c>CoreRegistration</c> before <c>SchemaOrgPage</c>;
/// <see cref="Transformation.TransformationEngine"/> uses the first registered reader that handles an address.
/// CONTRIBUTING.md's "Adding a recipe site" has the steps.
/// </summary>
internal interface IRecipePageReader
{
    /// <summary>What the reader is called, shown after an import, such as "Any site (schema.org)".</summary>
    string Name { get; }

    /// <summary>Whether this reader is the one for the address, usually judged by its host.</summary>
    bool Handles(Uri url);

    /// <summary>
    /// The address to download, when it isn't the one given: a site's print version, for example. The recipe still
    /// records the address that was given.
    /// </summary>
    Uri PageToFetch(Uri url) => url;

    /// <summary>Reads the recipe from the page found at <paramref name="url"/>.</summary>
    /// <returns>Null when the page has no recipe this reader can find.</returns>
    RecipeText? Read(Uri url, IDocument page);
}
