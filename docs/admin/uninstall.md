# Uninstall

## Podman

```sh
deploy/podman/uninstall.sh                # stop ChefBot and remove its service; keep its data
deploy/podman/uninstall.sh --delete-data  # also delete its data, settings and image (asks first)
```

Without a clone, download the script and run it the same way:
`curl -fsSL https://raw.githubusercontent.com/Jamiethenextkevinbaconhamm/chef-bot/main/deploy/podman/uninstall.sh | bash`.

The first form stops ChefBot and removes its service from `~/.config/containers/systemd/chefbot/`. It keeps the
`chefbot-data` volume, `chefbot.env` and the image, so `install.sh` brings ChefBot back as it was.

`--delete-data` removes those too, after you type `delete`. Download a backup from **Settings → Backups** first if you
want one.

## Docker Compose

```sh
docker compose down       # stop and remove the container; keep the data
docker compose down -v    # also delete the chefbot-data volume: all data, keys and backups
```

Download a backup first if you want to keep anything.

## Windows

Uninstall **ChefBot** from **Settings → Apps → Installed apps**. That removes the program and the ChefBot service, but
keeps `C:\ProgramData\ChefBot`, so installing again brings everything back. To remove your data too, delete that
folder afterwards.

The portable app installs nothing: delete `ChefBot-….exe` and the `ChefBot-data` folder next to it.
