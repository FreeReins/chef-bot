# Install with Podman

This guide installs ChefBot on Linux as rootless Podman services that start by themselves, restart if they stop
and log to the system journal. It takes a few minutes, most of it building the image.

You need Linux with **Podman 5 or later** and **systemd**, as on Fedora, Bazzite and other Fedora Atomic desktops,
plus **git** to download ChefBot. Nothing else.

## Install

1. Download ChefBot:

   ```sh
   git clone https://github.com/Jamiethenextkevinbaconhamm/chef-bot.git
   cd chef-bot
   ```

2. Run the install script:

   ```sh
   deploy/podman/install.sh
   ```

3. Optional but recommended: keep ChefBot running after you log out, and start it when the computer starts:

   ```sh
   loginctl enable-linger $USER
   ```

4. Open <http://localhost:8080> on the same computer, or `http://<this computer's address>:8080` from another
   device. ChefBot sends you on to `https://…:8443`. The browser warns about the certificate the first time:
   choose to continue. [HTTPS and certificates](../admin/https-and-certificates.md) explains why, and how to make
   the warning go away for good.

The setup wizard starts. Continue with [First run](first-run.md).

## What the script does

1. Builds the ChefBot image from the repository, after running its tests.
2. Creates a random database password and stores it as the Podman secret `chefbot-db-password`.
3. Copies the settings file to `~/.config/chefbot/chefbot.env`, unless it's already there.
4. Installs the services into `~/.config/containers/systemd/chefbot/` and starts them.

Run it again whenever you want to update. Your settings, data and password are kept. See
[Upgrading](../admin/upgrading.md).

## Day-to-day commands

```sh
systemctl --user status chefbot          # is it running?
systemctl --user restart chefbot         # after editing chefbot.env
journalctl --user -u chefbot -f          # follow the app's log (chefbot-db for the database)
systemctl --user stop chefbot-pod        # stop the app and the database
systemctl --user start chefbot-pod       # start them again
```

The most recent log entries are also in ChefBot, under **Settings → Logs** (admins only).

## Where things are kept

| What | Where |
|---|---|
| Settings file | `~/.config/chefbot/chefbot.env` |
| Services | `~/.config/containers/systemd/chefbot/` |
| Database | the `chefbot-db` Podman volume |
| Sign-in keys and HTTPS certificate | the `chefbot-keys` volume |
| Backups | the `chefbot-backups` volume. To keep them in a folder instead, see [Backups](../admin/backups-and-restore.md#keep-backups-in-a-folder) |
| Database password | the `chefbot-db-password` Podman secret |

## Change the ports

ChefBot listens on 8080 (HTTP) and 8443 (HTTPS). To use other ports on the host, edit the left-hand numbers in
`~/.config/containers/systemd/chefbot/chefbot.pod`:

```ini
PublishPort=80:8080
PublishPort=443:8443
```

If you change the HTTPS port, also set `Https__PublicPort` to it in `chefbot.env`, so ChefBot's links and redirects
use it. Then:

```sh
systemctl --user daemon-reload
systemctl --user restart chefbot-pod
```

Rootless Podman can't use ports below 1024 unless your system allows it. Running `install.sh` again copies the
units from the repository, so make the same change in `deploy/podman/chefbot.pod` too.

## If something goes wrong

- The build fails: the script stops and prints the failing test or step. Make sure you have the latest code
  (`git pull`) and run the script again.
- ChefBot doesn't open: see [Install and startup problems](../troubleshooting/install-and-startup.md).

## Next

[First run](first-run.md): create your account and plan your first week.
