# First run

The first time you open ChefBot, a short setup wizard creates your account and asks how you like to plan and shop.
It takes about five minutes. You can change every answer later.

## The setup wizard

1. **Create your account.** Enter your name, a username and a password of at least 8 characters. This first
   account is the **admin**: it can invite people and change settings for the whole household.
2. **Secure connection.** Shows the HTTPS address and lets you:
   - enter the names and addresses people use to reach ChefBot, such as `chefbot.local` or `192.168.1.20`, so the
     certificate matches them;
   - download the certificate, to install on your devices so the browser stops warning;
   - choose **Always use HTTPS**, which is on to start with.

   [HTTPS and certificates](../admin/https-and-certificates.md) explains each of these.
3. **Add your recipes.** Pick one or both:
   - **Import my recipes** from recipe files: a Mealie backup `.zip` with its photos, the `database.json` from one, a
     Mealie recipe export, schema.org recipes saved from a website, or JSON in ChefBot's own format. Add as many files
     as you like.
   - **Start with sample recipes:** tick the ones you want from 28 samples. You can come back to this list later from
     the Recipes tab.

   Or press **Skip for now** and add recipes later.
4. **How do you like to plan a week?** Say how many people you usually cook for, and pick the plan style you'll use
   for most weeks. The other starter styles are kept, so you can choose one for any week. See
   [Plan styles](../using/plan-styles.md).
5. **Where do you shop?** (optional) Choose your stores, and the shopping list is split between them. Skip it and
   the list is grouped by food group instead.

## Plan your first week

Open the **Plan** tab and press **Plan**. ChefBot makes a draft for the coming week. Review it, then confirm it.
[The weekly routine](../using/weekly-routine.md) explains each step.

> [!TIP]
> ChefBot plans better with more recipes to choose from. Ten or more dinners is a good start.

## Bring your recipes from Mealie

1. In Mealie, open **Settings → Backups** and make a backup.
2. Download it.
3. In ChefBot, open the **Recipes** tab and import the `.zip`.

Photos come along. Importing a newer backup later adds only the recipes ChefBot doesn't have yet.

## Invite your household

Open the **People** tab and press **Create invite link**. See [People and accounts](../admin/people-and-accounts.md).
Inviting anyone is optional: ChefBot works the same for one person as for a family.

## Next steps

- [Set up phones](../using/offline-on-your-phone.md) so the shopping list works in the shop
- [Get reminders](../admin/notifications.md) when a plan is ready or food is about to expire
- [Check your backups](../admin/backups-and-restore.md): ChefBot makes one every night
