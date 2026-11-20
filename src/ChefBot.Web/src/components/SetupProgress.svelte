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
