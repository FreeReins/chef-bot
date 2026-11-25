<!-- The reviews on a recipe's page: a one-tap review to leave a new one (QuickReview.svelte), then everyone's, newest
     first. People change and delete their own; an admin can delete anyone's. The rating they add up to goes back to
     the page through `onrating`. -->
<script>
  import { untrack } from 'svelte';
  import { app, isAdmin, ask, toast, loadReviews, saveReview, removeReview } from '../lib/app.svelte.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import QuickReview from '../components/QuickReview.svelte';
  import StarPicker from '../components/StarPicker.svelte';
  import Stars from '../components/Stars.svelte';
  import Pencil from '@lucide/svelte/icons/pencil';
  import Trash2 from '@lucide/svelte/icons/trash-2';

  /** recipe: { id, name }. onrating(rating): told the recipe's new rating, or null once it has no reviews. */
  let { recipe, onrating } = $props();

  let reviews = $state([]);
  let loaded = $state(false);
  let editing = $state(null); // the id of the review being changed
  let draft = $state({ stars: null, notes: '' });

  const me = $derived(app.session?.user?.id);
  const day = (at) => new Date(at).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });

  /** Shows the reviews and rating the server sent back. The recipe page calls it for a review saved in cooking mode. */
  export function show(answer) {
    if (!answer) return;
    reviews = answer.reviews;
    onrating?.(answer.rating);
  }

  // Loads the reviews for the recipe shown. run() changes the page's state as it goes, so that is left untracked.
  $effect(() => {
    const id = recipe.id;
    untrack(() => loadReviews(id).then((answer) => { show(answer); loaded = true; }));
  });

  function edit(review) {
    draft = { stars: review.stars, notes: review.notes ?? '' };
    editing = review.id;
  }

  function update(event, review) {
    event.preventDefault();
    return pending(event, async () => {
      const answer = await saveReview(recipe.id, { stars: draft.stars, notes: draft.notes.trim() || null }, review.id);
      if (!answer) return;
      show(answer);
      editing = null;
      toast('Review saved.');
    });
  }

  const remove = (review) => pendingClick(async () => {
    const whose = review.userId === me ? 'your' : `${review.reviewedBy}'s`;
    if (!(await ask({ title: `Delete ${whose} review?`, message: `It comes off ${recipe.name} and its rating for good.`,
      confirmLabel: 'Delete', danger: true }))) return;
    const answer = await removeReview(recipe.id, review.id);
    if (answer) { show(answer); toast('Review deleted.'); }
  });
</script>

<section class="card" id="recipe-reviews" tabindex="-1" aria-labelledby="recipe-reviews-title">
  <h3 id="recipe-reviews-title">Reviews</h3>
  <QuickReview {recipe} onsaved={show} />
  {#if reviews.length}
    <ul class="plain reviews">
      {#each reviews as review (review.id)}
        <li>
          {#if editing === review.id}
            <form class="edit" onsubmit={(event) => update(event, review)}>
              <StarPicker value={draft.stars} label="your review" size={24} onpick={(stars) => (draft.stars = stars)} />
              <textarea bind:value={draft.notes} aria-label="Notes" maxlength="2000" rows="3"></textarea>
              <div class="row-actions">
                <button type="submit" class="small primary">Save review</button>
                <button type="button" class="small" onclick={() => (editing = null)}>Cancel</button>
              </div>
            </form>
          {:else}
            <p class="who">
              <strong>{review.reviewedBy}</strong><Stars stars={review.stars} />
              <span class="muted"><time datetime={review.at}>{day(review.at)}</time>{review.edited ? ', edited' : ''}</span>
            </p>
            {#if review.notes}<p class="notes">{review.notes}</p>{/if}
            {#if review.userId === me || isAdmin()}
              <div class="row-actions">
                {#if review.userId === me}
                  <button type="button" class="small with-icon" aria-label="Change your review from {day(review.at)}"
                          onclick={() => edit(review)}><Pencil size={14} />Change</button>
                {/if}
                <button type="button" class="small with-icon" aria-label="Delete {review.userId === me ? 'your' : `${review.reviewedBy}'s`} review from {day(review.at)}"
                        onclick={remove(review)}><Trash2 size={14} />Delete</button>
              </div>
            {/if}
          {/if}
        </li>
      {/each}
    </ul>
  {:else if loaded}
    <p class="muted">No reviews yet. Rate it each time it's cooked, and the average shows beside its name.</p>
  {/if}
</section>

<style>
  /* Placed and focused like the page's other parts, which the jump links scroll to. */
  section { margin: 1.25rem 0 0; }
  section:focus { outline: none; }
  section:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
  @media (max-width: 48rem) { section { scroll-margin-top: 3.5rem; } }
  @media print { section { display: none; } }
  .reviews { margin: 1rem 0 0; }
  .reviews li { padding: .6rem 0; border-top: 1px solid var(--line); }
  .who { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem .6rem; margin: 0; }
  .notes { margin: .3rem 0 0; white-space: pre-line; }
  .row-actions { margin-top: .4rem; }
  .edit { display: grid; gap: .4rem; }
  .edit textarea { width: 100%; resize: vertical; font: inherit; }
</style>
