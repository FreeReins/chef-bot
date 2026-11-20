<!-- Asks before something hard to undo. App.svelte shows this once; ask() in lib/app.svelte.js opens it and
     waits for the answer. Escape, the Cancel button and a click outside the box all cancel. -->
<script>
  import { app, answer } from '../lib/app.svelte.js';

  let dialog;
  const current = $derived(app.confirmation);

  $effect(() => {
    if (current && !dialog.open) dialog.showModal();
    else if (!current && dialog.open) dialog.close();
  });

  // Escape closes the dialog itself, so anything that closes it without an answer counts as cancelling.
  const closed = () => { if (app.confirmation) answer(false); };
  const clickedOutside = (event) => { if (event.target === dialog) answer(false); };
</script>

<dialog bind:this={dialog} aria-labelledby="confirm-title" aria-describedby="confirm-message"
        onclose={closed} onclick={clickedOutside}>
  {#if current}
    <div class="box">
      <h2 id="confirm-title">{current.title}</h2>
      {#if current.message}<p id="confirm-message">{current.message}</p>{/if}
      <div class="actions">
        <!-- Cancel takes focus first, so pressing Enter by habit changes nothing. -->
        <!-- svelte-ignore a11y_autofocus -->
        <button autofocus onclick={() => answer(false)}>Cancel</button>
        <button class={current.danger ? 'danger' : 'primary'} onclick={() => answer(true)}>{current.confirmLabel}</button>
      </div>
    </div>
  {/if}
</dialog>

<style>
  dialog { padding: 0; border: 1px solid var(--line); border-radius: .6rem; background: var(--panel); color: var(--ink);
    width: min(28rem, calc(100vw - 2rem)); box-shadow: 0 10px 40px rgb(0 0 0 / .3); }
  dialog::backdrop { background: rgb(0 0 0 / .45); }
  .box { padding: 1.1rem 1.25rem 1rem; }
  h2 { margin: 0 0 .4rem; }
  p { margin: 0; color: var(--muted); font-size: .95rem; }
  .actions { justify-content: flex-end; margin-top: 1.1rem; }
</style>
