# Uninstall

## Podman

```sh
deploy/podman/uninstall.sh                # stop ChefBot and remove its services; keep its data
deploy/podman/uninstall.sh --delete-data  # also delete its data, password, settings and image (asks first)
```

The first form stops the pod and its containers and removes the services from
`~/.config/containers/systemd/chefbot/`. It keeps the `chefbot-db`, `chefbot-keys` and `chefbot-backups` volumes, the
`chefbot-db-password` secret, `chefbot.env` and the image, so `install.sh` brings ChefBot back as it was.

`--delete-data` removes those too, after you type `delete`. Download a backup from **Settings → Backups** first if you
want one. It deletes the password and the database together, because PostgreSQL only takes the password when the
database is first created: a new password wouldn't match an old database.

## Docker Compose

```sh
docker compose down       # stop and remove the containers; keep the data
docker compose down -v    # also delete the volumes: all data, keys and backups
```

Download a backup first if you want to keep anything.
