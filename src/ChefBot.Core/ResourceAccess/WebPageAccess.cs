using System.Text;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IWebPageAccess"/>
/// <remarks>Its <see cref="HttpClient"/> uses <see cref="PublicWeb.Handler"/>, set up in <c>CoreRegistration</c>.</remarks>
internal sealed class WebPageAccess(HttpClient http) : IWebPageAccess
{
    /// <summary>Recipe pages are rarely over 1 MB; anything much larger isn't one.</summary>
    private const int MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] HtmlTypes = ["text/html", "application/xhtml+xml"];

    public async Task<WebPage> FetchAsync(Uri address, CancellationToken cancellationToken)
    {
        var site = address.Host;
        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.5");
        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new RecipeSourceException(
                    $"{site} answered with HTTP {(int)response.StatusCode}, so ChefBot couldn't read the page. " +
                    "Check the link works in your browser; if it does, the site may not let ChefBot in, so type the recipe in instead.");
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is not null && !HtmlTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase))
            {
                throw new RecipeSourceException($"That link is a {mediaType} file, not a web page. Use the link to the recipe's page.");
            }

            var bytes = await ReadUpToAsync(response.Content, cancellationToken);
            var html = TextEncoding(response.Content.Headers.ContentType?.CharSet).GetString(bytes);
            return new WebPage(response.RequestMessage?.RequestUri ?? address, html);
        }
        catch (HttpRequestException exception) when (PublicWeb.Refusal(exception) is { } refusal)
        {
            throw refusal;
        }
        catch (HttpRequestException exception)
        {
            throw new RecipeSourceException($"ChefBot couldn't reach {site}: {exception.Message} Check the link and try again.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RecipeSourceException($"{site} took too long to answer. Try again later, or type the recipe in instead.", exception);
        }
    }

    private static async Task<byte[]> ReadUpToAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var body = await content.ReadAsStreamAsync(cancellationToken);
        using var copy = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            copy.Write(buffer, 0, read);
            if (copy.Length > MaxBytes)
            {
                throw new RecipeSourceException("That page is larger than 5 MB, which is more than any recipe page needs. Check the link.");
            }
        }

        return copy.ToArray();
    }

    /// <summary>The text encoding the server named, or UTF-8 when it named none ChefBot knows.</summary>
    private static Encoding TextEncoding(string? charset)
    {
        try
        {
            return string.IsNullOrWhiteSpace(charset) ? Encoding.UTF8 : Encoding.GetEncoding(charset.Trim('"', ' '));
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }
}
