<!-- Choosing a replacement for one meal, in a dialog over the week board: recipes ranked by how much of the pantry
     they use, then made-up plates. A meal's Swap button opens it (openSwap in lib/app.svelte.js); choosing one, Close,
     Escape or a click outside the box closes it, and focus goes back to that Swap button. -->
<script>
  import { app, closeSwap } from '../lib/app.svelte.js';
  import SwapPanel from './SwapPanel.svelte';
  import X from '@lucide/svelte/icons/x';

  let dialog;
  const meal = $derived(app.swap ? app.plan?.meals.find((other) => other.id === app.swap.mealId) ?? app.swap.options.meal : null);

  $effect(() => {
    if (meal && !dialog.open) dialog.showModal();
    else if (!meal && dialog.open) dialog.close();
  });

  // Escape closes the dialog itself, so anything that closes it counts as closing the swap.
  const closed = () => { if (app.swap) closeSwap(); };
  const clickedOutside = (event) => { if (event.target === dialog) closeSwap(); };
</script>

<dialog bind:this={dialog} aria-labelledby="swap-title" onclose={closed} onclick={clickedOutside}>
  {#if meal}
    <div class="box">
      <div class="head">
        <div>
          <h2 id="swap-title">Swap {meal.title}</h2>
          <p class="muted">{meal.day ? `${meal.day} ${meal.slot.toLowerCase()}` : `${meal.slot}, any day`}. Choose what to have instead.</p>
        </div>
        <button class="icon" aria-label="Close" title="Close" onclick={closeSwap}><X size={18} /></button>
      </div>
      <SwapPanel {meal} options={app.swap.options} />
    </div>
  {/if}
</dialog>

<style>
  dialog { padding: 0; border: 1px solid var(--line); border-radius: .6rem; background: var(--panel); color: var(--ink);
    width: min(36rem, calc(100vw - 2rem)); max-height: calc(100dvh - 2rem); box-shadow: 0 10px 40px rgb(0 0 0 / .3); }
  dialog::backdrop { background: rgb(0 0 0 / .45); }
  .box { padding: 1.1rem 1.25rem 1rem; }
  .head { display: flex; gap: 1rem; align-items: flex-start; justify-content: space-between; margin-bottom: .75rem; }
  h2 { margin: 0 0 .2rem; }
</style>
