<script>
  import { app, run, refreshAfterPantryChange } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { amount, shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';

  const TONES = { Expired: 'bad', ExpiringSoon: 'warn', Fresh: 'good', Unknown: '' };
  const LABELS = { Expired: 'Expired', ExpiringSoon: 'Use soon', Fresh: 'Fresh', Unknown: 'No date' };

  const pantry = $derived(app.ingredients?.pantry ?? []);

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

<section>
  <h2>Pantry ({pantry.length})</h2>
  <div class="card">
    {#if pantry.length === 0}
      <p class="muted">Empty. Items appear here when you mark shopping as bought.</p>
    {/if}
    <ul class="plain">
      {#each pantry as { item, usedByPlan } (item.id)}
        <li class="row">
          <div>
            <strong>{item.name}</strong> {amount(item.quantity, item.unit)}
            <div class="chips">
              <Chip tone={TONES[item.freshness]}>
                {item.expiresOn ? `${LABELS[item.freshness]}, ${shortDate(item.expiresOn)}` : LABELS[item.freshness]}
              </Chip>
              {#if !usedByPlan}<Chip>Not used by this plan</Chip>{/if}
            </div>
          </div>
          <button class="small" aria-label="Remove {item.name}" onclick={() => remove(item.id)}>Remove</button>
        </li>
      {/each}
    </ul>
  </div>
  <form class="inline card" onsubmit={add}>
    <label>Add an item <input id="pantry-name" name="name" required placeholder="e.g. cabbage"></label>
    <label>Amount <input id="pantry-quantity" name="quantity" type="number" min="0" step="any"></label>
    <label>Unit <input id="pantry-unit" name="unit" size="6" placeholder="head"></label>
    <label>Expires <input id="pantry-expires" name="expiresOn" type="date"></label>
    <button type="submit">Add</button>
  </form>
</section>
