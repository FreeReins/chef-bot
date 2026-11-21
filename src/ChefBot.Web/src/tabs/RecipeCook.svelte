<!-- Cooking mode, /recipes/{id}/cook: the recipe laid over the whole screen, one page at a time, for reading at arm's
     length with messy hands. It opens on the ingredients to gather, then what to start ahead (preheating, boiling water,
     long waits) when the steps ask for any, then each step on its own page with the ingredients it names, then done.
     Times in a step are buttons that start a timer; timers ring and vibrate when they end. The screen is kept awake
     where the browser allows it. Back and Next, the arrow and Page keys (and page-turner pedals that send them) and a
     swipe move between pages. This browser remembers the page, the ticked ingredients and the timers for half a day,
     so a reload or a closed tab picks up where it was. -->
<script>
  import { onDestroy, untrack } from 'svelte';
  import { ask } from '../lib/app.svelte.js';
  import X from '@lucide/svelte/icons/x';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';
  import ArrowRight from '@lucide/svelte/icons/arrow-right';
  import Timer from '@lucide/svelte/icons/timer';
  import Pause from '@lucide/svelte/icons/pause';
  import Play from '@lucide/svelte/icons/play';
  import BellRing from '@lucide/svelte/icons/bell-ring';
  import Sun from '@lucide/svelte/icons/sun';
  import Type from '@lucide/svelte/icons/type';
  import Flame from '@lucide/svelte/icons/flame';
  import Hourglass from '@lucide/svelte/icons/hourglass';
  import Check from '@lucide/svelte/icons/check';

  /** recipe: the RecipeResponse to cook. onclose(): asked to leave cooking mode. */
  let { recipe, onclose } = $props();

  /** How long this browser remembers where cooking got to. */
  const REMEMBER_MS = 12 * 60 * 60 * 1000;
  /** How long a finished timer keeps ringing unless it is stopped. */
  const RING_MS = 2 * 60 * 1000;
  const SIZE_KEY = 'chefbot.cook.size';
  const placeKey = $derived(`chefbot.cook.${recipe.id}`);

  // The steps as the server read them; a step without details still gets a page.
  const steps = $derived(recipe.stepDetails?.length === recipe.instructions.length ? recipe.stepDetails
    : recipe.instructions.map((text) => ({ text, timers: [], headStart: null, ingredients: [] })));
  const headStarts = $derived(steps.flatMap((step, index) => (step.headStart ? [{ ...step.headStart, step: index }] : [])));
  const startNow = $derived(headStarts.filter((item) => item.kind !== 'Wait'));
  const allowFor = $derived(headStarts.filter((item) => item.kind === 'Wait'));
  const pages = $derived([
    { key: 'gather', label: 'Ingredients' },
    ...(headStarts.length ? [{ key: 'ahead', label: 'Before you start' }] : []),
    ...steps.map((_, index) => ({ key: `step-${index}`, label: `Step ${index + 1}`, step: index })),
    { key: 'done', label: 'Done' },
  ]);

  const saved = untrack(remembered);
  let at = $state(saved?.at ?? 0);
  let ticked = $state(saved?.ticked ?? []);     // indexes of the ingredients set out
  let started = $state(saved?.started ?? []);   // indexes of the steps whose head start is under way
  let timers = $state(saved?.timers ?? []);     // { id, label, seconds, endsAt, left, doneAt, rangAt }
  let large = $state(rememberedSize());
  let now = $state(Date.now());
  let awake = $state(false);
  let announcement = $state('');
  let alarm = $state('');
  let dialog;

  const page = $derived(pages[Math.min(at, pages.length - 1)]);
  const step = $derived(page.step === undefined ? null : steps[page.step]);
  const progress = $derived(Math.round((at / (pages.length - 1)) * 100));
  const runningCount = $derived(timers.filter((timer) => !timer.doneAt).length);

  function remembered() {
    try {
      const value = JSON.parse(localStorage.getItem(placeKey) ?? 'null');
      return value && Date.now() - value.savedAt < REMEMBER_MS ? value : null;
    } catch { return null; }
  }

  function rememberedSize() {
    try { return localStorage.getItem(SIZE_KEY) === 'large'; } catch { return false; }
  }

  // Remembers the place in this browser as it changes.
  $effect(() => {
    const value = JSON.stringify({ at, ticked, started, timers, savedAt: Date.now() });
    try { localStorage.setItem(placeKey, value); } catch { /* kept for this visit only */ }
  });

  function toggleSize() {
    large = !large;
    try { localStorage.setItem(SIZE_KEY, large ? 'large' : 'normal'); } catch { /* kept for this visit only */ }
  }

  // ---------- moving between pages ----------

  function turnTo(index) {
    at = Math.max(0, Math.min(pages.length - 1, index));
    const shown = pages[at];
    announcement = shown.step === undefined ? shown.label : `Step ${shown.step + 1} of ${steps.length}. ${steps[shown.step].text}`;
    dialog?.querySelector('.content')?.scrollTo({ top: 0 });
  }

  function onkeydown(event) {
    if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey) return;
    if (event.target.closest?.('select, textarea, input:not([type="checkbox"])')) return;
    if (event.key === 'ArrowRight' || event.key === 'PageDown') { event.preventDefault(); turnTo(at + 1); }
    if (event.key === 'ArrowLeft' || event.key === 'PageUp') { event.preventDefault(); turnTo(at - 1); }
  }

  // A sideways swipe across the page turns it; an up-and-down one scrolls as usual.
  let swipe = null;
  function swipeStart(event) {
    swipe = event.pointerType === 'mouse' ? null : { x: event.clientX, y: event.clientY };
  }
  function swipeEnd(event) {
    if (!swipe) return;
    const dx = event.clientX - swipe.x;
    const dy = event.clientY - swipe.y;
    swipe = null;
    if (Math.abs(dx) > 70 && Math.abs(dx) > Math.abs(dy) * 2) turnTo(at + (dx < 0 ? 1 : -1));
  }

  /**
   * The step's own words cut around each time it gives, so each time can be shown as a button that starts a timer.
   * A time not found in the words (the text changed) is marked loose and shown after them.
   */
  function partsOf(current) {
    const found = [];
    let from = 0;
    for (const timer of current.timers) {
      const index = current.text.indexOf(timer.text, from);
      if (index < 0) { found.push({ timer, loose: true }); continue; }
      found.push({ text: current.text.slice(from, index) }, { timer });
      from = index + timer.text.length;
    }
    found.push({ text: current.text.slice(from) });
    return found;
  }

  // ---------- the checklists ----------

  const toggle = (list, index) => (list.includes(index) ? list.filter((item) => item !== index) : [...list, index]);

  // ---------- timers ----------

  let audio = null;

  function startTimer(stepIndex, timer) {
    // Sound can only start after a tap, so the chime is readied by the tap that starts the timer.
    try { audio ??= new AudioContext(); audio.resume(); } catch { audio = null; }
    timers.push({ id: Math.max(0, ...timers.map((item) => item.id)) + 1, label: `Step ${stepIndex + 1}: ${timer.text}`, seconds: timer.seconds,
      endsAt: Date.now() + timer.seconds * 1000, left: timer.seconds * 1000, doneAt: null, rangAt: 0 });
    now = Date.now();
  }

  const remaining = (timer) => (timer.doneAt ? 0 : timer.endsAt ? Math.max(0, timer.endsAt - now) : timer.left);
  const runningFor = (stepIndex, timer) => timers.some((item) => !item.doneAt && item.label === `Step ${stepIndex + 1}: ${timer.text}`);

  function pause(timer) {
    if (timer.endsAt) { timer.left = Math.max(0, timer.endsAt - Date.now()); timer.endsAt = null; }
    else timer.endsAt = Date.now() + timer.left;
  }

  function addMinute(timer) {
    if (timer.doneAt) { timer.doneAt = null; timer.endsAt = Date.now() + 60_000; }
    else if (timer.endsAt) timer.endsAt += 60_000;
    else timer.left += 60_000;
    now = Date.now();
  }

  const stop = (timer) => { timers = timers.filter((item) => item.id !== timer.id); };

  /** "4:05", or "1:04:05" past an hour. */
  function clock(ms) {
    const total = Math.ceil(ms / 1000);
    const hours = Math.floor(total / 3600);
    const minutes = Math.floor((total % 3600) / 60);
    const seconds = String(total % 60).padStart(2, '0');
    return hours ? `${hours}:${String(minutes).padStart(2, '0')}:${seconds}` : `${minutes}:${seconds}`;
  }

  /** Three short beeps. */
  function chime() {
    if (!audio) return;
    try {
      const start = audio.currentTime;
      for (const offset of [0, 0.25, 0.5]) {
        const tone = audio.createOscillator();
        const volume = audio.createGain();
        tone.frequency.value = 880;
        volume.gain.setValueAtTime(0.0001, start + offset);
        volume.gain.exponentialRampToValueAtTime(0.3, start + offset + 0.02);
        volume.gain.exponentialRampToValueAtTime(0.0001, start + offset + 0.2);
        tone.connect(volume).connect(audio.destination);
        tone.start(start + offset);
        tone.stop(start + offset + 0.22);
      }
    } catch { /* no sound; the timer still shows and vibrates */ }
  }

  // Counts down while any timer runs or rings. Timers keep their end time, so a sleeping tab catches up on waking.
  const busy = (timer, at) => Boolean(timer.endsAt) || (timer.doneAt !== null && at - timer.doneAt < RING_MS);
  $effect(() => {
    if (!timers.some((timer) => busy(timer, Date.now()))) return;
    const tick = setInterval(() => {
      now = Date.now();
      if (!timers.some((timer) => busy(timer, now))) clearInterval(tick);
      for (const timer of timers) {
        if (timer.endsAt && !timer.doneAt && timer.endsAt <= now) {
          timer.doneAt = timer.endsAt;
          timer.endsAt = null;
          alarm = `Time's up: ${timer.label}.`;
          navigator.vibrate?.([300, 150, 300, 150, 300]);
        }
        if (timer.doneAt && now - timer.doneAt < RING_MS && now - timer.rangAt >= 3000) {
          timer.rangAt = now;
          chime();
        }
      }
    }, 250);
    return () => clearInterval(tick);
  });

  // ---------- keeping the screen on ----------

  let lock = null;
  async function keepAwake() {
    if (!navigator.wakeLock || document.visibilityState !== 'visible') return;
    try {
      lock = await navigator.wakeLock.request('screen');
      awake = true;
      lock.addEventListener('release', () => { awake = false; });
    } catch { awake = false; }
  }
  // The browser lets go of the lock when the tab is hidden; it is asked for again on coming back.
  const onVisible = () => { if (document.visibilityState === 'visible') keepAwake(); };

  // ---------- opening and closing ----------

  $effect(() => {
    dialog.showModal();
    keepAwake();
    document.addEventListener('visibilitychange', onVisible);
    return () => {
      document.removeEventListener('visibilitychange', onVisible);
      if (dialog.open) dialog.close();
    };
  });

  onDestroy(() => {
    lock?.release().catch(() => {});
    audio?.close().catch(() => {});
  });

  async function leave() {
    if (runningCount && !(await ask({
      title: 'Leave cooking mode?',
      message: `${runningCount === 1 ? 'Your timer keeps' : `Your ${runningCount} timers keep`} counting, but won't ring until you come back to cooking mode.`,
      confirmLabel: 'Leave',
    }))) return;
    onclose();
  }

  /** Finished: forgets the place, so the next time starts from the ingredients. */
  function finish() {
    try { localStorage.removeItem(placeKey); } catch { /* nothing remembered */ }
    timers = [];
    onclose();
  }

  // Escape would close the dialog by itself; leave() decides instead, so the address stays in step.
  function cancel(event) {
    event.preventDefault();
    leave();
  }
