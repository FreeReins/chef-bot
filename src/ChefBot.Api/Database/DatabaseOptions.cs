namespace ChefBot.Api.Database;

/// <summary>The Database section of the settings.</summary>
public sealed class DatabaseOptions
{
    /// <summary>Bundled (the default) or External.</summary>
    public DatabaseMode Mode { get; set; } = DatabaseMode.Bundled;

    /// <summary>
    /// The port the bundled PostgreSQL listens on, on 127.0.0.1 only. Unusual on purpose, so it doesn't clash with a
    /// PostgreSQL already on the same computer.
    /// </summary>
    public int Port { get; set; } = 54329;

    /// <summary>The folder with initdb and pg_ctl. A relative path is relative to the application folder.</summary>
    public string Binaries { get; set; } = "pgsql/bin";
}
