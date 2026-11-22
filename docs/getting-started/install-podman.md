# Install with Podman

This guide installs ChefBot on Linux as a rootless Podman service that starts by itself, restarts if it stops, logs
to the system journal and updates itself when there's a new release. ChefBot is one container: it runs its own
database inside it. It takes a minute or two.

You need Linux with **Podman 5 or later** and **systemd**, as on Fedora, Bazzite and other Fedora Atomic desktops.
Nothing else.

## Install

1. Run the install script:

   ```sh
   curl -fsSL https://raw.githubusercontent.com/Jamiethenextkevinbaconhamm/chef-bot/main/deploy/podman/install.sh | bash
   ```

   From a clone of the repository, `deploy/podman/install.sh` does the same.

2. Optional but recommended: keep ChefBot running after you log out, and start it when the computer starts:

   ```sh
   loginctl enable-linger $USER
   ```

3. Open <http://localhost:8080> on the same computer, or `http://<this computer's address>:8080` from another
   device. ChefBot sends you on to `https://…:8443`. The browser warns about the certificate the first time:
   choose to continue. [HTTPS and certificates](../admin/https-and-certificates.md) explains why, and how to make
   the warning go away for good.

The setup wizard starts. Continue with [First run](first-run.md).

## What the script does

1. Downloads the ChefBot image from `ghcr.io/jamiethenextkevinbaconhamm/chef-bot`.
2. Copies the settings file to `~/.config/chefbot/chefbot.env`, unless it's already there.
3. Writes the service into `~/.config/containers/systemd/chefbot/` and starts it.
4. Turns on `podman-auto-update.timer`, which checks for a new image once a day and restarts ChefBot with it.

Run it again whenever you like: your settings and data are kept. See [Upgrading](../admin/upgrading.md).

To run code that isn't released yet, build the image from a clone instead: `deploy/podman/install.sh --build`.

## Day-to-day commands

```sh
systemctl --user status chefbot          # is it running?
systemctl --user restart chefbot         # after editing chefbot.env
journalctl --user -u chefbot -f          # follow the log
systemctl --user stop chefbot            # stop it (data is kept)
systemctl --user start chefbot           # start it again
podman auto-update                       # update now instead of waiting for the timer
```

The most recent log entries are also in ChefBot, under **Settings → Logs** (admins only).

## Where things are kept

| What | Where |
|---|---|
| Settings file | `~/.config/chefbot/chefbot.env` |
| Service | `~/.config/containers/systemd/chefbot/` |
| Database, sign-in keys, HTTPS certificate and backups | the `chefbot-data` Podman volume, mounted at `/data`. To keep it in a folder instead, see the comment in `chefbot.container` |

## Change the ports

ChefBot listens on 8080 (HTTP) and 8443 (HTTPS). To use other ports on the host, edit the left-hand numbers in
`~/.config/containers/systemd/chefbot/chefbot.container`:

```ini
PublishPort=80:8080
PublishPort=443:8443
```

If you change the HTTPS port, also set `Https__PublicPort` to it in `chefbot.env`, so ChefBot's links and redirects
use it. Then:

```sh
systemctl --user daemon-reload
systemctl --user restart chefbot
```

Rootless Podman can't use ports below 1024 unless your system allows it. Running `install.sh` again writes the
service file afresh, so make the change again after it.

## If something goes wrong

ChefBot doesn't open: see [Install and startup problems](../troubleshooting/install-and-startup.md).

## Next

[First run](first-run.md): create your account and plan your first week.
