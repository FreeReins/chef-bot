using System.Net;

namespace ChefBot.Core.Tests;

/// <summary>An HTTP server stand-in: it keeps each request with its body and answers every one with <see cref="Status"/>.</summary>
internal sealed class RecordingHandler : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((request, request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
        return new HttpResponseMessage(Status);
    }
}