</script>

{#snippet timerButton(stepIndex, timer)}
  {@const running = runningFor(stepIndex, timer)}
  <button type="button" class="time" class:running onclick={() => startTimer(stepIndex, timer)}
          aria-label={running ? `${timer.text}: timer running, start another` : `Start a timer for ${timer.text}`}>
    <Timer size={18} />{timer.text}
  </button>
{/snippet}

{#snippet stepText(stepIndex)}
  {@const parts = partsOf(steps[stepIndex])}
  <p class="step-text">
    {#each parts.filter((part) => !part.loose) as part, index (index)}{#if part.timer}{@render timerButton(stepIndex, part.timer)}{:else}{part.text}{/if}{/each}
  </p>
  {#if parts.some((part) => part.loose)}
    <p class="loose-timers">
      {#each parts.filter((part) => part.loose) as part, index (index)}{@render timerButton(stepIndex, part.timer)}{/each}
    </p>
  {/if}
{/snippet}

<dialog bind:this={dialog} class="cook" class:large aria-labelledby="cook-title" oncancel={cancel} {onkeydown}>
  <div class="frame">
    <header class="bar">
      <button type="button" class="icon exit" aria-label="Leave cooking mode" title="Leave cooking mode" onclick={leave}><X size={24} /></button>
      <div class="where">
        <h2 id="cook-title" class="clip" title={recipe.name}>{recipe.name}</h2>
        <p class="muted">Cooking mode · {page.step === undefined ? page.label : `Step ${page.step + 1} of ${steps.length}`}</p>
      </div>
      {#if awake}<span class="awake" title="The screen stays on while cooking mode is open"><Sun size={18} /><span class="sr-only">The screen stays on</span></span>{/if}
      <button type="button" class="icon" aria-pressed={large} aria-label="Larger text" title="Larger text" onclick={toggleSize}><Type size={22} /></button>
    </header>
    <div class="progress" aria-hidden="true"><span style:width="{progress}%"></span></div>

    {#if timers.length}
      <section class="timers" aria-label="Timers">
        {#each timers as timer (timer.id)}
          <div class="timer" class:ringing={timer.doneAt} class:paused={!timer.doneAt && !timer.endsAt}>
            <span class="timer-what">
              {#if timer.doneAt}<BellRing size={18} />{:else}<Timer size={18} />{/if}
              <span class="clip" title={timer.label}>{timer.label}</span>
            </span>
            <strong class="left">{timer.doneAt ? "Time's up" : clock(remaining(timer))}</strong>
            <span class="timer-actions">
              {#if !timer.doneAt}
                <button type="button" class="icon" aria-label="{timer.endsAt ? 'Pause' : 'Resume'} the {timer.label} timer" onclick={() => pause(timer)}>
                  {#if timer.endsAt}<Pause size={18} />{:else}<Play size={18} />{/if}
                </button>
              {/if}
              <button type="button" class="small" aria-label="+1 min on the {timer.label} timer" onclick={() => addMinute(timer)}>+1 min</button>
              <button type="button" class="small" class:primary={timer.doneAt} aria-label="{timer.doneAt ? 'Stop' : 'Cancel'} the {timer.label} timer"
                      onclick={() => stop(timer)}>{timer.doneAt ? 'Stop' : 'Cancel'}</button>
            </span>
          </div>
        {/each}
      </section>
    {/if}

    <div class="content" onpointerdown={swipeStart} onpointerup={swipeEnd} onpointercancel={() => (swipe = null)} role="presentation">
      <div class="page" class:split={step && step.ingredients.length}>
        {#if page.key === 'gather'}
          <h3 class="page-title">Gather your ingredients</h3>
          <p class="muted lead">
            Serves {recipe.servings}{recipe.totalMinutes ? ` · about ${recipe.totalMinutes} min in all` : ''}.
            Tick each one off as you set it out.
          </p>
          {#if recipe.ingredients.length}
            <ul class="gather">
              {#each recipe.ingredients as line, index (index)}
                <li>
                  <label class:ticked={ticked.includes(index)}>
                    <input type="checkbox" checked={ticked.includes(index)} onchange={() => (ticked = toggle(ticked, index))}>
                    <span>{line}</span>
                  </label>
                </li>
              {/each}
            </ul>
            <p class="muted count" aria-live="polite">{ticked.length} of {recipe.ingredients.length} set out</p>
          {:else}
            <p class="muted">This recipe lists no ingredients.</p>
          {/if}
        {:else if page.key === 'ahead'}
          <h3 class="page-title">Before you start</h3>
          {#if startNow.length}
            <h4><Flame size={20} />Start these now</h4>
            <p class="muted">They take a while, and a later step needs them ready.</p>
            <ul class="ahead">
              {#each startNow as item (item.step)}
                <li>
                  <label class:ticked={started.includes(item.step)}>
                    <input type="checkbox" checked={started.includes(item.step)} onchange={() => (started = toggle(started, item.step))}>
                    <span>{item.text}</span>
                  </label>
                  <button type="button" class="small" onclick={() => turnTo(pages.findIndex((entry) => entry.step === item.step))}>Needed at step {item.step + 1}</button>
                </li>
              {/each}
            </ul>
          {/if}
          {#if allowFor.length}
            <h4><Hourglass size={20} />Leave time for</h4>
            <p class="muted">These steps wait a long time. Read ahead so dinner isn't late.</p>
            <ul class="ahead">
              {#each allowFor as item (item.step)}
                <li>
                  <span class="wait">{item.text}</span>
                  <button type="button" class="small" onclick={() => turnTo(pages.findIndex((entry) => entry.step === item.step))}>Step {item.step + 1}</button>
                </li>
              {/each}
            </ul>
          {/if}
        {:else if step}
          <div class="main-step">
            <p class="eyebrow">Step {page.step + 1} of {steps.length}</p>
            <h3 class="sr-only">Step {page.step + 1}</h3>
            {@render stepText(page.step)}
          </div>
          {#if step.ingredients.length}
            <aside class="uses" aria-label="Ingredients for this step">
              <h4>For this step</h4>
              <ul class="plain">
                {#each step.ingredients as index (index)}<li>{recipe.ingredients[index]}</li>{/each}
              </ul>
            </aside>
          {/if}
        {:else}
          <div class="done">
            <Check size={48} />
            <h3 class="page-title">That's every step</h3>
            <p class="lead">Enjoy {recipe.name}.</p>
            {#if recipe.notes}<p class="muted notes">{recipe.notes}</p>{/if}
          </div>
        {/if}
      </div>
    </div>

    <nav class="turn" aria-label="Pages of the recipe">
      <button type="button" class="big" onclick={() => turnTo(at - 1)} disabled={at === 0}><ArrowLeft size={22} />Back</button>
      <select aria-label="Go to" value={at} onchange={(event) => turnTo(Number(event.currentTarget.value))}>
        {#each pages as entry, index (entry.key)}<option value={index}>{entry.label}</option>{/each}
      </select>
      {#if page.key === 'done'}
        <button type="button" class="big primary" onclick={finish}>Finish</button>
      {:else}
        <button type="button" class="big primary" onclick={() => turnTo(at + 1)}>Next<ArrowRight size={22} /></button>
      {/if}
    </nav>
  </div>
  <p class="sr-only" aria-live="polite">{announcement}</p>
  <p class="sr-only" aria-live="assertive">{alarm}</p>
</dialog>

<style>
  /* The whole screen, above everything else. */
  dialog.cook { position: fixed; inset: 0; width: 100vw; height: 100dvh; max-width: none; max-height: none; margin: 0; padding: 0;
    border: 0; background: var(--bg); color: var(--ink); --step-size: clamp(1.3rem, 1rem + 1.6vw, 2.1rem); --list-size: 1.15rem; }
  dialog.cook.large { --step-size: clamp(1.6rem, 1.2rem + 2.4vw, 2.9rem); --list-size: 1.45rem; }
  dialog::backdrop { background: var(--bg); }
  .frame { display: flex; flex-direction: column; height: 100%; }

  .bar { display: flex; align-items: center; gap: .5rem; padding: .5rem 1rem; padding-top: max(.5rem, env(safe-area-inset-top)); }
  .where { flex: 1; min-width: 0; }
  .where h2 { margin: 0; font-size: 1.1rem; }
  .where p { font-size: .85rem; }
  .bar button.icon[aria-pressed="true"] { color: var(--accent-ink); background: var(--accent); border-radius: .4rem; }
  .awake { display: inline-flex; color: var(--muted); padding: .5rem; }
  .progress { height: 4px; background: var(--line); }
  .progress span { display: block; height: 100%; background: var(--accent); transition: width .2s; }
  @media (prefers-reduced-motion: reduce) { .progress span { transition: none; } }

  .timers { display: flex; flex-wrap: wrap; gap: .5rem; padding: .5rem 1rem; border-bottom: 1px solid var(--line); background: var(--panel); }
  .timer { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem .75rem; flex: 1 1 18rem; min-width: 0; padding: .35rem .6rem;
    border: 1px solid var(--line); border-radius: .5rem; background: var(--bg); }
  .timer.ringing { border-color: var(--warn); border-width: 2px; }
  .timer.ringing .timer-what, .timer.ringing .left { color: var(--warn); }
  .timer-what { display: flex; align-items: center; gap: .35rem; flex: 1 1 8rem; min-width: 0; color: var(--muted); font-size: .9rem; }
  .left { font-size: 1.35rem; font-variant-numeric: tabular-nums; }
  .timer.paused .left { color: var(--muted); }
  .timer-actions { display: flex; align-items: center; gap: .35rem; }

  .content { flex: 1; min-height: 0; overflow-y: auto; overscroll-behavior: contain; padding: 1.25rem 1rem 2rem; }
  .page { max-width: 62rem; margin: 0 auto; }
  /* Wide screens, such as a tablet on its side: the step and what it uses side by side. */
  @media (min-width: 56rem) {
    .page.split { display: grid; grid-template-columns: minmax(0, 3fr) minmax(14rem, 2fr); gap: 2rem; align-items: start; }
  }
  .page-title { font-size: 1.5rem; margin: 0 0 .25rem; }
  .lead { font-size: 1.05rem; margin: 0 0 1rem; }
  h4 { display: flex; align-items: center; gap: .4rem; margin: 1.5rem 0 .2rem; font-size: 1.15rem; }

  ul.gather, ul.ahead { list-style: none; margin: .5rem 0 0; padding: 0; font-size: var(--list-size); }
  ul.gather li, ul.ahead li { border-top: 1px solid var(--line); }
  ul.gather li:first-child, ul.ahead li:first-child { border-top: 0; }
  ul.ahead li { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem 1rem; padding: .35rem 0; }
  ul.gather label, ul.ahead label { display: flex; align-items: flex-start; gap: .75rem; padding: .6rem 0; color: var(--ink);
    font-size: inherit; cursor: pointer; flex: 1 1 14rem; }
  ul.gather input, ul.ahead input { width: 1.5rem; height: 1.5rem; margin: .15em 0 0; flex: none; accent-color: var(--accent); }
  label.ticked span { color: var(--muted); text-decoration: line-through; }
  .wait { flex: 1 1 14rem; padding: .4rem 0; }
  .count { margin-top: .75rem; }

  .eyebrow { margin: 0 0 .5rem; color: var(--muted); font-weight: 600; letter-spacing: .02em; }
  .step-text { font-size: var(--step-size); line-height: 1.45; margin: 0; overflow-wrap: anywhere; }
  .loose-timers { display: flex; flex-wrap: wrap; gap: .5rem; margin: 1rem 0 0; }
  /* A time in the step: a button that starts a timer. */
  button.time { display: inline-flex; align-items: center; gap: .3rem; padding: .05em .45em; margin: 0 .1em; font-size: inherit;
    line-height: 1.3; border: 2px solid var(--accent); border-radius: .4em; color: var(--accent); background: var(--panel); vertical-align: baseline; }
  button.time.running { background: var(--accent); color: var(--accent-ink); }
  .uses { margin-top: 1.5rem; padding: .8rem 1rem; border: 1px solid var(--line); border-radius: .5rem; background: var(--panel);
    font-size: var(--list-size); }
  .uses h4 { margin: 0 0 .25rem; font-size: 1rem; color: var(--muted); }
  @media (min-width: 56rem) { .uses { margin-top: 2.2rem; position: sticky; top: 0; } }

  .done { display: grid; justify-items: center; text-align: center; gap: .5rem; padding-top: 2rem; color: var(--good); }
  .done .page-title, .done .lead { color: var(--ink); }
  .notes { max-width: 36rem; white-space: pre-line; }

  /* Big targets along the bottom, where a thumb reaches them. */
  .turn { display: grid; grid-template-columns: 1fr minmax(0, auto) 1fr; gap: .5rem; align-items: center; padding: .6rem 1rem;
    padding-bottom: max(.6rem, env(safe-area-inset-bottom)); border-top: 1px solid var(--line); background: var(--panel); }
  .turn select { max-width: 12rem; }
  button.big { display: inline-flex; align-items: center; justify-content: center; gap: .4rem; min-height: 3.25rem; font-size: 1.1rem; font-weight: 600; }
  .turn button.big:last-child { justify-self: end; min-width: min(10rem, 100%); }
  .turn button.big:first-child { justify-self: start; min-width: min(8rem, 100%); }

  /* Phones on their side: little height, so the bars get thinner. */
  @media (max-height: 30rem) {
    .bar { padding-block: .2rem; }
    .where p { display: none; }
    .content { padding-block: .75rem 1rem; }
    button.big { min-height: 2.6rem; }
  }
</style>
