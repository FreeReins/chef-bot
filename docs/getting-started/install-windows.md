# Install on Windows

This guide installs ChefBot on a Windows PC with no Docker or containers: one installer that runs ChefBot in the
background, starting with Windows, with its database inside. It takes a minute. There's also a portable app for trying
ChefBot without installing anything.

You need **Windows 10 or 11, 64-bit** (x86, not ARM), and an administrator account to install.

## Install

1. Download `ChefBot-<version>-win-x64.msi` from the
   [latest release](https://github.com/FreeReins/chef-bot/releases/latest).
2. Open it. If Windows says **Windows protected your PC**, press **More info**, check that the file name is the one
   you downloaded, then press **Run anyway**. Windows shows this for programs it hasn't seen often.
3. Accept the licence and press **Install**, then **Yes** when Windows asks for permission.
4. Leave **Open ChefBot in your browser** ticked and press **Finish**.

ChefBot opens at <http://localhost:8080> and sends you on to `https://localhost:8443`. The browser warns about the
certificate the first time: choose to continue. [HTTPS and certificates](../admin/https-and-certificates.md) explains
why, and how to make the warning go away for good.

The setup wizard starts. Continue with [First run](first-run.md).

## What the installer does

1. Installs ChefBot in `C:\Program Files\ChefBot`.
2. Sets up the **ChefBot** service, which starts with Windows (shortly after the desktop appears), restarts if it
   stops, and runs without anyone signed in.
3. Keeps everything ChefBot saves in `C:\ProgramData\ChefBot`: the database, sign-in keys, HTTPS certificate and
   backups.
4. Lets phones and other computers reach ports 8080 and 8443 through the Windows firewall on **private** networks.
5. Adds **ChefBot** and **ChefBot data folder** to the Start menu.

## Use it from your phone

Open `http://<this PC's name>:8080` on the phone, such as `http://kitchen-pc:8080`, or use the PC's address, which
`ipconfig` shows as **IPv4 Address**. The phone must be on the same network.

If it doesn't open, Windows may count your home network as **Public**, where the firewall blocks ChefBot. Open
**Settings → Network & internet**, choose your connection, and set **Network profile type** to **Private network**.

The PC must be on, and not asleep, for ChefBot to answer. A PC that sleeps after a few minutes isn't a good home for
ChefBot; change it under **Settings → System → Power**.

## Settings

Most settings are in ChefBot's web page. The few read at startup, such as the names in the HTTPS certificate, go in
`C:\ProgramData\ChefBot\settings.json` (create it in Notepad as an administrator). See the
[settings reference](../admin/settings.md#settings-in-the-environment). Then restart the service in the **Services**
app, or in PowerShell as an administrator:

```powershell
Restart-Service ChefBot
```

## Day-to-day

| To | Do |
|---|---|
| Check it's running | **Services** app, **ChefBot**; or `Get-Service ChefBot` in PowerShell |
| Stop or start it | **Services** app; or `Stop-Service ChefBot` / `Start-Service ChefBot` as an administrator |
| Read the log | ChefBot's **Settings → Logs**, or **Event Viewer → Windows Logs → Application**, source **ChefBot** |
| Update | Run the new version's `.msi`. See [Upgrading](../admin/upgrading.md) |
| Uninstall | See [Uninstall](../admin/uninstall.md#windows) |

## The portable app

`ChefBot-<version>-win-x64-portable.exe`, from the same release, is ChefBot in one file. Put it in a folder of its
own and open it: a window opens and ChefBot opens in the browser. It keeps its data in a `ChefBot-data` folder next to
the file, and runs until you close the window.

It doesn't start with Windows or open the firewall, so it's best for trying ChefBot out. To move to the installed
version, make a backup under **Settings → Backups**, install, and restore it there.

## If something goes wrong

See [Install and startup problems](../troubleshooting/install-and-startup.md). The PostgreSQL log, in
`C:\ProgramData\ChefBot\database\postgres.log`, says more when the database won't start.

## Next

[First run](first-run.md): create your account and plan your first week.
