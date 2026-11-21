using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IImageSourceAccess"/>
/// <remarks>Its <see cref="HttpClient"/> uses <see cref="PublicWeb.Handler"/>, set up in <c>CoreRegistration</c>.</remarks>
internal sealed class ImageSourceAccess(HttpClient http) : IImageSourceAccess
{
    public async Task<byte[]> DownloadAsync(string url, int maxBytes, bool allowPrivateAddresses, CancellationToken cancellationToken)
    {
        if (!WebAddress.IsHttp(url))
        {
            throw new RecipeSourceException($"The photo address {url} isn't a web address.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Options.Set(PublicWeb.AllowPrivateAddresses, allowPrivateAddresses);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new RecipeSourceException($"The photo at {url} couldn't be downloaded: HTTP {(int)response.StatusCode}.");
            }

            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var copy = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while (copy.Length <= maxBytes && (read = await body.ReadAsync(buffer, cancellationToken)) > 0)
            {
                copy.Write(buffer, 0, read);
            }

            return copy.ToArray();
        }
        catch (HttpRequestException exception) when (PublicWeb.Refusal(exception) is { } refusal)
        {
            throw refusal;
        }
        catch (HttpRequestException exception)
        {
            throw new RecipeSourceException($"The photo at {url} couldn't be downloaded: {exception.Message}", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RecipeSourceException($"The photo at {url} took too long to download.", exception);
        }
    }
}
