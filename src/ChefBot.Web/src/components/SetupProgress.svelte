<!-- The bar across the top of each setup step: one segment per step, filled up to the current one. Once the account
     exists, every other step can be opened from here. -->
<script>
  import { app, setupSteps, openSetupStep } from '../lib/app.svelte.js';
  import Check from '@lucide/svelte/icons/check';

  const steps = $derived(setupSteps());
  const current = $derived(steps.findIndex((step) => step.screen === app.screen));
  const canMove = $derived(Boolean(app.session?.user));
</script>

<nav class="progress" aria-label="Setup progress">
  <p class="steps">Setup, step {current + 1} of {steps.length}</p>
  <ol style:grid-template-columns="repeat({steps.length}, 1fr)">
    {#each steps as step, index (step.screen)}
      <li class:done={index < current} class:current={index === current} aria-current={index === current ? 'step' : undefined}>
        {#if canMove && index !== current}
          <button type="button" class="step" onclick={() => openSetupStep(step.screen)}>
            <span class="segment"></span>
            <span class="label">{#if index < current}<Check size={13} /><span class="sr-only">Done: </span>{/if}{step.label}</span>
          </button>
        {:else}
          <span class="step">
            <span class="segment"></span>
            <span class="label">{#if index < current}<Check size={13} /><span class="sr-only">Done: </span>{/if}{step.label}</span>
          </span>
        {/if}
      </li>
    {/each}
  </ol>
</nav>

<style>
  .progress { margin-bottom: 1rem; }
  .steps { color: var(--muted); font-size: .85rem; letter-spacing: .04em; text-transform: uppercase; margin: 0 0 .25rem; }
  ol { list-style: none; margin: .35rem 0 0; padding: 0; display: grid; gap: .4rem; }
  li { font-size: .8rem; color: var(--muted); }
  .step { display: grid; gap: .3rem; width: 100%; text-align: left; font: inherit; color: inherit; }
  button.step { padding: 0; border: 0; background: none; cursor: pointer; }
  .label { display: inline-flex; align-items: center; gap: .25rem; }
  button.step:hover .label { color: var(--ink); text-decoration: underline; }
  .segment { height: .4rem; border-radius: 1rem; background: var(--line); }
  li.done .segment, li.current .segment { background: var(--accent); }
  li.current { color: var(--ink); font-weight: 600; }
</style>
