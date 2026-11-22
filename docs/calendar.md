# The meal plan in your calendar

ChefBot can publish the meal plan as a calendar feed. Calendar apps subscribe to it and keep it up to date by
themselves, so each confirmed week's meals show up next to everything else in your calendar.

## What the calendar shows

- **Breakfasts, lunches and dinners** of each confirmed week, from last week on, at mealtimes: 08:00, 12:30 and
  18:30 unless your ChefBot is set up differently (see [Changing the mealtimes](#changing-the-mealtimes)). Each
  event lasts 45 minutes and is marked as free time, so it doesn't block your calendar.
- **Leftover lunches** are marked "(leftovers)".
- **The portions, and a link to the recipe** in each event's notes. The link opens the recipe in ChefBot.
- **A reminder to start the slow cooker**, 8 hours before a slow cooker dinner.
- **Not in the calendar:** draft weeks (until they are confirmed), skipped meals, and snacks, sweets and drinks,
  since those have no day.

Times have no time zone attached, so a dinner at 18:30 shows at 18:30 wherever your phone is. Calendar apps check
the feed for changes about once an hour, though some check less often (see each app below).

## Before you start

1. **Turn the feed on.** An admin opens **Settings → Calendar** in ChefBot and presses **Turn on the calendar
   feed**. This only needs doing once for the household.
2. **Get the address.** Anyone signed in can open **Settings → Calendar** and press **Copy address**. On the
   device you want the calendar on, **Subscribe on this device** opens your calendar app directly instead.
3. **Make sure your calendar app can reach ChefBot.** The calendar app fetches the feed itself. Apps that fetch
   from your phone or computer work on your home network. Apps that fetch from their company's servers (Google
   Calendar, Outlook on the web) can only reach a ChefBot that is on the internet. The table shows which is which.
4. **If ChefBot uses its own certificate**, the device must trust it first, or the calendar app will refuse to
   connect. Download the certificate from ChefBot's **Settings → HTTPS** and install it on the device, as the
   [HTTPS section of the README](../README.md#https) describes.

| Calendar app | Fetches the feed from | Works with ChefBot only on your home network |
|---|---|---|
| [Apple Calendar on iPhone and iPad](#iphone-and-ipad) | The device | Yes |
| [Apple Calendar on Mac](#mac) | The Mac, if the calendar is kept "On My Mac" | Yes |
| [Android, with ICSx⁵](#android) | The phone | Yes |
| [Thunderbird](#thunderbird) | The computer | Yes |
| [GNOME Calendar](#gnome-calendar) | The computer | Yes |
| [Outlook for Windows (classic)](#outlook-for-windows-classic) | The computer, or Microsoft's servers with some accounts | Sometimes |
| [Google Calendar](#google-calendar) | Google's servers | No |
| [Outlook on the web, Outlook.com and the new Outlook](#outlook-on-the-web-outlookcom-and-the-new-outlook) | Microsoft's servers | No |

The address contains a secret instead of a password, because calendar apps can't sign in. Anyone with the address
can read the meal plan, and nothing else in ChefBot, so share it only with your household. If it gets out, an
admin can press **Make a new address** in **Settings → Calendar**. The old address stops working at once, and
every calendar needs the new one added in its place.

## iPhone and iPad

The quickest way: open ChefBot in Safari on the iPhone, go to **Settings → Calendar** and tap **Subscribe on this
device**, then tap **Subscribe**.

To add it by hand:

1. Copy the address from ChefBot's **Settings → Calendar**.
2. Open the **Settings** app, then **Apps → Calendar → Calendar Accounts → Add Account**. On iOS 17 and earlier
   it is **Calendar → Accounts → Add Account**.
3. Tap **Other**, then **Add Subscribed Calendar**.
4. Paste the address into **Server** and tap **Next**.
5. Change the description to something like "Meals" if you like, then tap **Save**.

The meals then appear in the Calendar app under that name. Turn the calendar off and on in the Calendar app's
**Calendars** list. With ChefBot's own certificate, trust it first: after installing it, turn it on under
**Settings → General → About → Certificate Trust Settings**.

## Mac

1. Copy the address from ChefBot's **Settings → Calendar**.
2. In the Calendar app, choose **File → New Calendar Subscription**.
3. Paste the address and click **Subscribe**.
4. Give it a name such as "Meals", and set **Auto-refresh** to **Every hour**.
5. Set **Location** to **On My Mac**. If you choose iCloud, Apple's servers fetch the feed instead of your Mac,
   which only works when ChefBot is on the internet.
6. Click **OK**.

A calendar kept On My Mac stays on that Mac. Add it on the iPhone separately, as above.

## Android

Android's calendar apps, Google Calendar and Samsung Calendar among them, can't subscribe to a feed address
themselves. The free app **ICSx⁵** (from Google Play or F-Droid) does it for them: it fetches the feed on the
phone and adds the meals to the phone's calendar, where every calendar app shows them.

1. Install **ICSx⁵** and open it.
2. Tap **+**, choose **Subscribe to calendar**, and paste the address from ChefBot's **Settings → Calendar**.
3. Tap **→**. If ChefBot uses its own certificate, ICSx⁵ asks whether to accept it. Check the details and accept.
4. Name the calendar, such as "Meals", pick a colour, and tap **✓**.
5. Open your calendar app. If the meals don't show, turn on the new calendar in the app's list of calendars.

ICSx⁵ checks the feed on a schedule. Change how often in ICSx⁵'s settings (**Sync interval**).

These meals stay on the phone. They aren't copied into your Google account, so they don't appear in Google
Calendar on the web.

## Google Calendar

Google Calendar fetches feeds from Google's servers, not from your phone or computer. It can only subscribe when
ChefBot is reachable from the internet, at an address with a certificate from a public certificate authority,
such as through a reverse proxy (see [Behind a reverse proxy](../README.md#behind-a-reverse-proxy)). On a home
network only, use [ICSx⁵](#android) on Android instead.

1. Copy the address from ChefBot's **Settings → Calendar**.
2. On a computer, open [calendar.google.com](https://calendar.google.com). Adding a feed isn't possible in the
   phone app.
3. In the side panel, press **+** next to **Other calendars**, then choose **From URL**.
4. Paste the address and press **Add calendar**.

The calendar then appears under **Other calendars** and on your phones signed in to the same Google account.
Google decides how often to check the feed, often only every few hours, so changes to the plan can take a while
to show.

## Outlook on the web, Outlook.com and the new Outlook

These fetch feeds from Microsoft's servers, so like Google Calendar they only work when ChefBot is reachable from
the internet with a publicly trusted certificate.

1. Copy the address from ChefBot's **Settings → Calendar**.
2. Open the calendar and choose **Add calendar**.
3. Choose **Subscribe from web**, paste the address, and give it a name such as "Meals".
4. Choose **Import**.

Microsoft can take several hours to pick up changes to the plan.

## Outlook for Windows (classic)

1. Copy the address from ChefBot's **Settings → Calendar**.
2. In the calendar, choose **Home → Add Calendar → From Internet**.
3. Paste the address and choose **OK**, then **Yes** to subscribe.

Classic Outlook usually fetches the feed from your computer, so it works on a home network. With a Microsoft 365
or Outlook.com account, newer versions may hand the subscription to Microsoft's servers instead, and then it only
works when ChefBot is on the internet.

## Thunderbird

1. Copy the address from ChefBot's **Settings → Calendar**.
2. Open the calendar, then press **+** next to the list of calendars, or choose **File → New → Calendar**.
3. Choose **On the Network** and press **Next**.
4. Paste the address into **Location**. Leave the username empty, and tick **This location doesn't require
   credentials**.
5. Press **Find Calendars**, make sure the feed is ticked, then press **Subscribe**.

If ChefBot uses its own certificate, Thunderbird refuses it until you add an exception. Go to **Settings →
Privacy & Security → Certificates → Manage Certificates → Servers → Add Exception**, enter ChefBot's address,
press **Get Certificate**, then **Confirm Security Exception**.

## GNOME Calendar

1. Copy the address from ChefBot's **Settings → Calendar**.
2. In GNOME Calendar, open the menu and choose **Manage Calendars**, then **Add Calendar**.
3. Paste the address into **Calendar URL**, or **Import a Calendar** in older versions, and choose **Add**.

GNOME Calendar uses the computer's own list of trusted certificates. Install ChefBot's certificate into it first
if ChefBot uses its own.

## Changing the mealtimes

An admin changes the times under **Settings → Calendar → Meal times**. Everyone else can see them there.

| Setting | Default | Meaning |
|---|---|---|
| Breakfast, Lunch, Dinner | `08:00`, `12:30`, `18:30` | When each meal starts, as local time |
| Each meal lasts | 45 minutes | How long each meal's event lasts |
| Slow cooker reminder | 8 hours | How many hours before a slow cooker dinner the reminder goes off; `0` for none |

The `Calendar__*` settings in `chefbot.env` of earlier versions are no longer read.

Subscribed calendars show the new times the next time they check the feed.

## When the calendar doesn't update

- **Nothing shows at all:** check that the week is confirmed. Draft weeks aren't in the calendar. Then open the
  address in a browser on the same device. It should download a small `meals.ics` file. If it can't be reached,
  the calendar app can't reach it either.
- **"Not found" or an error 404:** the feed was turned off, or an admin made a new address. Copy the address
  again from **Settings → Calendar** and subscribe again.
- **A certificate or security error:** the device doesn't trust ChefBot's certificate yet. See
  [Before you start](#before-you-start).
- **Changes are slow to appear:** each app checks on its own schedule. Apple Calendar and ICSx⁵ let you change it.
  Google and Microsoft decide for themselves.
