using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ModelContextProtocol;

namespace ChefBot.Api.Mcp;

/// <summary>
/// The JSON shapes of the MCP tools' own parameters, made at build time. Their results are ChefBot's contracts,
/// read with <see cref="ContractJsonContext"/>, and the protocol's own messages use the SDK's context.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(DateOnly))]
[JsonSerializable(typeof(DateOnly?))]
[JsonSerializable(typeof(decimal?))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(IReadOnlyList<Guid>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(MealStatus))]
[JsonSerializable(typeof(RecipeCourse?))]
public sealed partial class McpJsonContext : JsonSerializerContext
{
    /// <summary>Options for the tools: their parameters, ChefBot's contracts, then the protocol's own types.</summary>
    public static JsonSerializerOptions ToolOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
            ContractJsonContext.Default, Default, McpJsonUtilities.DefaultOptions.TypeInfoResolver),
    };
}
