namespace ChefBot.Api;

/// <summary>
/// The folders ChefBot writes to: session keys with the HTTPS certificate, and backups. Checked at startup, so a
/// mounted folder the app can't write to is reported straight away and plainly, not at the first backup.
/// </summary>
public static class DataFolders
{
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
        "A named volume needs nothing.", exception);
}
