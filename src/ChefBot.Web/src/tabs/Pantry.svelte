<!-- What is in the kitchen, and which meals on the plan are going to use each item. Items no meal uses are listed
     apart, since swapping a meal is how they get used up. Works without a plan too: then nothing is "used".
     Each list is grouped by food group, in shop order. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, loadPantry, refreshAfterPantryChange, addToShoppingList, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { amount, shortDate, parseDate, DAYS, FOOD_GROUPS } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import ListPlus from '@lucide/svelte/icons/list-plus';

  const TONES = { Expired: 'bad', ExpiringSoon: 'warn', Fresh: 'good', Unknown: '' };
  const LABELS = { Expired: 'Expired', ExpiringSoon: 'Use soon', Fresh: 'Fresh', Unknown: 'No date' };

  // With a plan, the pantry comes with its ingredients view; without one, it is loaded on its own. Only the plan
  // is tracked, so a toast shown by run() doesn't load it again.
  $effect(() => {
    if (!app.plan) untrack(() => run(loadPantry));
  });

  const pantry = $derived(app.plan
    ? (app.ingredients?.pantry ?? [])
    : app.pantry.map((item) => ({ item, usedByPlan: false, usedFor: [] })));
  const used = $derived(pantry.filter((line) => line.usedByPlan));
  const unused = $derived(pantry.filter((line) => !line.usedByPlan));

  /** Splits lines by food group, in shop order, sorted by name; items the catalog doesn't know go under "Other". */
  const byGroup = (lines) => FOOD_GROUPS
    .map(([group, title]) => ({
      group,
      title,
      lines: lines
        .filter((line) => (line.item.group ?? 'Other') === group)
        .sort((a, b) => a.item.name.localeCompare(b.item.name)),
    }))
    .filter((section) => section.lines.length > 0);

  /** "Tue dinner: Slaw Fish Tacos", or "Snack: Granola" for meals with no set day. */
  function mealLabel({ title, date, slot }) {
    const when = date ? `${DAYS[parseDate(date).getDay()].slice(0, 3)} ${slot.toLowerCase()}` : slot;
    return `${when}: ${title}`;
  }

  /** Puts a pantry item on the shopping list, to buy more of it. */
  async function restock(item) {
    if (await addToShoppingList({ name: item.name, unit: item.unit || null })) {
      toast(`${item.name} was added to the shopping list.`);
    }
  }

  const remove = (id) => run(async () => {
    await api('DELETE', `/api/pantry/${id}`);
    await refreshAfterPantryChange();
  });

  function add(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    run(async () => {
      await api('POST', '/api/pantry', {
        name: data.get('name'),
        quantity: data.get('quantity') ? Number(data.get('quantity')) : null,
        unit: data.get('unit') || null,
        expiresOn: data.get('expiresOn') || null,
      });
      form.reset();
      await refreshAfterPantryChange();
    });
  }
</script>

{#snippet items(lines)}
  <ul class="plain">
    {#each lines as { item, usedFor } (item.id)}
      <li class="row">
        <div>
          <strong>{item.name}</strong> {amount(item.quantity, item.unit)}
          <div class="chips">
            <Chip tone={TONES[item.freshness]}>
              {item.expiresOn ? `${LABELS[item.freshness]}, ${shortDate(item.expiresOn)}` : LABELS[item.freshness]}
            </Chip>
            {#each usedFor as meal (meal.mealId)}<Chip>{mealLabel(meal)}</Chip>{/each}
          </div>
        </div>
        <div class="row-actions">
          <button class="icon" aria-label="Add {item.name} to the shopping list" title="Add to the shopping list"
                  onclick={() => restock(item)}><ListPlus size={16} /></button>
          <button class="icon remove" aria-label="Remove {item.name}" title="Remove" onclick={() => remove(item.id)}><Trash2 size={16} /></button>
        </div>
      </li>
    {/each}
  </ul>
{/snippet}

{#snippet grouped(lines)}
  <div class="card">
    {#each byGroup(lines) as section (section.group)}
      <h3>{section.title} ({section.lines.length})</h3>
      {@render items(section.lines)}
    {/each}
  </div>
{/snippet}

<section>
  <form class="inline card" onsubmit={add}>
    <label>Add an item <input id="pantry-name" name="name" required placeholder="e.g. cabbage"></label>
    <label>Amount <input id="pantry-quantity" name="quantity" type="number" min="0" step="any"></label>
    <label>Unit <input id="pantry-unit" name="unit" size="6" placeholder="head"></label>
    <label>Expires <input id="pantry-expires" name="expiresOn" type="date"></label>
    <button type="submit">Add</button>
  </form>

  {#if pantry.length === 0}
    <div class="card">
      <p class="muted">The pantry is empty. Items appear here when you mark shopping as bought, or add them above.</p>
    </div>
  {:else if !app.plan}
    <h2>In the pantry ({pantry.length})</h2>
    {@render grouped(pantry)}
  {:else}
    {#if used.length}
      <h2>Used this week ({used.length})</h2>
      {@render grouped(used)}
    {/if}
    {#if unused.length}
      <h2>Not used this week ({unused.length})</h2>
      <p class="muted">No meal on the plan uses these. Swap a meal on the Meals tab to use them up.</p>
      {@render grouped(unused)}
    {/if}
  {/if}
</section>

<style>
  h3 {
    margin: 0.75rem 0 0.25rem;
  }

  h3:first-child {
    margin-top: 0;
  }
</style>
