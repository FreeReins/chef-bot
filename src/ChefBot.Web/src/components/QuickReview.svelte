<!-- "How was it?": a review in one tap, for straight after cooking. Pressing a star saves it at once, with no form to
     fill in; a note can be added afterwards, and pressing another star changes the saved review. Nothing has to be
     answered: with `ondismiss` there is a button to close it. -->
<script>
  import { tick } from 'svelte';
  import { app, saveReview } from '../lib/app.svelte.js';
  import { pending } from '../lib/pending.js';
  import StarPicker from './StarPicker.svelte';
  import X from '@lucide/svelte/icons/x';

  /**
   * recipe: { id, name }. onsaved(answer): told the recipe's reviews and rating after each save. ondismiss(): asked to
   * close it. compact: smaller, for a meal card.
   */
  let { recipe, onsaved = null, ondismiss = null, compact = false } = $props();

  let saved = $state(null); // RecipeReviewResponse: the review saved here
  let saving = $state(false);
  let noting = $state(false);
  let notes = $state('');
  let notesBox = $state();

  /** Saves the stars, and the notes when given; otherwise the notes saved before stay. Returns whether it worked. */
  async function save(stars, newNotes = saved?.notes ?? null) {
    saving = true;
    const answer = await saveReview(recipe.id, { stars, notes: newNotes }, saved?.id);
    saving = false;
    if (!answer) return false;
    const mine = app.session?.user?.id;
    saved = answer.reviews.find((review) => (saved ? review.id === saved.id : review.userId === mine)) ?? saved;
    onsaved?.(answer);
    return true;
  }

  async function startNote() {
    notes = saved.notes ?? '';
    noting = true;
    await tick();
    notesBox?.focus();
  }

  function saveNote(event) {
    event.preventDefault();
    return pending(event, async () => { if (await save(saved.stars, notes.trim() || null)) noting = false; });
  }
</script>

<div class="quick" class:compact>
  <p class="ask" aria-live="polite">
    {#if saved}Saved, {saved.stars} star{saved.stars === 1 ? '' : 's'}.{#if saved.notes} Note kept.{/if}{:else}How was it?{/if}
  </p>
  <div class="line">
    <StarPicker value={saved?.stars ?? null} label={recipe.name} size={compact ? 20 : 30} disabled={saving} onpick={(stars) => save(stars)} />
    {#if saved && !noting}
      <button type="button" class="small" onclick={startNote}>{saved.notes ? 'Change the note' : 'Add a note'}</button>
    {/if}
    {#if ondismiss && !noting}
      <button type="button" class="icon close" aria-label={saved ? 'Close' : 'Not now'} title={saved ? 'Close' : 'Not now'} onclick={ondismiss}>
        <X size={16} />
      </button>
    {/if}
  </div>
  {#if noting}
    <form class="note" onsubmit={saveNote}>
      <textarea bind:this={notesBox} bind:value={notes} aria-label="Notes on {recipe.name}" maxlength="2000" rows={compact ? 2 : 3}
                placeholder="Less salt next time, great with rice…"></textarea>
      <div class="row-actions">
        <button type="submit" class="small primary">Save note</button>
        <button type="button" class="small" onclick={() => (noting = false)}>Cancel</button>
      </div>
    </form>
  {/if}
</div>

<style>
  /* Lined up as its parent lines things up: centred on cooking mode's last page, from the start elsewhere. */
  .quick { display: grid; justify-items: inherit; gap: .25rem; }
  .ask { margin: 0; font-weight: 600; color: var(--ink); }
  .compact .ask { font-size: .85rem; font-weight: 500; }
  .line { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem .5rem; }
  .close { margin-left: auto; color: var(--muted); }
  .note { display: grid; gap: .4rem; width: min(30rem, 100%); }
  .note textarea { width: 100%; resize: vertical; font: inherit; }
</style>
