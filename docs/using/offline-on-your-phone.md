# Offline on your phone

The **Shopping** and **Pantry** tabs keep working with no connection, for example in a shop with no signal, and so do
the recipes on the plan. This page shows how to set a phone up for it. It takes a few minutes, once per phone.

## What works offline

- **Shopping:** tick items off, add items, remove the ones you added, and mark everything as bought. Your changes are
  sent to ChefBot as soon as the phone can reach it again. Items count as bought on the day you ticked them.
- **Pantry:** read only.
- **Recipes on the plan:** read them and use **Start cooking**, from the list of recipes under the shopping list
  (**Recipes on this plan**, or **Recipes in the draft** before the plan is confirmed). Photos aren't kept, and editing,
  favourites and reviews need ChefBot. A recipe that isn't on the plan needs ChefBot too.
- **Everything else** needs ChefBot.

When ChefBot can't be reached, a banner says so and shows when the list was last loaded.

## Set up the phone

Phones only keep a web page for offline use over HTTPS, with a certificate they trust. With your own trusted
certificate or a reverse proxy, skip to step 3.

1. **Download ChefBot's certificate on the phone itself:** open ChefBot, go to **Settings → HTTPS** and tap
   **Download the certificate**.
2. **Install and trust it:**
   - **iPhone and iPad:** open the downloaded profile under **Settings → General → VPN & Device Management** (or the
     **Profile Downloaded** line at the top of Settings) and install it. Then turn it on under **Settings → General →
     About → Certificate Trust Settings**.
   - **Android:** **Settings → Security → Encryption & credentials → Install a certificate → CA certificate**, then
     choose the downloaded file. The menu names vary a little between phone makers; search Settings for
     "certificate" if you can't find it.
3. **Open `https://<ChefBot's address>:8443` while connected, and sign in.**
4. Optional: add it to the home screen (Safari's **Share → Add to Home Screen**, or Chrome's menu **→ Add to Home
   screen**), so it opens like an app.

After that, open ChefBot as usual. The page is kept on the phone, and the shopping list is saved each time it loads.

## Good to know

- **Over plain HTTP,** the page still works offline while it stays open, but reloading it needs the connection.
- **Recipes are saved with the plan.** Recipes on the plan you're looking at are saved each time the page loads
  while connected, and again whenever a meal changes. The list says **(not saved on this device)** beside any recipe
  that wasn't saved in time.
- **Signing out** removes the saved list and recipes from the phone.
- **A renewed certificate** has to be installed again. ChefBot's own certificate lasts about two years. See
  [HTTPS and certificates](../admin/https-and-certificates.md).

## If something goes wrong

- The page doesn't load offline at all: the phone doesn't trust the certificate yet, or you're on plain HTTP. See
  [HTTPS problems](../troubleshooting/https-and-certificates.md).
- Ticks made offline didn't arrive: open ChefBot again while connected and leave it open for a moment.
