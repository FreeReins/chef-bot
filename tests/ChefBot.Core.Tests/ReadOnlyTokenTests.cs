using System.Reflection;
using System.Security.Claims;
using ChefBot.Api;
using ChefBot.Api.Mcp;
using ChefBot.Core.Managers.Contracts.Messages;
using ModelContextProtocol.Server;

namespace ChefBot.Core.Tests;

/// <summary>An assistant with a read-only token can look things up but not change them.</summary>
public class ReadOnlyTokenTests
{
    private static readonly SessionInfo Owen = new(Guid.CreateVersion7(), "owen", false, 1);

    /// <summary>Every MCP tool, by name, and whether it is marked read-only.</summary>
    private static readonly Dictionary<string, bool> Tools = typeof(ReadOnlyTokens).Assembly.GetTypes()
        .SelectMany(type => type.GetMethods())
        .Select(method => method.GetCustomAttribute<McpServerToolAttribute>())
        .OfType<McpServerToolAttribute>()
        .ToDictionary(tool => tool.Name!, tool => tool.ReadOnly);

    private static ClaimsPrincipal Signed(bool readOnly) => Sessions.Principal(Owen, ApiTokenAuthentication.SchemeName, readOnly);

    [Test]
    public async Task A_read_only_token_can_use_only_the_tools_that_change_nothing()
    {
        var allowed = Tools.Where(tool => ReadOnlyTokens.Allows(Signed(readOnly: true), tool.Value)).Select(tool => tool.Key).ToList();

        await Assert.That(allowed).Contains("search_recipes");
        await Assert.That(allowed).Contains("get_current_plan");
        await Assert.That(allowed).DoesNotContain("confirm_plan");
        await Assert.That(allowed).DoesNotContain("add_shopping_item");
        await Assert.That(allowed.Count).IsEqualTo(Tools.Count(tool => tool.Value));
    }

    [Test]
    public async Task Other_tokens_can_use_every_tool() =>
        await Assert.That(Tools.All(tool => ReadOnlyTokens.Allows(Signed(readOnly: false), tool.Value))).IsTrue();

    [Test]
    public async Task A_tool_not_marked_read_only_is_refused()
    {
        await Assert.That(ReadOnlyTokens.Allows(Signed(readOnly: true), null)).IsFalse();
        await Assert.That(Sessions.IsReadOnly(Signed(readOnly: true))).IsTrue();
        await Assert.That(Sessions.IsReadOnly(Signed(readOnly: false))).IsFalse();
    }
}
