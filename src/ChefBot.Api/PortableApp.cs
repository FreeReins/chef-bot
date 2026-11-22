using System.Diagnostics;

namespace ChefBot.Api;

/// <summary>
/// The portable Windows app: one ChefBot.exe, built with the ChefBot.Portable switch (deploy/windows/build.ps1). It
/// unpacks itself to a temporary folder, so it keeps its data in a ChefBot-data folder next to the exe, and when
/// someone opens it by hand it says where ChefBot is and opens it in the browser.
/// </summary>
public static class PortableApp
{
    /// <summary>True in the portable build.</summary>
    public static bool IsPortable => AppContext.TryGetSwitch("ChefBot.Portable", out var portable) && portable;

    /// <summary>ChefBot-data next to ChefBot.exe.</summary>
    public static string DataRoot =>
        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory, "ChefBot-data");

    /// <summary>Says where ChefBot is and opens it in the browser, when it is the portable app opened by someone.</summary>
    public static void Announce(IConfiguration configuration, ILogger logger)
    {
        if (!IsPortable || !Environment.UserInteractive)
        {
            return;
        }

        var address = $"http://localhost:{configuration.GetValue("Https:HttpPort", 8080)}";
        logger.LogWarning(
            "ChefBot is running at {Address}. Its data is in {Folder}. Close this window to stop ChefBot.",
            address, configuration["Data:Root"]);
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            logger.LogInformation("Open {Address} in your browser: ChefBot couldn't open it for you ({Reason}).", address, exception.Message);
        }
    }
}
