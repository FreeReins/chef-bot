<!-- The bar across the top of each setup step: one segment per step, filled up to the current one. -->
<script>
  /** current: the step being shown, counting from 1. */
  let { current } = $props();

  const STEPS = ['Your account', 'Secure connection', 'Recipes'];
</script>

<nav class="progress" aria-label="Setup progress">
  <p class="steps">Setup, step {current} of {STEPS.length}</p>
  <ol>
    {#each STEPS as label, index (label)}
      {@const step = index + 1}
      <li class:done={step < current} class:current={step === current} aria-current={step === current ? 'step' : undefined}>
        <span class="segment"></span>
        <span class="label">{step < current ? '✓ ' : ''}{label}</span>
      </li>
    {/each}
  </ol>
</nav>

<style>
  .progress { margin-bottom: 1rem; }
  .steps { color: var(--muted); font-size: .85rem; letter-spacing: .04em; text-transform: uppercase; margin: 0 0 .25rem; }
  ol { list-style: none; margin: .35rem 0 0; padding: 0; display: grid; grid-template-columns: repeat(3, 1fr); gap: .4rem; }
  li { display: grid; gap: .3rem; font-size: .8rem; color: var(--muted); }
  .segment { height: .4rem; border-radius: 1rem; background: var(--line); }
  li.done .segment, li.current .segment { background: var(--accent); }
  li.current { color: var(--ink); font-weight: 600; }
</style>
