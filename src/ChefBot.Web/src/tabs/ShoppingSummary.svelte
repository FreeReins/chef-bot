<!-- What the plan's shopping list comes to, in a line that opens into the list grouped by store. The full list,
     where items are ticked off, is on the Shopping tab. -->
<script>
  import { app } from '../lib/app.svelte.js';
  import { amount, plural } from '../lib/format.js';

  const lines = $derived(app.ingredients?.ingredients ?? []);
  const toBuy = $derived(lines.filter((line) => line.status === 'ToBuy'));
  const fromPantry = $derived(lines.filter((line) => line.status === 'InPantry').length);
  const groups = $derived.by(() => {
    const names = [...new Set(toBuy.map((line) => line.store || ''))].sort((a, b) => (a === '') - (b === '') || a.localeCompare(b));
    return names.map((store) => ({
      store: store || 'Anywhere',
      lines: toBuy.filter((line) => (line.store || '') === store).sort((a, b) => a.name.localeCompare(b.name)),
    }));
  });
  const storeCount = $derived(groups.filter((group) => group.store !== 'Anywhere').length);

  const headline = $derived([
    toBuy.length ? `${plural(toBuy.length, 'item')} to buy${storeCount > 1 ? ` at ${storeCount} stores` : ''}` : 'Nothing to buy',
    fromPantry ? `${fromPantry} from your pantry` : null,
  ].filter(Boolean).join(', '));
</script>

{#if toBuy.length}
  <details class="summary">
    <summary>{headline}</summary>
    <div class="groups">
      {#each groups as group (group.store)}
        <div>
          <p class="store">{group.store}</p>
          <ul class="plain">
            {#each group.lines as line (line.shoppingItemId ?? line.name)}
              <li>{line.name}{amount(line.quantity, line.unit, line.moreAmounts) ? `, ${amount(line.quantity, line.unit, line.moreAmounts)}` : ''}{line.isOptional ? ' (optional)' : ''}</li>
            {/each}
          </ul>
        </div>
      {/each}
    </div>
    <a href="/shopping">Open the shopping list</a>
  </details>
{:else}
  <p class="muted">{headline}. <a href="/shopping">Open the shopping list</a></p>
{/if}

<style>
  .summary summary { cursor: pointer; color: var(--ink); }
  .groups { display: grid; gap: .5rem 1.5rem; grid-template-columns: repeat(auto-fill, minmax(min(13rem, 100%), 1fr)); margin: .5rem 0; }
  .store { margin: 0; font-weight: 600; font-size: .9rem; }
  .groups li { font-size: .9rem; padding: .15rem 0; }
</style>
