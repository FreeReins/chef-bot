<!-- Choosing tags to filter recipes by, made for libraries with hundreds of tags: the chosen ones show as chips that
     can be removed, and typing narrows the suggestions to the tags that contain what was typed. With nothing typed it
     suggests the most used. Enter adds the first suggestion, or what was typed when nothing matches. -->
<script>
  import X from '@lucide/svelte/icons/x';

  /** tags: RecipeTagResponse[] ({ name, recipes }), most used first; chosen: the tag names chosen, bindable. */
  let { tags = [], chosen = $bindable([]), onchange, id = 'tag-filter' } = $props();

  /** How many suggestions show at once. More are reached by typing. */
  const SHOWN = 12;

  let typed = $state('');
  let input = $state();

  const isChosen = (name) => chosen.some((tag) => tag.toLowerCase() === name.toLowerCase());
  const word = $derived(typed.trim().replace(/^#/, '').toLowerCase());
  // Tags that start with what was typed come before those that only contain it; each keeps the most-used order.
  const matching = $derived(tags
    .filter((tag) => !isChosen(tag.name) && tag.name.toLowerCase().includes(word))
    .sort((a, b) => Number(!a.name.toLowerCase().startsWith(word)) - Number(!b.name.toLowerCase().startsWith(word))));
  const suggestions = $derived(matching.slice(0, SHOWN));
  const hidden = $derived(matching.length - suggestions.length);

  function add(name) {
    const tag = name.trim().replace(/^#/, '');
    if (!tag || isChosen(tag)) return;
    chosen = [...chosen, tags.find((known) => known.name.toLowerCase() === tag.toLowerCase())?.name ?? tag];
    typed = '';
    onchange?.();
  }

  function remove(name) {
    chosen = chosen.filter((tag) => tag !== name);
    onchange?.();
    input?.focus();
  }

  function onkeydown(event) {
    if (event.key === 'Enter') {
      event.preventDefault();
      add(suggestions[0]?.name ?? typed);
    } else if (event.key === 'Backspace' && typed === '' && chosen.length) {
      remove(chosen[chosen.length - 1]);
    }
  }
</script>

<div class="tag-filter">
  <div class="head"><label for={id}>Tags</label><span class="muted">A recipe must have all of them</span></div>
  <div class="field">
    {#each chosen as tag (tag)}
      <span class="chosen">#{tag}<button type="button" class="icon" aria-label="Remove tag {tag}" onclick={() => remove(tag)}><X size={14} /></button></span>
    {/each}
    <input {id} type="text" autocomplete="off" bind:this={input} bind:value={typed} {onkeydown}
           placeholder={chosen.length ? 'Add another tag' : 'Type to find a tag'} aria-describedby="{id}-hint">
  </div>
  <p id="{id}-hint" class="sr-only">Press Enter to add the first suggestion. Backspace in an empty box removes the last tag.</p>

  {#if tags.length === 0}
    <p class="muted note">No recipes have tags yet. Add some when editing a recipe to filter by them here.</p>
  {:else if suggestions.length}
    <div class="suggestions" role="group" aria-label={word ? `Tags containing ${word}` : 'Most used tags'}>
      <span class="muted">{word ? 'Matching' : 'Most used'}</span>
      {#each suggestions as tag (tag.name)}
        <button type="button" class="small suggestion" onclick={() => add(tag.name)}>#{tag.name}<span class="count">{tag.recipes}</span></button>
      {/each}
      {#if hidden > 0}<span class="muted more">and {hidden} more{word ? '' : ': type to find them'}</span>{/if}
    </div>
  {:else if word}
    <p class="muted note">No tag contains "{word}". Press Enter to look for it anyway.</p>
  {/if}
</div>

<style>
  .tag-filter { display: grid; gap: .4rem; min-width: 0; }
  .head { display: flex; flex-wrap: wrap; align-items: baseline; gap: .15rem .6rem; }
  .head .muted { font-size: .85rem; }
  .field { display: flex; flex-wrap: wrap; align-items: center; gap: .3rem; padding: .25rem .35rem; min-height: 2.4rem;
    border: 1px solid var(--line); border-radius: .4rem; background: var(--panel); cursor: text; }
  .field:focus-within { outline: 2px solid var(--accent); outline-offset: 2px; }
  .field input { flex: 1; min-width: 9rem; border: 0; background: none; padding: .2rem .25rem; outline: none; font: inherit; color: inherit; }
  .chosen { display: inline-flex; align-items: center; gap: .15rem; padding: .1rem .15rem .1rem .55rem; border-radius: 999px;
    background: var(--accent); color: var(--accent-ink); font-size: .85rem; }
  .chosen .icon { color: inherit; padding: .15rem; border-radius: 999px; }
  .chosen .icon:hover { color: var(--accent); background: var(--accent-ink); }
  .suggestions { display: flex; flex-wrap: wrap; align-items: center; gap: .3rem; }
  .suggestions > .muted { font-size: .85rem; margin-right: .15rem; }
  .suggestion { display: inline-flex; align-items: center; gap: .4rem; border-radius: 999px; }
  .count { font-size: .75rem; color: var(--muted); }
  .more { font-size: .85rem; }
  .note { margin: 0; font-size: .85rem; }
</style>
