<!-- Links that download recipes as a file, one per format the server writes (GET /api/recipes/{id}/export or
     /api/recipes/export, with ?format=): a PDF to print or keep, Markdown for notes apps, plain text, and the
     schema.org file other recipe apps import. Used in a recipe's ⋯ menu and in the library's "Download all" menu;
     `onpick` lets the menu close once one is chosen. -->
<script>
  import FileText from '@lucide/svelte/icons/file-text';
  import FileCode from '@lucide/svelte/icons/file-code';
  import FileType from '@lucide/svelte/icons/file-type';
  import FileBraces from '@lucide/svelte/icons/file-braces';

  let { href, onpick = () => {} } = $props();

  const FORMATS = [
    { format: 'pdf', label: 'Download as PDF', icon: FileText },
    { format: 'markdown', label: 'Download as Markdown', icon: FileCode },
    { format: 'text', label: 'Download as text', icon: FileType },
    { format: 'json', label: 'Download recipe file', icon: FileBraces, title: 'A schema.org file, which other recipe apps and ChefBot import' },
  ];
</script>

{#each FORMATS as { format, label, icon: Icon, title } (format)}
  <a href={format === 'json' ? href : `${href}?format=${format}`} download {title} onclick={onpick}>
    <Icon size={16} />{label}</a>
{/each}

<style>
  a { display: flex; align-items: center; gap: .35rem; padding: .4rem .8rem; border-radius: .4rem; color: var(--ink); text-decoration: none; font-size: .95rem; }
  a:hover { background: var(--chip); }
</style>
