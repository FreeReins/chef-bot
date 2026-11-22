# Your own certificate

Use a certificate from Let's Encrypt, your router, or your own certificate authority instead of the one ChefBot makes.
Browsers and phones then trust ChefBot with no warning and nothing to install.

## Set it up

1. Put the certificate and key where the container can read them, such as `/srv/chefbot/certs/`.
2. Mount the folder and point ChefBot at the files.

   **Docker Compose:**

   ```yaml
       environment:
         Https__Mode: Certificate
         Https__CertificatePath: /certs/fullchain.pem
         Https__KeyPath: /certs/privkey.pem
       volumes:
         - /srv/chefbot/certs:/certs:ro
   ```

   **Podman:** add a `Volume=` line to `~/.config/containers/systemd/chefbot/chefbot.container` (and to
   `deploy/podman/chefbot.container`, so `install.sh` keeps it):

   ```ini
   Volume=/srv/chefbot/certs:/certs:ro,Z
   ```

   Then set these in `~/.config/chefbot/chefbot.env`:

   ```sh
   Https__Mode=Certificate
   Https__CertificatePath=/certs/fullchain.pem
   Https__KeyPath=/certs/privkey.pem
   ```

   Then run `systemctl --user daemon-reload && systemctl --user restart chefbot`.
3. Open ChefBot at the name the certificate is for.

## Things to get right

- **Use the full chain.** A PEM file holding the certificate followed by its intermediates (Let's Encrypt's
  `fullchain.pem`) is served whole, which phones, `curl` and Home Assistant need.
- **A `.pfx` file works too.** Set `Https__CertificatePath` to it, leave `Https__KeyPath` empty, and set
  `Https__CertificatePassword` if it has one.
- **The key must be readable by user 1654**, the user ChefBot runs as in the container.

## Renewals

ChefBot checks the files every minute and switches to a renewed certificate without a restart. A half-written file is
ignored until it's complete.

Let's Encrypt keys are readable by root only, so give ChefBot its own copy from a certbot deploy hook, such as
`/etc/letsencrypt/renewal-hooks/deploy/chefbot.sh`:

```sh
#!/bin/sh
install -m 644 "$RENEWED_LINEAGE/fullchain.pem" /srv/chefbot/certs/fullchain.pem
install -m 600 -o 1654 "$RENEWED_LINEAGE/privkey.pem" /srv/chefbot/certs/privkey.pem
```

With rootless Podman, user 1654 inside the container is a different id on the host. Leave out `-o 1654` and run
`podman unshare chown 1654 /srv/chefbot/certs/privkey.pem` after copying instead.

## Browsers remember to use HTTPS

With your own certificate and **Always use HTTPS** on, ChefBot sends `Strict-Transport-Security` for a year. Browsers
then go straight to HTTPS without trying HTTP first, and won't let anyone click past a certificate warning for that
address. Keep a valid certificate for that address for at least that long.

A self-signed certificate never gets this header.

## If something goes wrong

ChefBot stops at startup with a message saying what's wrong: a file that's missing (check it's mounted), a key it
can't read (check the owner), or a certificate it can't use (check it's PEM or `.pfx`). Read it with
`journalctl --user -u chefbot` or `docker compose logs chefbot`. See
[HTTPS and certificate problems](../troubleshooting/https-and-certificates.md).
