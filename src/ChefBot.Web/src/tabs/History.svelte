<!-- The household's food history as a timeline: one long noodle down the page, with a sign for each month and its
     moments hanging off it as plates. Newest at the top; "Dig deeper" loads older months. The server says what
     happened (HistoryResponse); lib/history.js picks the wording. -->
<script>
  import { onMount } from 'svelte';
  import { run } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { shortDate, plural } from '../lib/format.js';
  import { tell, captionText, chapterTitle, monthName } from '../lib/history.js';
  import ScrollText from '@lucide/svelte/icons/scroll-text';

  let chapters = $state([]);
  let olderBefore = $state(null);
  let since = $state(null);
  let totalDinners = $state(0);
  let loaded = $state(false);

  async function load(before = null) {
    const answer = await api('GET', before ? `/api/history?before=${before}` : '/api/history');
    chapters = before ? [...chapters, ...answer.chapters] : answer.chapters;
    olderBefore = answer.olderBefore;
    since = answer.since;
    totalDinners = answer.totalDinners;
    loaded = true;
  }

  onMount(() => { run(() => load()); });
</script>

<section class="history">
  <div class="intro">
    <h2>The Noodle of Time</h2>
    {#if since}
      <p class="muted">The household's food story since {monthName(since)}, with {plural(totalDinners, 'dinner')} cooked so far.</p>
    {/if}
  </div>

  {#if loaded && chapters.length === 0}
    <div class="card empty">
      <span class="empty-icon" style="--area: var(--area-history)" aria-hidden="true"><ScrollText size={26} /></span>
      <p class="muted">The noodle is still dry. Add a recipe or mark a dinner as cooked, and the story starts here.</p>
    </div>
  {:else if chapters.length > 0}
    <div class="timeline">
      <span class="noodle" aria-hidden="true"></span>
      {#each chapters as chapter (chapter.month)}
        <section class="chapter" aria-labelledby="chapter-{chapter.month}">
          <header class="sign">
            <h3 id="chapter-{chapter.month}">{chapterTitle(chapter)}</h3>
            <p>{monthName(chapter.month)}{#if chapter.dinners > 0} · {plural(chapter.dinners, 'dinner')}{/if}</p>
          </header>
          <ol class="moments">
            {#each chapter.moments as moment, index (`${moment.kind}-${moment.date}-${index}`)}
              {@const told = tell(moment)}
              <li class="moment" class:right={index % 2 === 1} style="--delay: {Math.min(index, 8) * 60}ms">
                <span class="plate" aria-hidden="true">{told.emoji}</span>
                <article class="card" aria-label={captionText(told.parts)}>
                  <p class="caption">
                    {#each told.parts as part, partIndex (partIndex)}
                      {#if typeof part === 'string'}{part}{:else if part.recipeId}<a href="/recipes/{part.recipeId}">{part.dish}</a>{:else}<strong>{part.dish}</strong>{/if}
                    {/each}
                  </p>
                  {#if moment.dishes.length > 0}
                    <ul class="dishes">
                      {#each moment.dishes as served, dishIndex (dishIndex)}
                        <li>{#if served.recipeId}<a href="/recipes/{served.recipeId}">{served.title}</a>{:else}{served.title}{/if}</li>
                      {/each}
                      {#if moment.kind === 'RecipeStampede' && moment.count > moment.dishes.length}
                        <li class="more">and {moment.count - moment.dishes.length} more</li>
                      {/if}
                    </ul>
                  {/if}
                  <p class="when muted"><time datetime={moment.date}>{shortDate(moment.date)}</time></p>
                </article>
              </li>
            {/each}
          </ol>
        </section>
      {/each}
      {#if olderBefore}
        <div class="deeper">
          <button onclick={() => run(() => load(olderBefore))}>Dig deeper</button>
        </div>
      {:else}
        <p class="the-end muted"><span aria-hidden="true">🍝</span> The very first forkful.</p>
      {/if}
    </div>
  {/if}
</section>

<style>
  .intro { margin-bottom: 1rem; }
  .intro h2 { margin-bottom: .25rem; }
  .intro p { margin: 0; }

  /* The noodle: a wavy line drawn as a mask, so it takes the pasta colour in both themes, repeating down the page. */
  .timeline { position: relative; padding: .5rem 0 1rem; --noodle-x: 50%; }
  .noodle { position: absolute; top: 0; bottom: 0; left: var(--noodle-x); width: 26px; translate: -50% 0; z-index: 0;
    background: var(--noodle);
    mask: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='26' height='90' viewBox='0 0 26 90'%3E%3Cpath d='M13 0 C 25 22, 1 23, 13 45 S 1 68, 13 90' fill='none' stroke='black' stroke-width='6' stroke-linecap='round'/%3E%3C/svg%3E") repeat-y center top / 26px 90px; }

  .chapter { position: relative; z-index: 1; margin: 0 0 1.5rem; }
  /* Each month's sign hangs on the noodle, in sauce red. */
  .sign { position: relative; margin: 0 auto 1rem; width: fit-content; max-width: min(100%, 26rem); text-align: center;
    background: var(--panel); border: 2px solid var(--area-history); border-radius: 1rem; padding: .5rem 1.1rem;
    box-shadow: 0 2px 0 var(--area-history), 0 4px 14px var(--shadow); rotate: -1.5deg; }
  .chapter:nth-of-type(even) .sign { rotate: 1.5deg; }
  .sign h3 { margin: 0; font-size: 1.1rem; }
  .sign p { margin: .1rem 0 0; color: var(--muted); font-size: .85rem; }

  .moments { list-style: none; margin: 0; padding: 0; display: grid; gap: .25rem; }
  /* Wide screens: the plate sits on the noodle and its card alternates left and right of it. */
  .moment { display: grid; grid-template-columns: 1fr 3rem 1fr; align-items: center; gap: .5rem;
    animation: wobble .5s ease-out both; animation-delay: var(--delay); }
  .moment .plate { grid-column: 2; grid-row: 1; }
  .moment .card { grid-column: 1; grid-row: 1; margin: 0; }
  .moment.right .card { grid-column: 3; }
  .plate { display: inline-grid; place-items: center; width: 2.6rem; height: 2.6rem; justify-self: center; font-size: 1.3rem;
    border-radius: 50%; background: var(--panel); border: 3px double var(--line); box-shadow: 0 2px 6px var(--shadow);
    transition: rotate .2s; }
  .moment:hover .plate { rotate: -12deg; }
  .caption { margin: 0; }
  .caption strong { font-weight: 600; }
  .when { margin: .3rem 0 0; font-size: .8rem; }
  .dishes { list-style: none; margin: .45rem 0 0; padding: 0; display: flex; flex-wrap: wrap; gap: .3rem; }
  .dishes li { background: var(--chip); border-radius: 999px; padding: .1rem .6rem; font-size: .85rem; }
  .dishes li.more { background: none; color: var(--muted); }

  .deeper, .the-end { position: relative; z-index: 1; text-align: center; margin: .5rem 0 0; }
  .deeper button { background: var(--panel); }
  .the-end { background: var(--bg); width: fit-content; margin-inline: auto; padding: .25rem .75rem; }

  /* Plates arrive with a little wobble, as if just set down. */
  @keyframes wobble {
    0% { opacity: 0; translate: 0 6px; rotate: -2deg; }
    60% { opacity: 1; rotate: 1deg; }
    100% { opacity: 1; translate: 0 0; rotate: 0deg; }
  }
  @media (prefers-reduced-motion: reduce) {
    .moment { animation: none; }
    .plate { transition: none; }
    .moment:hover .plate { rotate: none; }
  }

  /* Phones: the noodle runs down the left and every card hangs to its right. */
  @media (max-width: 40rem) {
    .timeline { --noodle-x: 1.3rem; }
    .sign { margin-left: 0; rotate: none; }
    .chapter:nth-of-type(even) .sign { rotate: none; }
    .moment { grid-template-columns: 2.6rem minmax(0, 1fr); }
    .moment .plate { grid-column: 1; }
    .moment .card, .moment.right .card { grid-column: 2; }
    .deeper, .the-end { text-align: left; margin-left: 0; }
  }
</style>
