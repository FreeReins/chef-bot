<!-- The bar across the top of setup: one segment per step, filled up to the current one. App.svelte shows it once
     above the steps, so it stays put while they slide and its segments fill as you go. Once the account exists,
     every other step can be opened from here, except while something is still being saved or imported. -->
<script>
  import { app, setupSteps, openSetupStep } from '../lib/app.svelte.js';
  import Check from '@lucide/svelte/icons/check';

  const steps = $derived(setupSteps());
  const current = $derived(steps.findIndex((step) => step.screen === app.screen));
  const canMove = $derived(Boolean(app.session?.user));
  const locked = $derived(app.busy > 0 || app.importing > 0);
</script>

<nav class="progress" aria-label="Setup progress">
  <p class="steps">Setup, step {current + 1} of {steps.length}</p>
  <ol style:grid-template-columns="repeat({steps.length}, 1fr)">
    {#each steps as step, index (step.screen)}
      <li class:done={index < current} class:current={index === current} aria-current={index === current ? 'step' : undefined}>
        {#if canMove && index !== current}
          <button type="button" class="step" disabled={locked} onclick={() => openSetupStep(step.screen)}>
            <span class="segment"></span>
            <span class="label">{#if index < current}<span class="tick" aria-hidden="true"><Check size={13} /></span><span class="sr-only">Done: </span>{/if}{step.label}</span>
          </button>
        {:else}
          <span class="step">
            <span class="segment"></span>
            <span class="label">{#if index < current}<span class="tick" aria-hidden="true"><Check size={13} /></span><span class="sr-only">Done: </span>{/if}{step.label}</span>
          </span>
        {/if}
      </li>
    {/each}
  </ol>
</nav>

<style>
  /* Always as wide as `.gate.wide`, so the bar doesn't change size between steps; on the narrow steps it reaches
     out past both sides of the column. */
  .progress { --width: max(100%, min(52rem, 100vw - 2rem)); margin: 0 calc((100% - var(--width)) / 2) 1rem; }
  .steps { color: var(--muted); font-size: .85rem; letter-spacing: .04em; text-transform: uppercase; margin: 0 0 .25rem; }
  ol { list-style: none; margin: .35rem 0 0; padding: 0; display: grid; gap: .4rem; }
  li { font-size: .8rem; color: var(--muted); }
  .step { display: grid; gap: .3rem; width: 100%; text-align: left; font: inherit; color: inherit; }
  button.step { padding: 0; border: 0; background: none; cursor: pointer; }
  .label { display: inline-flex; align-items: center; gap: .25rem; }
  button.step:hover .label { color: var(--ink); text-decoration: underline; }
  /* The fill grows across its segment from the left as you reach a step, and shrinks back if you go back. */
  .segment { position: relative; height: .4rem; border-radius: 1rem; background: var(--line); overflow: hidden; }
  .segment::after { content: ""; position: absolute; inset: 0; border-radius: inherit; transform: scaleX(0); transform-origin: left;
    background: linear-gradient(90deg, var(--accent), color-mix(in srgb, var(--accent) 70%, var(--accent-2))); }
  li.done .segment::after, li.current .segment::after { transform: scaleX(1); }
  .tick { display: inline-flex; color: var(--accent); }
  button.step:disabled { cursor: progress; opacity: 1; }
  button.step:disabled:hover .label { color: inherit; text-decoration: none; }
  @media (prefers-reduced-motion: no-preference) {
    .segment::after { transition: transform .45s cubic-bezier(.3, .9, .3, 1) .1s; }
    li.current .segment { animation: glow 1.2s ease-out .45s; }
    .tick { animation: pop .35s cubic-bezier(.3, 1.6, .5, 1); }
  }
  @keyframes glow { 0% { box-shadow: 0 0 0 0 color-mix(in srgb, var(--accent) 45%, transparent); } 100% { box-shadow: 0 0 0 .4rem transparent; } }
  li.current { color: var(--ink); font-weight: 600; }
</style>
