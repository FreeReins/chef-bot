using System.Diagnostics;
using System.Security.Cryptography;
using ChefBot.Core;
using Npgsql;

namespace ChefBot.Api.Database;

/// <summary>
/// Runs ChefBot's own PostgreSQL, so an install is one container or one Windows program with nothing else to set
/// up. On first start it creates a database in Data:Root/database with a random password, then each start it starts
/// the server on 127.0.0.1 with pg_ctl, and stops it once ChefBot has stopped. Nothing outside the computer can
/// reach it, and only ChefBot knows the password.
/// </summary>
public sealed partial class BundledDatabase
{
    private const string User = "chefbot";
    private const string DatabaseName = "chefbot";

    /// <summary>The settings file PostgreSQL reads on every start, written by ChefBot from Database:Port.</summary>
    private const string SettingsFile = "chefbot.conf";

    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(90);

    private readonly DatabaseOptions options;
    private readonly string binaries;
    private readonly string folder;
    private readonly string password;
    private bool started;

    private BundledDatabase(DatabaseOptions options, string binaries, string folder, string password)
    {
        this.options = options;
        this.binaries = binaries;
        this.folder = folder;
        this.password = password;
    }

    /// <summary>The PostgreSQL data folder.</summary>
    public string DataFolder => Path.Combine(folder, "pgdata");

    /// <summary>The server's log, kept next to the data folder.</summary>
    public string LogFile => Path.Combine(folder, "postgres.log");

    private string PasswordFile => Path.Combine(folder, "password");

    /// <summary>
    /// Points ConnectionStrings:ChefBot at the bundled server unless Database:Mode is External. Call it before
    /// anything reads the connection string. It writes nothing: the database is created by <see cref="StartAsync"/>.
    /// </summary>
    /// <returns>The bundled database to start, or null when ChefBot uses an outside one.</returns>
    public static BundledDatabase? Configure(ConfigurationManager configuration)
    {
        var options = configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
        if (options.Mode == DatabaseMode.External)
        {
            return null;
        }

        var binaries = CoreRegistration.DataPath(configuration, "Database:Binaries", options.Binaries);
        var folder = CoreRegistration.StoragePath(configuration, "Data:Database", "database");
        var passwordFile = Path.Combine(folder, "password");
        // A new password is only kept once the database is created with it.
        var password = File.Exists(passwordFile) ? File.ReadAllText(passwordFile).Trim() : NewPassword();

        configuration["ConnectionStrings:ChefBot"] = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = options.Port,
            Database = DatabaseName,
            Username = User,
            Password = password,
        }.ConnectionString;

