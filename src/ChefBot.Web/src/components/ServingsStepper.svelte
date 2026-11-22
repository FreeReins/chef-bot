<!-- How many servings to make: − and + change it one serving at a time, the number can be typed, and ½×, 1×, 2× and 3×
     jump straight to a multiple of the recipe as written. Used on a recipe's page and when adding it to the plan. -->
<script>
  import Minus from '@lucide/svelte/icons/minus';
  import Plus from '@lucide/svelte/icons/plus';

  /** servings: what the recipe makes as written. serves: the count shown. onchange(count): a new count was chosen. */
  let { servings, serves, onchange, multiples = true, label = 'Servings' } = $props();

  /** The most the server lets a meal be planned for (ValidationEngine.MaxServings). */
  const MAX_SERVINGS = 50;
  const MULTIPLES = [[0.5, '½×', 'Half'], [1, '1×', 'As written'], [2, '2×', 'Double'], [3, '3×', 'Triple']]; // [factor, shown, title]
  const id = `servings-${Math.random().toString(36).slice(2, 9)}`;

  const clamp = (value) => Math.min(MAX_SERVINGS, Math.max(1, Math.round(value)));
  const multiple = (factor) => (factor === 1 ? servings : clamp(servings * factor));
  const set = (value) => { if (Number.isFinite(value) && value > 0) onchange(value === servings ? servings : clamp(value)); };
</script>

<div class="servings">
  <div class="stepper" role="group" aria-labelledby="{id}-label">
    <button type="button" class="step" aria-label="One serving fewer" disabled={serves <= 1} onclick={() => set(Math.ceil(serves) - 1)}>
      <Minus size={18} />
    </button>
    <input id={id} type="number" inputmode="numeric" min="1" max={MAX_SERVINGS} step="1" value={serves}
           onchange={(event) => set(Number(event.currentTarget.value))}>
    <label id="{id}-label" for={id}>{label}</label>
    <button type="button" class="step" aria-label="One serving more" disabled={serves >= MAX_SERVINGS} onclick={() => set(Math.floor(serves) + 1)}>
      <Plus size={18} />
    </button>
  </div>
  {#if multiples}
    <div class="segmented multiples" role="group" aria-label="Scale the recipe">
      {#each MULTIPLES as [factor, text, name] (factor)}
        <button type="button" aria-pressed={serves === multiple(factor)} title="{name}: {multiple(factor)} servings"
                onclick={() => set(multiple(factor))}>{text}<span class="sr-only">, {multiple(factor)} servings</span></button>
      {/each}
    </div>
  {/if}
</div>

<style>
  .servings { display: flex; flex-wrap: wrap; align-items: center; gap: .5rem .75rem; }
  .stepper { display: inline-flex; align-items: center; gap: .15rem; padding: .2rem; border: 1px solid var(--line); border-radius: 999px;
    background: var(--bg); }
  .step { display: inline-grid; place-items: center; width: 2.1rem; height: 2.1rem; padding: 0; border: 0; border-radius: 50%;
    background: var(--panel); color: var(--area-recipes); box-shadow: 0 1px 2px var(--shadow); }
  .stepper input { width: 3rem; padding: .2rem 0; border: 0; background: none; color: var(--ink); text-align: center;
    font: inherit; font-weight: 700; font-size: 1.1rem; font-variant-numeric: tabular-nums; -moz-appearance: textfield; appearance: textfield; }
  .stepper input::-webkit-inner-spin-button, .stepper input::-webkit-outer-spin-button { -webkit-appearance: none; margin: 0; }
  .stepper label { color: var(--muted); font-size: .85rem; padding-right: .35rem; display: inline; }
  .multiples button { font-variant-numeric: tabular-nums; min-width: 2.6rem; }
  @media (pointer: coarse) {
    .step { width: 2.6rem; height: 2.6rem; }
  }
</style>
