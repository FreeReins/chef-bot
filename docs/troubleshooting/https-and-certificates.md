# HTTPS and certificate problems

## "Your connection is not private"

Expected with ChefBot's own certificate: nobody but ChefBot vouches for it. Choose **Advanced** and continue, or
install the certificate on the device to make the warning go away. See
[Trust the certificate](../admin/https-and-certificates.md#trust-the-certificate-on-your-devices).

## Still warned after installing the certificate

- **The name doesn't match.** The certificate only works for the names written in it. If you open
  `https://chefbot.local:8443` but the certificate only lists `localhost`, add the name under **Settings → HTTPS** and
  create a new certificate. Then download and install the new one.
- **It's installed but not trusted.** On iPhone, turn it on under **Settings → General → About → Certificate Trust
  Settings**. In Firefox, import it under **Authorities** with **Trust this CA to identify websites** ticked.
- **The browser needs a restart.**

## A phone won't trust the certificate

- Download it on the phone itself, from **Settings → HTTPS → Download the certificate**.
- Android: install it as a **CA certificate**, not a VPN or Wi-Fi certificate.
- iPhone: after installing the profile, also turn on full trust (see above).
- Open ChefBot at the same name the certificate lists.

See [Offline on your phone](../using/offline-on-your-phone.md).

## The certificate was renewed and the warning came back

ChefBot's own certificate is renewed in its last 30 days. A renewed certificate has to be installed again on each
device.

## The browser won't let me click past the warning

The browser has seen `Strict-Transport-Security` from this address before, when it had a real certificate. Put the
real certificate back, or clear the address from the browser's HSTS list (in Chrome, `chrome://net-internals/#hsts`,
**Delete domain security policies**).

## Plain HTTP doesn't redirect to HTTPS

Turn on **Always use HTTPS** under **Settings → HTTPS**. Behind a reverse proxy it stays off, and the proxy redirects
instead.

## Links ChefBot makes have the wrong port

When HTTPS is published on another port on the host, set `Https__PublicPort` to that port. See
[HTTPS settings](../admin/https-and-certificates.md#https-settings).

## Your own certificate isn't used

ChefBot stops at startup with a message saying what's wrong. The usual causes:

- **The file wasn't found:** check it's mounted into the container, at the path in `Https__CertificatePath`.
- **It can't read the key:** the key must be readable by user 1654. See
  [Renewals](../admin/own-certificate.md#renewals).
- **It couldn't be used:** use a PEM certificate (`fullchain.pem`) with its key in `Https__KeyPath`, or a `.pfx` with
  `Https__CertificatePassword`.
