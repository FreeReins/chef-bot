<!-- A link to a page of the documentation built into ChefBot (/help), opened in a new tab so the page keeps its place.
     page is the path under docs/ without ".md", optionally with #section; the text says what the guide is about. -->
<script>
  import { helpUrl } from '../lib/help.js';
  import CircleHelp from '@lucide/svelte/icons/circle-help';

  let { page, children } = $props();
</script>

<a class="help-link" href={helpUrl(page)} target="_blank" rel="noopener"><CircleHelp size={16} aria-hidden="true" /><span>{@render children()}</span></a>

<style>
  .help-link { display: inline-flex; align-items: center; gap: .35rem; }
  .help-link :global(svg) { flex: none; }
</style>
