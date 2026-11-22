# HTTPS and certificates

HTTPS keeps passwords and your household's data private as they cross your network. ChefBot serves HTTPS out of the
box with a certificate it makes itself, so there's nothing to install or buy. This page explains the browser warning
you'll see, how to get rid of it, and the HTTPS settings.

For a certificate from Let's Encrypt or your own authority, see [Your own certificate](own-certificate.md). If Caddy,
Traefik or nginx handles HTTPS in front of ChefBot, see [Behind a reverse proxy](reverse-proxy.md).

## How it works out of the box

- ChefBot serves **HTTPS on port 8443** and plain **HTTP on port 8080**.
- On the first start it makes a **self-signed certificate** and keeps it in `keys` in the data folder (`/data/keys` in the container).
- The certificate lasts about **two years** and is renewed by itself in its last 30 days.
- **Always use HTTPS** sends anyone arriving on port 8080 to the HTTPS address. It's on from the start.

## The browser warning

The first visit to `https://<address>:8443` shows a warning such as "Your connection is not private". It appears
because nobody but ChefBot vouches for the certificate. The connection is still encrypted.

- **To carry on now:** choose **Advanced** (or **Show details**) and then continue to the site.
- **To make it go away for good:** install the certificate on each device, as below.

## Trust the certificate on your devices

First make sure the certificate names the address you use (see the next section). Then download it from **Settings →
HTTPS → Download the certificate**, on the device itself, and install it:

| Device | How |
|---|---|
| **iPhone, iPad** | Open the downloaded profile in Settings and install it, then turn it on under **Settings → General → About → Certificate Trust Settings** |
| **Android** | **Settings → Security → Encryption & credentials → Install a certificate → CA certificate**, then choose the file |
| **Windows** | Double-click the file, **Install Certificate → Local Machine → Place all certificates in the following store → Trusted Root Certification Authorities** |
| **macOS** | Double-click the file to add it to Keychain Access, open it, and under **Trust** set **When using this certificate** to **Always Trust** |
| **Linux (Fedora)** | `sudo cp chefbot.crt /etc/pki/ca-trust/source/anchors/ && sudo update-ca-trust` |
| **Linux (Debian, Ubuntu)** | `sudo cp chefbot.crt /usr/local/share/ca-certificates/ && sudo update-ca-certificates` |
| **Firefox** (any system) | **Settings → Privacy & Security → Certificates → View Certificates → Authorities → Import**, and tick **Trust this CA to identify websites** |

Restart the browser afterwards. Firefox uses its own list unless it's set to use the system's.

## Name the addresses you use

A certificate only works for the names written in it. If people reach ChefBot as `chefbot.local`, `192.168.1.20` or
`kitchen.home.arpa`, the certificate has to list those names.

- **In ChefBot:** an admin opens **Settings → HTTPS**, enters the names and addresses in **Names and addresses people
  use to reach ChefBot**, separated by commas, and presses **Create a certificate for these names**. The new
  certificate is used straight away, no restart needed.
- **Before the first start:** set `Https__Hostnames=chefbot.local,192.168.1.20` in `chefbot.env` (or under
  `environment:` in compose).

A new certificate has to be downloaded and installed again on every device that trusted the old one.

## Always use HTTPS

Under **Settings → HTTPS**, **Always use HTTPS** sends anyone arriving over plain HTTP to the HTTPS address, so
passwords never cross the network unencrypted.

- It's on from the start whenever ChefBot serves HTTPS, until an admin changes it. After that, the saved choice
  stays.
- `/health` is never redirected, so uptime monitors can use plain HTTP.

## HTTPS settings

These go in `chefbot.env` (Podman) or under `environment:` (compose), followed by a restart.

| Setting | Default | Meaning |
|---|---|---|
| `Https__Mode` | `SelfSigned` in the container image | `SelfSigned`, `Certificate` ([your own](own-certificate.md)) or `Off` ([behind a proxy](reverse-proxy.md)) |
| `Https__Hostnames` | empty | Names and addresses the first self-signed certificate covers |
| `Https__PublicPort` | `8443` | The HTTPS port people type, if it differs from 8443 on the host |
| `Https__HttpPort`, `Https__Port` | `8080`, `8443` | The ports ChefBot listens on inside the container. Rarely changed |

## If something goes wrong

See [HTTPS and certificate problems](../troubleshooting/https-and-certificates.md).
