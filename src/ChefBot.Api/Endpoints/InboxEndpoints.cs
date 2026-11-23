using System.Security.Claims;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>
/// The signed-in person's inbox: the messages behind the bell on the web page. Each person has their own, and only
/// the newest 50 are kept. Changes are announced on <c>/api/live</c> as "inbox".
/// </summary>
public static class InboxEndpoints
{
    /// <summary>Your messages, newest first, and how many are unread.</summary>
    [WolverineGet("/api/inbox")]
    public static Task<InboxResponse> Get(ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.GetInboxAsync(Sessions.RequireUserId(principal), cancellationToken);

    /// <summary>Adds a note to your own inbox, unread, such as the page saying "Recipe saved.". Returns the inbox.</summary>
    [WolverinePost("/api/inbox")]
    public static Task<InboxResponse> Add(
        InboxNoteRequest request, ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.AddInboxNoteAsync(Sessions.RequireUserId(principal), request, cancellationToken);

    /// <summary>Marks every message in your inbox read. Returns the inbox.</summary>
    [WolverinePost("/api/inbox/read")]
    public static Task<InboxResponse> MarkRead(ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.MarkInboxReadAsync(Sessions.RequireUserId(principal), cancellationToken);

    /// <summary>Empties your inbox. Returns the (empty) inbox.</summary>
    [WolverineDelete("/api/inbox")]
    public static Task<InboxResponse> Clear(ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.ClearInboxAsync(Sessions.RequireUserId(principal), cancellationToken);
}
