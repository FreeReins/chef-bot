<!-- The shopping list: what the plan needs and what was added by hand, and where each item comes from. What is left
     to buy comes first, split by store, and anything without a store is grouped by food group. It works without a
     plan: then only the hand-added items are listed. Pantry items running out of date are offered for the list, and
     what the plan needs but needn't be bought (in the pantry, bought already, staples) is folded away at the end.
     A staple that has run out can be added to the list from there. The pantry itself is on the Pantry tab. -->
<script>
  import { untrack } from 'svelte';
  import { app, isConfirmed, run, loadPantry, addToShoppingList, buyEverything, toast } from '../lib/app.svelte.js';
  import { FOOD_GROUPS, shortDate, weekName, freshness, plural } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import IngredientLine from './IngredientLine.svelte';

  // With a plan the pantry comes with its ingredients view; without one it is loaded on its own, as on the Pantry tab.
  $effect(() => {
    if (!app.plan) untrack(() => run(loadPantry));
  });

  const lines = $derived(app.plan ? (app.ingredients?.ingredients ?? []) : []);
  const byStatus = (status) => lines.filter((line) => line.status === status);
  const planToBuy = $derived(byStatus('ToBuy'));
  const bought = $derived(byStatus('Purchased'));
  const handAdded = $derived(app.manualItems.map((item) => ({
    name: item.name, status: 'ToBuy', quantity: item.quantity, unit: item.unit, store: item.store || null,
    group: item.group, isOptional: false, usedIn: [], manualId: item.id,
  })));
  const toBuy = $derived([...planToBuy, ...handAdded]);
  // What "Mark all as bought" covers: everything once the plan is confirmed, otherwise only the hand-added items.
  const buyable = $derived(isConfirmed() ? toBuy.length : handAdded.length);

  // Items with a store go under it, in the order the stores were saved; the rest go under their food group.
  const sections = $derived.by(() => {
    const saved = (app.stores?.stores ?? []).map((store) => store.name);
    const named = [...new Set(toBuy.filter((line) => line.store).map((line) => line.store))]
      .sort((a, b) => (saved.indexOf(a) + 1 || Infinity) - (saved.indexOf(b) + 1 || Infinity));
    const loose = toBuy.filter((line) => !line.store);
    const byName = (a, b) => a.name.localeCompare(b.name);
    return [
      ...named.map((store) => ({ key: `store:${store}`, title: store, lines: toBuy.filter((line) => line.store === store).sort(byName) })),
      ...FOOD_GROUPS.map(([group, title]) => ({ key: `group:${group}`, title, lines: loose.filter((line) => (line.group ?? 'Other') === group).sort(byName) })),
    ].filter((section) => section.lines.length > 0);
  });

  // Names already on the list, lower-cased, so nothing is offered for it twice.
  const listed = $derived(new Set(toBuy.map((line) => line.name.toLowerCase())));

  // Pantry items that are out of date or soon will be, and aren't on the list already.
  const pantryItems = $derived(app.plan ? (app.ingredients?.pantry ?? []).map((line) => line.item) : app.pantry);
  const restock = $derived.by(() => {
    const seen = new Set();
    return pantryItems
      .filter((item) => freshness(item))
      .filter((item) => !listed.has(item.name.toLowerCase()) && !seen.has(item.name.toLowerCase()) && seen.add(item.name.toLowerCase()));
  });

  const notListed = $derived(app.plan
    ? [
      { title: 'From your pantry', note: 'Already in the kitchen, so not on the list.', lines: byStatus('InPantry') },
      { title: 'Already bought', note: 'Ticked off this week.', lines: bought },
      {
        title: 'Staples assumed on hand',
        note: "Things like salt and oil that are taken as kept in. Add any you've run out of to the list.",
        lines: byStatus('Staple').filter((line) => !listed.has(line.name.toLowerCase())),
        addable: true,
      },
    ].filter((group) => group.lines.length)
    : []);

  async function add(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const added = await addToShoppingList({
      name: data.get('name'),
      quantity: data.get('quantity') ? Number(data.get('quantity')) : null,
      unit: data.get('unit') || null,
    });
    if (added) {
      form.reset();
      form.elements.name.focus();
    }
  }

  // Puts a pantry item running out, or a staple that has run out, on the list by name.
  async function addByName(item) {
    if (await addToShoppingList({ name: item.name, unit: item.unit || null })) toast(`${item.name} was added to the list.`);
  }
