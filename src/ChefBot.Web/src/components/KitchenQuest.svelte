<!-- The first steps in the kitchen, on the Achievements page: six things to try, ticked off as they happen. Done
     steps shrink to chips; the rest are listed with the next one picked out and a link to where to do it. Inviting
     someone is a seventh, optional step for admins, which "Just me, thanks" puts away for good: it never counts
     towards finishing and earns nothing. lib/achievements.js has the wording. -->
<script>
  import { app, isAdmin, isPutAway, putAway } from '../lib/app.svelte.js';
  import { STEPS, questLine } from '../lib/achievements.js';
  import { celebrate, popIn } from '../lib/motion.js';
  import Check from '@lucide/svelte/icons/check';

  const progress = $derived(app.achievements);
  const required = $derived(progress?.starter.filter((step) => !step.optional) ?? []);
  const guest = $derived(progress?.starter.find((step) => step.optional) ?? null);
  const finished = $derived(required.filter((step) => step.tier > 0));
  const left = $derived(required.filter((step) => step.tier === 0));
  const next = $derived(left[0]?.kind ?? null);
  const graduated = $derived(required.length > 0 && left.length === 0);

  let waved = $state(isPutAway('guest-waved'));
  const showGuest = $derived(guest && guest.tier === 0 && !waved && isAdmin());

  let heading = $state();

  // The first time this viewer sees all six steps done, a little burst.
  $effect(() => {
    if (!graduated || !heading || isPutAway('quest-graduated')) return;
    putAway('quest-graduated');
    celebrate(heading, { count: 22 });
  });

  function waveOff() {
    putAway('guest-waved');
    waved = true;
  }
</script>

{#if progress}
  <section class="card quest" class:graduated aria-labelledby="quest-heading">
    <h3 id="quest-heading" bind:this={heading}>{graduated ? 'Kitchen apprenticeship complete' : 'First steps in the kitchen'}</h3>
    <p class="muted">{graduated ? questLine(finished.length, required.length) : `${finished.length} of ${required.length} done. ${questLine(finished.length, required.length)}`}</p>
    <div class="meter" role="progressbar" aria-label="First steps done" aria-valuemin="0" aria-valuemax={required.length}
         aria-valuenow={finished.length} aria-valuetext="{finished.length} of {required.length}">
      <span style:width="{(finished.length / required.length) * 100}%"></span>
    </div>

    {#if finished.length > 0}
      <ul class="done-steps" aria-label="Done">
        {#each finished as step (step.kind)}
          <li title={STEPS[step.kind].done}>
            <span class="tick" aria-hidden="true" in:popIn><Check size={13} strokeWidth={3} /></span>{STEPS[step.kind].title}
          </li>
        {/each}
      </ul>
    {/if}

    {#if left.length > 0}
      <ol class="steps" aria-label="Still to try">
        {#each left as step (step.kind)}
          {@const words = STEPS[step.kind]}
          <li class:next={step.kind === next}>
            <span class="mark" aria-hidden="true">{words.emoji}</span>
            <span class="text">
              <strong>{words.title}</strong>
              <span class="muted">{words.hint}</span>
            </span>
            {#if step.kind === next}<a class="button small" href={words.href}>{words.cta}</a>{/if}
          </li>
        {/each}
      </ol>
    {/if}

    {#if showGuest}
      {@const words = STEPS[guest.kind]}
      <div class="optional">
        <span class="mark" aria-hidden="true">{words.emoji}</span>
        <span class="text">
          <strong>{words.title} <span class="chip">Optional</span></strong>
          <span class="muted">{words.hint}</span>
        </span>
        <span class="choices">
          <a class="button small" href={words.href}>{words.cta}</a>
          <button type="button" class="small" onclick={waveOff}>Just me, thanks</button>
        </span>
      </div>
    {/if}
  </section>
{/if}

<style>
  .quest { display: grid; gap: .6rem; padding: 1rem 1.1rem; margin: 0;
    background: linear-gradient(135deg, color-mix(in srgb, var(--noodle) 12%, var(--panel)), var(--panel) 60%); }
  .quest > p { margin: -.4rem 0 0; }
  .meter { height: .5rem; border-radius: 1rem; background: var(--line); overflow: hidden; }
  .meter span { display: block; height: 100%; border-radius: inherit;
    background: linear-gradient(90deg, var(--accent), color-mix(in srgb, var(--accent) 60%, var(--noodle))); }
  .done-steps { list-style: none; margin: 0; padding: 0; display: flex; flex-wrap: wrap; gap: .35rem; }
  .done-steps li { display: inline-flex; align-items: center; gap: .3rem; padding: .1rem .6rem .1rem .35rem; border-radius: 1rem;
    font-size: .8rem; color: var(--good); background: var(--good-bg); }
  .tick { display: inline-flex; }
  .steps { list-style: none; margin: 0; padding: 0; display: grid; gap: .35rem; }
  .steps li, .optional { display: flex; gap: .6rem; align-items: center; padding: .35rem .5rem; border-radius: .4rem; }
  .steps li.next { background: var(--accent-bg); }
  .text { display: grid; flex: 1 1 0; min-width: 0; }
  .text .muted { font-size: .85rem; }
  .steps .button { white-space: nowrap; }
  .mark { flex: none; display: inline-grid; place-items: center; width: 1.8rem; height: 1.8rem; border-radius: 50%;
    background: var(--chip); font-size: 1rem; line-height: 1; }
  .optional { border-top: 1px dashed var(--line); border-radius: 0; padding-top: .6rem; flex-wrap: wrap; }
  .optional .text { flex: 1 1 calc(100% - 2.6rem); }
  .optional .chip { margin-left: .3rem; font-weight: 500; }
  .choices { display: flex; flex-wrap: wrap; gap: .4rem; margin-left: auto; }
  @media (prefers-reduced-motion: no-preference) {
    .meter span { transition: width .6s cubic-bezier(.3, .9, .3, 1); }
    .steps li.next .mark { animation: nudge 2.4s ease-in-out infinite; }
    .graduated h3 { animation: bounce .6s cubic-bezier(.3, 1.6, .5, 1); }
  }
  @keyframes nudge { 0%, 80%, 100% { transform: none; } 88% { transform: rotate(-12deg) scale(1.1); } 94% { transform: rotate(10deg) scale(1.1); } }
  @keyframes bounce { 0% { transform: scale(.9); } 60% { transform: scale(1.04); } 100% { transform: none; } }
</style>