        return new BundledDatabase(options, binaries, folder, password);
    }

    /// <summary>
    /// Creates the database on first start, then starts the server and waits until it accepts connections. A server
    /// left running by a ChefBot that didn't stop cleanly is used as it is.
    /// </summary>
    /// <exception cref="InvalidOperationException">PostgreSQL is missing, was made by another version, or won't start.</exception>
    public async Task StartAsync(ILogger logger, CancellationToken cancellationToken = default)
    {
        RequireBinaries();
        Directory.CreateDirectory(folder);

        var bundledVersion = await BundledMajorVersionAsync(cancellationToken);
        if (!File.Exists(Path.Combine(DataFolder, "PG_VERSION")))
        {
            await CreateAsync(logger, cancellationToken);
        }

        RequireSameVersion(bundledVersion);
        if (!File.Exists(PasswordFile))
        {
            throw new InvalidOperationException(
                $"ChefBot's database password file {PasswordFile} is missing, so it can't open its database. Put the " +
                "file back from a copy of the data folder, or move the database folder aside and restore a backup " +
                "into a new one.");
        }

        await File.WriteAllTextAsync(Path.Combine(DataFolder, SettingsFile), ServerSettings(), cancellationToken);
        if (await RunAsync("pg_ctl", ["status", "-D", DataFolder], cancellationToken) is { ExitCode: 0 })
        {
            logger.LogInformation("Using the bundled PostgreSQL that was already running in {Folder}.", DataFolder);
        }
        else
        {
            RotateLog();
            logger.LogInformation("Starting the bundled PostgreSQL {Version} on 127.0.0.1:{Port}.", bundledVersion, options.Port);
            // Not captured: the server pg_ctl starts would keep the output open, and its own output goes to the log.
            var start = await RunAsync(
                "pg_ctl", ["start", "--wait", "--timeout=60", "-D", DataFolder, "-l", LogFile], cancellationToken, capture: false);
            if (start.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"ChefBot's bundled PostgreSQL didn't start. The end of its log ({LogFile}):{Environment.NewLine}{LogTail()}");
            }
        }

        started = true;
        await CreateChefBotDatabaseAsync(cancellationToken);
        StartForwardingLog(logger);
    }

    /// <summary>Stops the server quickly and cleanly. Safe to call more than once, and when it never started.</summary>
    public async Task StopAsync(ILogger logger)
    {
        if (!started)
        {
            return;
        }

        started = false;
        StopForwardingLog();
        var stop = await RunAsync("pg_ctl", ["stop", "--wait", "--timeout=30", "-D", DataFolder, "-m", "fast"], CancellationToken.None);
        if (stop.ExitCode == 0)
        {
            logger.LogInformation("Stopped the bundled PostgreSQL.");
        }
        else
        {
            logger.LogWarning("The bundled PostgreSQL didn't stop cleanly; it checks its data when it next starts. {Output}", stop.Output);
        }
    }

    private static string NewPassword() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    private void RequireBinaries()
    {
        if (!File.Exists(Tool("pg_ctl")) || !File.Exists(Tool("initdb")))
        {
            throw new InvalidOperationException(
                $"ChefBot's bundled PostgreSQL wasn't found in {binaries}. Reinstall ChefBot, or set Database__Mode=External " +
                "and ConnectionStrings__ChefBot to use a PostgreSQL server of your own.");
        }
    }

    /// <summary>The major version of the bundled server, from pg_ctl --version ("pg_ctl (PostgreSQL) 17.6").</summary>
    private async Task<int> BundledMajorVersionAsync(CancellationToken cancellationToken)
    {
        var result = await RunAsync("pg_ctl", ["--version"], cancellationToken);
        var version = result.Output.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        return int.TryParse(version.Split('.')[0], out var major)
            ? major
            : throw new InvalidOperationException($"ChefBot couldn't tell the version of its bundled PostgreSQL from '{result.Output.Trim()}'.");
    }

    private void RequireSameVersion(int bundledVersion)
    {
        var made = File.ReadAllText(Path.Combine(DataFolder, "PG_VERSION")).Trim();
        if (made != bundledVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException(
                $"The database in {DataFolder} was made by PostgreSQL {made}, but this copy of ChefBot comes with " +
                $"PostgreSQL {bundledVersion}. Go back to the version of ChefBot you had, or move the database folder aside, " +
                "start this version, and restore your latest backup in Settings, Backups.");
        }
    }

    /// <summary>Creates the data folder with initdb: ChefBot's user and password, and text that sorts the same everywhere.</summary>
    private async Task CreateAsync(ILogger logger, CancellationToken cancellationToken)
    {
        if (Directory.Exists(DataFolder) && Directory.EnumerateFileSystemEntries(DataFolder).Any())
        {
            throw new InvalidOperationException(
                $"{DataFolder} has files in it but isn't a PostgreSQL database, perhaps from a first start that was " +
                "interrupted. If ChefBot has never run with this data folder, delete that folder and start ChefBot again.");
        }

        logger.LogInformation("Creating ChefBot's database in {Folder}. This takes a few seconds, once.", DataFolder);
        await WritePrivateAsync(PasswordFile, password, cancellationToken);
        var passwordInput = Path.Combine(folder, $".initdb-{Guid.NewGuid():N}");
        await WritePrivateAsync(passwordInput, password, cancellationToken);
        try
        {
            // The builtin C.UTF-8 locale (PostgreSQL 17) behaves the same on Linux and Windows and folds case for
            // every language, which ILIKE searches rely on.
            var result = await RunAsync(
                "initdb",
                [
                    "-D", DataFolder, "-U", User, "--auth=scram-sha-256", $"--pwfile={passwordInput}", "-E", "UTF8",
                    "--locale=C", "--locale-provider=builtin", "--builtin-locale=C.UTF-8", "--no-instructions",
                ],
                cancellationToken);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"ChefBot couldn't create its database in {DataFolder}: {result.Output.Trim()}");
            }
        }
        finally
        {
            File.Delete(passwordInput);
        }

        await File.AppendAllTextAsync(
            Path.Combine(DataFolder, "postgresql.conf"),
            $"{Environment.NewLine}# Written by ChefBot on every start.{Environment.NewLine}include_if_exists = '{SettingsFile}'{Environment.NewLine}",
            cancellationToken);
    }

    /// <summary>Only ChefBot on this computer can connect: no network address but 127.0.0.1, and no socket files.</summary>
    private string ServerSettings() => string.Join(
        Environment.NewLine,
        "# ChefBot writes this file each time it starts PostgreSQL. Change Database:Port in ChefBot's settings instead.",
        "listen_addresses = '127.0.0.1'",
        $"port = {options.Port}",
        "unix_socket_directories = ''",
        "max_connections = 50",
        // ChefBot's queries are small; the container image leaves out the JIT compiler.
        "jit = off",
        "log_line_prefix = '%m [%p] '",
        string.Empty);

    /// <summary>initdb only makes the postgres database; ChefBot's is created once the server is up.</summary>
    private async Task CreateChefBotDatabaseAsync(CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = options.Port,
            Database = "postgres",
            Username = User,
            Password = password,
            Pooling = false,
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", DatabaseName);
        if (await exists.ExecuteScalarAsync(cancellationToken) is null)
        {
            await using var create = new NpgsqlCommand($"CREATE DATABASE {DatabaseName}", connection);
            await create.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task WritePrivateAsync(string path, string text, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(path, text, cancellationToken);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private string Tool(string name) => Path.Combine(binaries, OperatingSystem.IsWindows() ? $"{name}.exe" : name);

    private async Task<ToolResult> RunAsync(string tool, IReadOnlyList<string> arguments, CancellationToken cancellationToken, bool capture = true)
    {
        var start = new ProcessStartInfo(Tool(tool))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = capture,
            RedirectStandardError = capture,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"ChefBot couldn't run {start.FileName}.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ToolTimeout);
        var output = capture ? ReadAllAsync(process, timeout.Token) : Task.FromResult(string.Empty);
        await process.WaitForExitAsync(timeout.Token);
        return new ToolResult(process.ExitCode, await output);
    }

    private static async Task<string> ReadAllAsync(Process process, CancellationToken cancellationToken)
    {
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        return await output + await error;
    }

    private sealed record ToolResult(int ExitCode, string Output);
}
