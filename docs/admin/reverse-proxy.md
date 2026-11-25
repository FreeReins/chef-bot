# Behind a reverse proxy

When Caddy, Traefik, nginx or another reverse proxy handles HTTPS, let it talk to ChefBot over plain HTTP. This is
also the safe way to reach ChefBot from outside your home: the proxy gets a real certificate, and ChefBot stays on
your network.

## Set ChefBot up

Turn ChefBot's own HTTPS off, and tell it which proxy to believe:

**Docker Compose:**

```yaml
    environment:
      Https__Mode: "Off"
      Https__TrustedProxies: 172.16.0.0/12   # the proxy's address or network
```

**Podman** (`~/.config/chefbot/chefbot.env`, then `systemctl --user restart chefbot`):

```sh
Https__Mode=Off
Https__TrustedProxies=10.0.0.5
```

- **`Https__TrustedProxies`** lists the proxies whose `X-Forwarded-For` and `X-Forwarded-Proto` headers ChefBot
  believes: addresses (`10.0.0.5`) or networks (`172.16.0.0/12`), separated by commas. With it, ChefBot knows
  visitors arrived over HTTPS, marks sign-in cookies secure, and **Settings → HTTPS** says HTTPS is handled by a
  reverse proxy.
- **Leave it empty when nothing sits in front of ChefBot.** Otherwise anyone could claim to be on HTTPS.
- **Point the proxy at port 8080.** Port 8443 isn't used in this mode.
- **"Always use HTTPS" stays off.** Redirecting to HTTPS, and sending `Strict-Transport-Security`, is the proxy's job.

Which address to list: with Docker Compose, the proxy's container network (Docker's default networks are inside
`172.16.0.0/12`). With the proxy on the same machine outside containers, the address the container sees it come
from. If **Settings → HTTPS** says "HTTPS is switched off" rather than "handled by a reverse proxy" while you're on
HTTPS, the listed address doesn't match the proxy.

## What the proxy must do

- **Pass the original `Host` header**, so links ChefBot makes (invites, the calendar feed) use your public name, and
  so older browsers' changes are recognised as coming from ChefBot's own page.
- **Send `X-Forwarded-For` and `X-Forwarded-Proto`.**
- **Allow large uploads:** a Mealie backup can be up to 200 MB.
- **Don't buffer `/api/live`.** It's a live stream that tells open pages when something changes on another device.
  ChefBot sends `X-Accel-Buffering: no` for nginx and a ping every 25 seconds to keep it open. Without it, pages
  still work, but changes from other devices only show after a reload.

Caddy and Traefik do all of this by themselves. nginx needs the `Host`, forwarded-header and upload-size lines below.

## Caddy

```caddy
chefbot.example.com {
    reverse_proxy chefbot:8080
}
```

## nginx

```nginx
server {
    listen 443 ssl;
    server_name chefbot.example.com;
    # ssl_certificate and ssl_certificate_key as usual

    client_max_body_size 200m;

    location / {
        proxy_pass http://chefbot:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## Traefik

Labels on the ChefBot service in compose:

```yaml
    labels:
      - traefik.enable=true
      - traefik.http.routers.chefbot.rule=Host(`chefbot.example.com`)
      - traefik.http.routers.chefbot.entrypoints=websecure
      - traefik.http.routers.chefbot.tls.certresolver=letsencrypt
      - traefik.http.services.chefbot.loadbalancer.server.port=8080
```

## Before you open it to the internet

ChefBot is built for a home network. It has password hashing, lockout and HTTPS, but no two-step sign-in. Before you
expose it:

- use a strong password for every account;
- keep the [lockout](people-and-accounts.md#lockout) on, and set `Https__TrustedProxies` so the limit of 10 sign-in
  tries a minute counts each visitor, not the proxy;
- give AI assistants [read-only tokens that expire](ai-assistants.md#tokens) where you can;
- consider a VPN (WireGuard, [Tailscale](tailscale.md)) or your proxy's own sign-in in front of it instead.

Exposing it does let Google Calendar and Outlook on the web reach the [calendar feed](../using/calendar.md), and
cloud AI assistants reach [MCP](ai-assistants.md).
