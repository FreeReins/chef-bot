using ChefBot.Core;
using Microsoft.Extensions.Configuration.Json;

namespace ChefBot.Api;

/// <summary>
/// The folders ChefBot writes to, all under Data:Root: the bundled database, session keys with the HTTPS certificate,
/// and backups. Checked at startup, so a mounted folder the app can't write to is reported straight away and plainly,
/// not at the first backup.
/// </summary>
public static class DataFolders
{
    /// <summary>The optional settings file in Data:Root. It overrides appsettings.json and is overridden by environment variables.</summary>
    public const string SettingsFile = "settings.json";

    /// <summary>
    /// Settles Data:Root (the portable Windows app keeps its data next to itself), then reads settings.json from it
    /// when there is one. Call it straight after creating the builder, before anything reads the settings.
    /// </summary>
    public static void UseDataRoot(ConfigurationManager configuration)
    {
        if (PortableApp.IsPortable && configuration["Data:Root"] is not { Length: > 0 })
        {
            configuration["Data:Root"] = PortableApp.DataRoot;
        }

        var root = CoreRegistration.DataRoot(configuration);
        if (!File.Exists(Path.Combine(root, SettingsFile)))
        {
            return;
        }

        // After the appsettings files, so environment variables and the command line still win.
        var sources = ((IConfigurationBuilder)configuration).Sources;
        var lastJson = sources.Select((source, index) => (source, index)).LastOrDefault(pair => pair.source is JsonConfigurationSource).index;
        var settings = new JsonConfigurationSource { Path = SettingsFile, Optional = true };
        settings.FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(root);
        sources.Insert(lastJson + 1, settings);
    }

    /// <summary>The account ChefBot runs as. In the container image this is "app", user id 1654.</summary>
    public static string CurrentUser => Environment.UserName;

    /// <summary>Creates the folder if needed and checks a file can be written to it.</summary>
    /// <exception cref="InvalidOperationException">The folder can't be created or written to.</exception>
    public static void RequireWritable(string folder, string setting)
    {
        var probe = Path.Combine(folder, $".write-check-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            throw CannotWrite(folder, setting, exception);
        }
    }

    public static InvalidOperationException CannotWrite(string folder, string setting, Exception exception) => new(
        $"ChefBot can't write to '{folder}' ({setting}): {exception.Message} It runs as user {CurrentUser} " +
        "(user id 1654 in the container image). For a host folder mounted into the container, give it to that user: " +
        "'sudo chown 1654 <folder>' with Docker, or 'podman unshare chown 1654 <folder>' with rootless Podman. " +
        "A named volume needs nothing. On Windows, the ChefBot service needs to be able to change C:\\ProgramData\\ChefBot.", exception);
}
