<!-- A list of words to add to and remove from, shown as chips, such as the labels that sort imported recipes into a
     course. Changes the list in place and calls onchange after each change. -->
<script>
  import Plus from '@lucide/svelte/icons/plus';
  import X from '@lucide/svelte/icons/x';

  /** words: string[], changed in place. label: what the words are for, for screen readers. id: a unique prefix. */
  let { words = $bindable(), label, id, placeholder = 'Add a word', onchange = () => {} } = $props();

  let typed = $state('');

  function add(event) {
    event.preventDefault();
    const word = typed.trim().toLowerCase();
    if (word && !words.includes(word)) {
      words.push(word);
      onchange();
    }
    typed = '';
  }

  function remove(word) {
    words.splice(words.indexOf(word), 1);
    onchange();
  }
</script>

<div class="word-list">
  {#if words.length}
    <div class="chips">
      {#each words as word (word)}
        <span class="chip word">{word}
          <button type="button" class="remove" aria-label="Remove {word} from {label}" onclick={() => remove(word)}><X size={14} /></button>
        </span>
      {/each}
    </div>
  {:else}
    <p class="muted none">No words.</p>
  {/if}
  <form class="inline add-word" onsubmit={add}>
    <input id="{id}-new" bind:value={typed} maxlength="50" {placeholder} aria-label="New word for {label}">
    <button type="submit" class="small with-icon"><Plus size={16} />Add</button>
  </form>
</div>

<style>
  .word-list { display: grid; gap: .35rem; }
  .word { display: inline-flex; gap: .25rem; align-items: center; color: var(--ink); }
  .word .remove { padding: 0 .1rem; }
  .none { margin: 0; font-size: .85rem; }
  .add-word input { flex: 1 1 12rem; }
</style>
