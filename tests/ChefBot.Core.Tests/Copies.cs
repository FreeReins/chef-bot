using System.Text.Json;

namespace ChefBot.Core.Tests;

internal static class Copies
{
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = false };

    public static T Of<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json)!;
}
