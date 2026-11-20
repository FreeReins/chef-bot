<script>
  /** tone: '', 'good', 'warn' or 'bad'. */
  let { tone = '', children } = $props();
</script>

<span class="chip {tone}">{@render children()}</span>
