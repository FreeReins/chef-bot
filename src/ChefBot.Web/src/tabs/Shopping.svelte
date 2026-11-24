<!-- The shopping list: what the plan needs and what was added by hand, and where each item comes from. What is left
     to buy comes first, split by store and, within each store, by area of the store (produce, bakery, canned and dry
     goods...) in the order most shoppers walk it. Anything without a store is grouped by area on its own. It works without a
     plan: then only the hand-added items are listed. Pantry items running out of date are offered for the list, and
     what the plan needs but needn't be bought (in the pantry, bought already, staples) is folded away at the end.
     A staple that has run out can be added to the list from there. The pantry itself is on the Pantry tab. -->
<script>
  import { untrack } from 'svelte';
  import { app, isConfirmed, run, loadPantry, addToShoppingList, buyEverything, toast } from '../lib/app.svelte.js';
  import { STORE_AREAS, shortDate, weekName, freshness, plural, todayIso } from '../lib/format.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import { celebrate } from '../lib/motion.js';
  import { canShare, share, download, shoppingText, shoppingCsv } from '../lib/exporting.js';
  import Chip from '../components/Chip.svelte';
  import IngredientLine from './IngredientLine.svelte';
  import Share2 from '@lucide/svelte/icons/share-2';
  import Copy from '@lucide/svelte/icons/copy';
  import Printer from '@lucide/svelte/icons/printer';
  import Download from '@lucide/svelte/icons/download';
  import ListTodo from '@lucide/svelte/icons/list-todo';
  import CheckCheck from '@lucide/svelte/icons/check-check';
  import { api } from '../lib/api.js';
  import ShoppingCart from '@lucide/svelte/icons/shopping-cart';
  import Plus from '@lucide/svelte/icons/plus';

  // With a plan the pantry comes with its ingredients view; without one it is loaded on its own, as on the Pantry tab.
  // Offline, the pantry saved last time is shown instead.
  $effect(() => {
    if (!app.plan && !app.offline) untrack(() => run(loadPantry));
  });

  const lines = $derived(app.plan ? (app.ingredients?.ingredients ?? []) : []);
  const byStatus = (status) => lines.filter((line) => line.status === status);
  const planToBuy = $derived(byStatus('ToBuy'));
  const bought = $derived(byStatus('Purchased'));
  const handAdded = $derived(app.manualItems.map((item) => ({
    name: item.name, status: 'ToBuy', quantity: item.quantity, unit: item.unit, store: item.store || null,
    group: item.group, area: item.area ?? 'Other', isOptional: false, usedIn: [], manualId: item.id, pending: item.pending,
  })));
  const toBuy = $derived([...planToBuy, ...handAdded]);
  // What "Mark all as bought" covers: everything once the plan is confirmed, otherwise only the hand-added items.
  const buyableIn = (list) => (isConfirmed() ? list.length : list.filter((line) => line.manualId).length);
  const buyable = $derived(buyableIn(toBuy));

  // Items with a store go under it, in the order the stores were saved, split by area of the store; the rest get a
  // card per area. Areas come in walking order (STORE_AREAS), and items are A to Z within an area.
  const sections = $derived.by(() => {
    const saved = (app.stores?.stores ?? []).map((store) => store.name);
    const named = [...new Set(toBuy.filter((line) => line.store).map((line) => line.store))]
      .sort((a, b) => (saved.indexOf(a) + 1 || Infinity) - (saved.indexOf(b) + 1 || Infinity));
    const byName = (a, b) => a.name.localeCompare(b.name);
    const byArea = (list) => STORE_AREAS
      .map(([area, title]) => ({ area, title, lines: list.filter((line) => (line.area ?? 'Other') === area).sort(byName) }))
      .filter((group) => group.lines.length > 0);
    const stores = named.map((store) => {
      const areas = byArea(toBuy.filter((line) => line.store === store));
      return { key: `store:${store}`, title: store, byStore: true, areas, lines: areas.flatMap((group) => group.lines) };
    });
    const loose = byArea(toBuy.filter((line) => !line.store))
      .map((group) => ({ key: `area:${group.area}`, title: group.title, byStore: false, areas: [group], lines: group.lines }));
    return [...stores, ...loose];
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

  // What is left to buy, to take out of ChefBot: shared or copied as a checklist, or saved as a spreadsheet.
  const listTitle = $derived(app.plan ? `Shopping list, week of ${shortDate(app.plan.weekStart)}` : 'Shopping list');

  function shareList() {
    run(async () => {
      if (await share(listTitle, shoppingText(listTitle, sections)) === 'copied') {
        toast('The shopping list was copied. Paste it into a note or a message.');
      }
    });
  }

  function downloadCsv() {
    run(async () => download(`shopping-list-${app.plan?.weekStart ?? todayIso()}.csv`, 'text/csv;charset=utf-8', shoppingCsv(sections)));
  }

  // Sending to a to-do app adds every item there again, so a second press waits for the first to finish.
  let sendingTo = $state(null);

  async function sendTo(target) {
    sendingTo = target.id;
    await run(async () => {
      const result = await api('POST', '/api/shopping/send', { targetId: target.id, planId: app.plan?.id ?? null });
      if (result.problem) {
        toast(`${result.sent} of ${plural(result.total, 'item')} went to ${target.name}. ${result.problem}`, 'error');
      } else {
        toast(`${plural(result.sent, 'item')} went to ${target.name}. Nothing is ticked off here until you buy it.`);
      }
    });
    sendingTo = null;
  }

  async function add(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const added = await pending(event, () => addToShoppingList({
      name: data.get('name'),
      quantity: data.get('quantity') ? Number(data.get('quantity')) : null,
      unit: data.get('unit') || null,
    }));
    if (added) {
      form.reset();
      form.elements.name.focus();
    }
  }

  // Puts a pantry item running out, or a staple that has run out, on the list by name.
  const addByName = (item) => pendingClick(async () => {
    if (await addToShoppingList({ name: item.name, unit: item.unit || null })) toast(`${item.name} was added to the list.`);
  });

  const buyAll = pendingClick(() => buyEverything(buyable));

  // A store's own button, for when one shop is done but another is still to come. Only when the list covers more than
  // that store, so it never does the same as "Mark all as bought".
  const storeBuyable = (section) => {
    const count = section.byStore ? buyableIn(section.lines) : 0;
    return count > 0 && count < buyable ? count : 0;
  };
  const buyStore = (section) => pendingClick(() => buyEverything(storeBuyable(section), section.title));

  // Buying the last thing on the list is worth a moment: the cart hops and a few sprinkles fly.
  let emptyIcon = $state();
  let cleared = $state(false);
  let lastCount = untrack(() => toBuy.length);
  $effect(() => {
    const count = toBuy.length;
    if (lastCount > 0 && count === 0 && emptyIcon) {
      cleared = true;
      celebrate(emptyIcon, { count: 16 });
    }
    lastCount = count;
  });
</script>

<section>
  <h2 class="print-only">{listTitle}</h2>
  {#if app.plan && !isConfirmed()}
    <p class="card notice no-print">
      A preview for the draft plan, {shortDate(app.plan.weekStart)} to {shortDate(app.plan.weekEnd)}.
      <a href="/plan">Confirm the plan</a> to tick its items off.
    </p>
  {:else if app.plan && app.plans.length > 1}
    <p class="muted no-print">
      {weekName(app.plan.weekStart, app.plan.weekEnd)}: {shortDate(app.plan.weekStart)} to {shortDate(app.plan.weekEnd)}.
      Choose another week on the <a href="/plan">Plan tab</a>.
    </p>
  {/if}

  <div class="row head">
    <div>
      <h2>To buy ({toBuy.length})</h2>
      {#if bought.length}<p class="muted">{plural(bought.length, 'item')} bought so far</p>{/if}
    </div>
  </div>

  {#if toBuy.length}
    <div class="row-actions export">
      {#if buyable}
        <button class="small with-icon" onclick={buyAll}>
          <CheckCheck size={14} />{isConfirmed() || planToBuy.length === 0 ? 'Mark all as bought' : 'Mark hand-added items as bought'}
        </button>
      {/if}
      {#if canShare()}
        <button class="small with-icon" onclick={shareList}><Share2 size={14} />Share list</button>
      {:else}
        <button class="small with-icon" onclick={shareList}><Copy size={14} />Copy list</button>
      {/if}
      <button class="small with-icon" onclick={() => window.print()}><Printer size={14} />Print</button>
      <button class="small with-icon" onclick={downloadCsv}><Download size={14} />Download CSV</button>
      {#each app.shoppingTargets as target (target.id)}
        <button class="small with-icon" onclick={() => sendTo(target)} disabled={sendingTo !== null || app.offline}
                title={app.offline ? 'Needs ChefBot: you are offline' : undefined}>
          <ListTodo size={14} />{sendingTo === target.id ? `Sending to ${target.name}…` : `Send to ${target.name}`}
        </button>
      {/each}
    </div>
  {/if}

  <form class="inline card add no-print" onsubmit={add}>
    <label>Add to the list <input id="shopping-name" name="name" required placeholder="e.g. milk"></label>
    <label>Amount <input id="shopping-quantity" name="quantity" type="number" min="0" step="any"></label>
    <label>Unit <input id="shopping-unit" name="unit" size="6" placeholder="l"></label>
    <button type="submit" class="with-icon"><Plus size={16} />Add</button>
  </form>

  {#if toBuy.length === 0}
    <div class="card empty-list">
      <span class="empty-icon" class:hop={cleared} style="--area: var(--area-shopping)" aria-hidden="true" bind:this={emptyIcon}><ShoppingCart size={24} /></span>
      <p class="muted">
        {#if app.plan && bought.length}<strong class="all-bought">Everything is bought. Time to cook.</strong> Add anything else you need above.
        {:else if app.plan}Nothing left to buy for this plan. Add anything else you need above.
        {:else}Your shopping list is empty. Add an item above, or add pantry items that are running out below.{/if}
      </p>
    </div>
  {/if}
  <div class="lists">
    {#each sections as section (section.key)}
      {@const storeCount = storeBuyable(section)}
      <div class="card">
        <div class="section-head">
          <h3>{section.title} <span class="count">{section.lines.length}</span></h3>
          {#if storeCount}
            <button class="small with-icon no-print" aria-label="Mark all as bought from {section.title}" onclick={buyStore(section)}>
              <CheckCheck size={14} />Mark all as bought
            </button>
          {/if}
        </div>
        {#each section.areas as group (group.area)}
          <div class="area">
            {#if section.byStore}<h4>{group.title} <span class="count">{group.lines.length}</span></h4>{/if}
            <ul class="plain">
              {#each group.lines as line (line.manualId ?? line.shoppingItemId ?? line.name)}<IngredientLine {line} />{/each}
            </ul>
          </div>
        {/each}
      </div>
    {/each}
  </div>

  {#if restock.length}
    <h2 class="no-print">Running out in the pantry ({restock.length})</h2>
    <div class="card no-print">
      <ul class="plain">
        {#each restock as item (item.id)}
          {@const warning = freshness(item)}
          <li class="row">
            <div>
              <strong>{item.name}</strong> <Chip tone={warning.tone}>{warning.text}</Chip>
            </div>
            <button class="small with-icon" aria-label="Add to list: {item.name}" onclick={addByName(item)}><Plus size={16} />Add to list</button>
          </li>
        {/each}
      </ul>
    </div>
  {/if}

  {#if notListed.length}
    <h2 class="no-print">Not on the list</h2>
    <div class="card folds no-print">
      {#each notListed as group (group.title)}
        <details>
          <summary>{group.title} ({group.lines.length})</summary>
          <p class="muted">{group.note}</p>
          <ul class="plain">
            {#each group.lines as line, index (index)}
              {#if group.addable}
                <IngredientLine {line}>
                  {#snippet actions()}
                    <button class="small with-icon" aria-label="Add to list: {line.name}" onclick={addByName(line)}><Plus size={16} />Add to list</button>
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
  .empty-list { display: flex; gap: .9rem; align-items: center; padding: 1rem; }
  .empty-list .empty-icon { flex: none; margin: 0; }
  .all-bought { color: var(--ink); }
  @media (prefers-reduced-motion: no-preference) { .empty-icon.hop :global(svg) { animation: hop .6s cubic-bezier(.3, 1.6, .5, 1); } }
  @keyframes hop { 30% { transform: translateY(-6px) rotate(-8deg); } 60% { transform: translateY(0) rotate(4deg); } }
  h3 { margin-bottom: .25rem; }
  .section-head { display: flex; flex-wrap: wrap; gap: .25rem .75rem; align-items: baseline; justify-content: space-between; }
  h4 { margin: .75rem 0 .1rem; color: var(--muted); font-size: .78rem; font-weight: 700; letter-spacing: .05em; text-transform: uppercase; }
  .area:first-of-type h4 { margin-top: .35rem; }
  .count { color: var(--muted); font-weight: 400; font-size: .9rem; }
  h4 .count { font-size: .78rem; letter-spacing: 0; }
  .folds details { border-top: 1px solid var(--line); padding: .4rem 0; }
  .folds details:first-child { border-top: 0; }
  .folds summary { cursor: pointer; font-weight: 600; }
  .folds details > p { margin: .25rem 0; }
  .export { margin-top: .5rem; }

  /* On paper: only what is left to buy, in two columns with each store kept together. */
  @media print {
    .head { display: none; }
    .lists { columns: 2; column-gap: 1.5rem; }
    .lists .card { margin: 0 0 .75rem; }
    .lists .area { break-inside: avoid; }
  }
</style>
