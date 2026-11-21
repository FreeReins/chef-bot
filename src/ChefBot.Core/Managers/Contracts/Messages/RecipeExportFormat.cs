using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The kind of file recipes are exported as.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeExportFormat>))]
public enum RecipeExportFormat
{
    /// <summary>schema.org JSON-LD, which other recipe apps and ChefBot's own import read. The default.</summary>
    Json,

    /// <summary>A PDF to print or keep: text only, no photos.</summary>
    Pdf,

    /// <summary>Markdown, for notes apps.</summary>
    Markdown,

    /// <summary>Plain text, as sharing a recipe gives.</summary>
    Text,
}
