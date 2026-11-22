# Customising the catalogs

ChefBot ships with three files that it reads at startup. Edit your own copies to suit where you live and shop, then
mount them over the built-in ones.

| File | Holds |
|---|---|
| `config/ingredients.json` | Every ingredient ChefBot knows: its shared name, other names, food group, shelf life and season |
| `config/stores.json` | The stores offered as starting points under **Settings → Stores** |
| `config/plan-styles.json` | The starter plan styles. The first is the default for a new household |

## The ingredient catalog

This is what makes planning work: shelf lives decide expiry dates, seasons decide what's in season, and groups decide
how the shopping list is grouped and which simple plates ChefBot can invent.

```json
{"name": "cabbage", "group": "Vegetable", "shelfLifeDays": 14, "seasonMonths": [6, 7, 9, 10, 11, 12, 1, 2]}
```

The seasons shipped are a starting point for the Kansas City area, and the shelf lives are rough estimates. Edit both
to match what you see. The file is large, so search it rather than reading it from the top.

## Known stores

```json
{"name": "Costco", "kind": "WarehouseClub", "description": "Bulk meat, eggs and dairy", "items": ["chicken", "egg"]}
```

The file holds national and regional supermarkets, warehouse clubs, specialty markets with the ingredients they're
good for, and a farmers market open May to October. Adding one in ChefBot copies it, so a household can change its
items or months afterwards.

## Starter plan styles

The same shape as a style in the web page: name, description, cooking days, leftover lunches, counts and rules. See
[Plan styles](../using/plan-styles.md). Adding one in ChefBot copies it.

## Use your own copy

1. Get the file from the repository (for example
   [config/ingredients.json](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/config/ingredients.json))
   and save it as, say, `~/.config/chefbot/ingredients.json`.
2. Edit it. Keep it valid JSON: ChefBot reads the files at startup, and a mistake such as a missing comma is reported
   in its log, with where it is.
3. Mount it over the built-in one.

   **Podman:** add a line to `~/.config/containers/systemd/chefbot/chefbot.container`, and to
   `deploy/podman/chefbot.container` so `install.sh` keeps it. The file already has these lines commented out:

   ```ini
   Volume=%h/.config/chefbot/ingredients.json:/app/config/ingredients.json:ro,Z
   Volume=%h/.config/chefbot/stores.json:/app/config/stores.json:ro,Z
   Volume=%h/.config/chefbot/plan-styles.json:/app/config/plan-styles.json:ro,Z
   ```

   Then run `systemctl --user daemon-reload && systemctl --user restart chefbot`.

   **Docker Compose:**

   ```yaml
       volumes:
         - ./ingredients.json:/app/config/ingredients.json:ro
   ```

   Then run `docker compose up -d`.

Your copy doesn't change when ChefBot is updated. Compare it with the new version now and then to pick up new
ingredients.

Improved the seasons or shelf lives for your area? A pull request is welcome.