</script>

<section>
  {#if app.plan && !isConfirmed()}
    <p class="card notice">
      A preview for the draft plan, {shortDate(app.plan.weekStart)} to {shortDate(app.plan.weekEnd)}.
      <a href="/plan">Confirm the plan</a> to tick its items off.
    </p>
  {:else if app.plan && app.plans.length > 1}
    <p class="muted">
      {weekName(app.plan.weekStart, app.plan.weekEnd)}: {shortDate(app.plan.weekStart)} to {shortDate(app.plan.weekEnd)}.
      Choose another week on the <a href="/plan">Plan tab</a>.
    </p>
  {/if}

  <div class="row head">
    <div>
      <h2>To buy ({toBuy.length})</h2>
      {#if bought.length}<p class="muted">{plural(bought.length, 'item')} bought so far</p>{/if}
    </div>
    {#if buyable}
      <button class="small" onclick={() => buyEverything(buyable)}>
        {isConfirmed() || planToBuy.length === 0 ? 'Mark all as bought' : 'Mark hand-added items as bought'}
      </button>
    {/if}
  </div>

  <form class="inline card add" onsubmit={add}>
    <label>Add to the list <input id="shopping-name" name="name" required placeholder="e.g. milk"></label>
    <label>Amount <input id="shopping-quantity" name="quantity" type="number" min="0" step="any"></label>
    <label>Unit <input id="shopping-unit" name="unit" size="6" placeholder="l"></label>
    <button type="submit">Add</button>
  </form>

  {#if toBuy.length === 0}
    <div class="card empty-list">
      <p class="muted">
        {app.plan
          ? 'Nothing left to buy for this plan. Add anything else you need above.'
          : 'Your shopping list is empty. Add an item above, or add pantry items that are running out below.'}
      </p>
    </div>
  {/if}
  {#each sections as section (section.key)}
    <div class="card">
      <h3>{section.title} <span class="count">{section.lines.length}</span></h3>
      <ul class="plain">
        {#each section.lines as line (line.manualId ?? line.shoppingItemId ?? line.name)}<IngredientLine {line} />{/each}
      </ul>
    </div>
  {/each}

  {#if restock.length}
    <h2>Running out in the pantry ({restock.length})</h2>
    <div class="card">
      <ul class="plain">
        {#each restock as item (item.id)}
          {@const warning = freshness(item)}
          <li class="row">
            <div>
              <strong>{item.name}</strong> <Chip tone={warning.tone}>{warning.text}</Chip>
            </div>
            <button class="small" aria-label="Add to list: {item.name}" onclick={() => addByName(item)}>Add to list</button>
          </li>
        {/each}
      </ul>
    </div>
  {/if}

  {#if notListed.length}
    <h2>Not on the list</h2>
    <div class="card folds">
      {#each notListed as group (group.title)}
        <details>
          <summary>{group.title} ({group.lines.length})</summary>
          <p class="muted">{group.note}</p>
          <ul class="plain">
            {#each group.lines as line, index (index)}
              {#if group.addable}
                <IngredientLine {line}>
                  {#snippet actions()}
                    <button class="small" aria-label="Add to list: {line.name}" onclick={() => addByName(line)}>Add to list</button>
                  {/snippet}
                </IngredientLine>
              {:else}
                <IngredientLine {line} />
              {/if}
            {/each}
          </ul>
        </details>
      {/each}
    </div>
  {/if}
</section>

<style>
  .notice { border-left: 4px solid var(--warn); font-size: .95rem; }
  .head { align-items: center; margin-top: 1rem; }
  .head h2 { margin: 0; }
  .add { padding: .6rem 1rem; }
  .empty-list { padding: 1.25rem 1rem; }
  h3 { margin-bottom: .25rem; }
  .count { color: var(--muted); font-weight: 400; font-size: .9rem; }
  .folds details { border-top: 1px solid var(--line); padding: .4rem 0; }
  .folds details:first-child { border-top: 0; }
  .folds summary { cursor: pointer; font-weight: 600; }
  .folds details > p { margin: .25rem 0; }
</style>
