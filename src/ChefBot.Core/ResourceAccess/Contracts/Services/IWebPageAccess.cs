using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>Downloads recipe pages from the public web. Addresses on private networks are refused.</summary>
internal interface IWebPageAccess
{
    /// <summary>The HTML page at an http or https address, following up to three redirects.</summary>
    /// <exception cref="Managers.Contracts.RecipeSourceException">
    /// The address is on a private network, the site couldn't be reached or refused, or the answer isn't a web page
    /// of a sensible size.
    /// </exception>
    Task<WebPage> FetchAsync(Uri address, CancellationToken cancellationToken);
}
