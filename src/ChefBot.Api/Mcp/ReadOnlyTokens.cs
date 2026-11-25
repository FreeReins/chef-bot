using System.Security.Claims;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ChefBot.Api.Mcp;

/// <summary>
/// Keeps an assistant signed in with a read-only API token to the tools marked <c>ReadOnly = true</c>: it is only
/// shown those, and calling any other is refused. A new tool is refused until it is marked read-only, so a tool that
/// changes something can't slip through by being left unmarked.
/// </summary>
public static class ReadOnlyTokens
{
    /// <summary>What an assistant is told when its read-only token calls a tool that changes something.</summary>
    public const string Refusal =
        "This assistant's ChefBot token is read-only, so it can look things up but not change them. To let it make changes, " +
        "make a token that isn't read-only under Settings, AI assistants.";

    /// <summary>True when <paramref name="user"/> may call a tool with this read-only hint.</summary>
    public static bool Allows(ClaimsPrincipal? user, bool? readOnlyHint) => !Sessions.IsReadOnly(user) || readOnlyHint == true;

    /// <summary>Adds the filters that list and run tools for read-only tokens.</summary>
    public static void Add(IMcpRequestFilterBuilder filters)
    {
        filters.AddListToolsFilter(next => async (context, cancellationToken) =>
        {
            var result = await next(context, cancellationToken);
            if (Sessions.IsReadOnly(context.User))
            {
                result.Tools = result.Tools.Where(tool => Allows(context.User, tool.Annotations?.ReadOnlyHint)).ToList();
            }

            return result;
        });

        filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            Allows(context.User, (context.MatchedPrimitive as McpServerTool)?.ProtocolTool.Annotations?.ReadOnlyHint)
                ? await next(context, cancellationToken)
                : throw new McpException(Refusal));
    }
}
