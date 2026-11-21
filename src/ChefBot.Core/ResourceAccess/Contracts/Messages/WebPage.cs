namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A downloaded web page.</summary>
/// <param name="Address">Where the page was found, after any redirects. Relative links on it are relative to this.</param>
/// <param name="Html">The page's HTML.</param>
internal sealed record WebPage(Uri Address, string Html);
