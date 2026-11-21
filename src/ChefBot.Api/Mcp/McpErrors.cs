using ChefBot.Core.Managers.Contracts;
using ModelContextProtocol;

namespace ChefBot.Api.Mcp;

/// <summary>
/// Passes ChefBot's own errors (no such meal, a rule broken, ...) to the assistant with their plain message, so it can
/// say what went wrong. The MCP SDK only sends the message of an <see cref="McpException"/>; anything else becomes a
/// generic error.
/// </summary>
public static class McpErrors
{
    public static async Task<T> Explain<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (ChefBotException error)
        {
            throw new McpException(error.Message, error);
        }
    }

    public static async Task<string> Explain(Func<Task> call, string done)
    {
        try
        {
            await call();
            return done;
        }
        catch (ChefBotException error)
        {
            throw new McpException(error.Message, error);
        }
    }
}
