<!-- A dialog laid over the page for one job, such as adding or editing a recipe. It opens when it is shown and is
     removed by its parent: Escape, the close button and a click outside the box all call onclose, which usually moves
     to the address behind the dialog. `nav` is an optional row of tabs under the title; on phones the dialog fills
     the screen. -->
<script>
  import X from '@lucide/svelte/icons/x';

  /** title: the heading. wide: room for a form. onclose(): asked to close. children, nav: snippets. */
  let { title, wide = false, onclose, children, nav = null } = $props();

  const id = `modal-${Math.random().toString(36).slice(2, 9)}`;
  let dialog;

  // Opens as it appears, and puts focus back where it was (the button or link that opened it) as it goes.
  $effect(() => {
    const opener = document.activeElement;
    dialog.showModal();
    return () => {
      if (dialog.open) dialog.close();
      if (opener instanceof HTMLElement && opener.isConnected) opener.focus();
    };
  });

  // Escape would close the dialog by itself; the parent decides instead, so the address stays in step.
  function cancel(event) {
    event.preventDefault();
    onclose();
  }
  const clickedOutside = (event) => { if (event.target === dialog) onclose(); };
</script>

<dialog bind:this={dialog} class:wide aria-labelledby="{id}-title" oncancel={cancel} onclick={clickedOutside}>
  <div class="box">
    <header>
      <h2 id="{id}-title">{title}</h2>
      <button type="button" class="icon" aria-label="Close" onclick={onclose}><X size={20} /></button>
    </header>
    {#if nav}<div class="nav">{@render nav()}</div>{/if}
    <div class="body">{@render children()}</div>
  </div>
</dialog>

<style>
  dialog { padding: 0; border: 1px solid var(--line); border-radius: .6rem; background: var(--bg); color: var(--ink);
    width: min(30rem, calc(100vw - 2rem)); max-height: calc(100dvh - 2rem); box-shadow: 0 10px 40px rgb(0 0 0 / .3); }
  dialog.wide { width: min(46rem, calc(100vw - 2rem)); }
  dialog::backdrop { background: rgb(0 0 0 / .45); }
  .box { display: flex; flex-direction: column; max-height: calc(100dvh - 2rem); }
  header { display: flex; align-items: center; justify-content: space-between; gap: 1rem; padding: .9rem 1.1rem .5rem; }
  h2 { margin: 0; font-size: 1.2rem; min-width: 0; overflow-wrap: anywhere; }
  .nav { padding: 0 1.1rem .5rem; border-bottom: 1px solid var(--line); }
  .body { overflow-y: auto; padding: .75rem 1.1rem 1.1rem; }

  /* Phones: a sheet that fills the screen. */
  @media (max-width: 40rem) {
    dialog, dialog.wide { width: 100vw; max-width: 100vw; height: 100dvh; max-height: 100dvh; margin: 0; border: 0; border-radius: 0; }
    .box { height: 100%; max-height: none; }
    .body { flex: 1; padding-bottom: calc(1.1rem + env(safe-area-inset-bottom)); }
  }
</style>
