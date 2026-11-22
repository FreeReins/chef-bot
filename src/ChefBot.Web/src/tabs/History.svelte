<!-- The household's food history as a timeline: one long noodle down the page, with a sign for each month and its
     moments hanging off it as plates. Newest at the top; "Dig deeper" loads older months. The server says what
     happened (HistoryResponse); lib/history.js picks the wording. -->
<script>
  import { onMount } from 'svelte';
  import { onLive, run } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { shortDate, fullDate, plural, favouriteOf } from '../lib/format.js';
  import { tell, captionText, chapterTitle, monthName, dayOf, timed } from '../lib/history.js';
  import ScrollText from '@lucide/svelte/icons/scroll-text';
  import { noodle } from '../lib/noodle.js';

  let chapters = $state([]);
  let olderBefore = $state(null);
  let since = $state(null);
  let totalDinners = $state(0);
  let loaded = $state(false);
  let dug = false; // older months were loaded with "Dig deeper"

  async function load(before = null) {
    dug = before !== null;
    const answer = await api('GET', before ? `/api/history?before=${before}` : '/api/history');
    chapters = before ? [...chapters, ...answer.chapters] : answer.chapters;
    olderBefore = answer.olderBefore;
    since = answer.since;
    totalDinners = answer.totalDinners;
    loaded = true;
  }

  onMount(() => { run(() => load()); });
  // Something new happened in the kitchen while the history is open. Not while digging through older months, which
  // a reload would fold away; they get it next time the tab opens.
  onMount(() => onLive('history', () => (dug ? null : load())));
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
    <div class="timeline" use:noodle>
      <svg class="noodle" aria-hidden="true">
        <defs>
          <radialGradient id="noodle-glow" gradientUnits="userSpaceOnUse" cx="40" cy="-999" r="100">
            <stop offset="0" /><stop offset="1" stop-opacity="0" />
          </radialGradient>
        </defs>
        <path class="pasta" /><path class="glow" />
      </svg>
      {#each chapters as chapter (chapter.month)}
        <section class="chapter" aria-labelledby="chapter-{chapter.month}">
          <header class="sign">
            <h3 id="chapter-{chapter.month}">{chapterTitle(chapter)}</h3>
            <p>{monthName(chapter.month)}{#if chapter.dinners > 0}{` · ${plural(chapter.dinners, 'dinner')}`}{/if}</p>
          </header>
          <ol class="moments">
            {#each chapter.moments as moment, index (`${moment.kind}-${moment.at}-${index}`)}
              {@const told = tell(moment)}
              <li class="moment" class:right={index % 2 === 1} style="--delay: {Math.min(index, 8) * 60}ms">
                <span class="plate" aria-hidden="true">{told.emoji}</span>
                <article class="card" aria-label={captionText(told.parts)}>
                  <p class="caption">
                    {#each told.parts as part, partIndex (partIndex)}
                      {#if typeof part === 'string'}{part}{:else if part.recipeId}<a href="/recipes/{part.recipeId}">{part.dish}</a>{:else}<strong>{part.dish}</strong>{/if}
                    {/each}
                  </p>
                  {#if moment.kind !== 'FavouriteCooked' && moment.favouredBy?.length}
                    <p class="favoured">♥ {favouriteOf(moment.favouredBy)}</p>
                  {/if}
                  {#if moment.review?.notes}<blockquote class="said">{moment.review.notes}</blockquote>{/if}
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
                  <p class="when muted"><time datetime={moment.at} title={fullDate(dayOf(moment), timed(moment) ? moment.at : null)}>{shortDate(dayOf(moment))}</time></p>
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

  /* The noodle: one SVG path down the page, drawn and nudged by the mouse in lib/noodle.js, in the pasta colour for
     both themes. Its glow is the same path again, stroked with a gradient that follows the pointer; hidden until then. */
  .timeline { position: relative; padding: .5rem 0 1rem; --noodle-x: 50%; }
  .noodle { position: absolute; top: 0; left: var(--noodle-x); width: 80px; height: 100%; translate: -50% 0; z-index: 0;
    pointer-events: none; --glow: color-mix(in oklab, var(--area-history) 45%, var(--noodle)); }
  .noodle path { fill: none; stroke-width: 6; stroke-linecap: round; stroke-linejoin: round; }
  .pasta { stroke: var(--noodle); }
  .glow { stroke: url(#noodle-glow); opacity: 0; }
  .noodle stop { stop-color: var(--glow); }

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
  .moment { position: relative; display: grid; grid-template-columns: 1fr 6.5rem 1fr; align-items: center;
    animation: wobble .5s ease-out both; animation-delay: var(--delay); }
  /* A strand of spaghetti from the noodle under the plate to the card, so no card floats. It starts at the plate's
     centre and tucks under the card's edge; both cover its ends. */
  .moment::before { content: ''; position: absolute; top: 50%; right: 50%; width: 4.5rem; height: 12px; translate: 0 -50%;
    z-index: -1; background: var(--noodle);
    mask: url("/strand.svg") repeat-x left center / 24px 12px; }
  .moment.right::before { right: auto; left: 50%; }
  .moment .plate { position: relative; grid-column: 2; grid-row: 1; }
  .moment .card { grid-column: 1; grid-row: 1; margin: 0; }
  .moment.right .card { grid-column: 3; }
  .plate { display: inline-grid; place-items: center; width: 2.6rem; height: 2.6rem; justify-self: center; font-size: 1.3rem;
    border-radius: 50%; background: var(--panel); border: 3px double var(--line); box-shadow: 0 2px 6px var(--shadow);
    transition: rotate .2s; }
  .moment:hover .plate { rotate: -12deg; }
  .caption { margin: 0; }
  .caption strong { font-weight: 600; }
  .when { margin: .3rem 0 0; font-size: .8rem; }
  .favoured { margin: .3rem 0 0; color: var(--bad); font-size: .85rem; }
  /* A review's notes, in the reviewer's own words. */
  .said { margin: .35rem 0 0; padding-left: .6rem; border-left: 3px solid var(--line); font-style: italic; white-space: pre-line;
    overflow-wrap: anywhere; }
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
  /* Mouse reactions: the noodle warms where the pointer is and gives way to it (lib/noodle.js), a hovered moment's
     strand takes the same tint and its card lifts a little, a click on a plate sprinkles, and a month's sign swings on
     its hook. Nothing loops. */
  @media (prefers-reduced-motion: no-preference) {
    .glow { transition: opacity .4s; }
    .timeline:global([data-pointer]) .glow { opacity: .7; }
    .moment::before { transition: background-color .25s; }
    .moment:is(:hover, :focus-within)::before { background: color-mix(in oklab, var(--area-history) 45%, var(--noodle)); }
    .moment .card { transition: translate .2s, box-shadow .2s; }
    .moment:is(:hover, :focus-within) .card { translate: 0 -2px; box-shadow: 0 6px 16px var(--shadow); }
    .plate { cursor: pointer; }
    .sign:hover { animation: swing 1.1s ease-out; transform-origin: top center; }
  }
  @keyframes swing {
    0% { transform: rotate(0deg); }
    20% { transform: rotate(5deg); }
    45% { transform: rotate(-3.5deg); }
    70% { transform: rotate(1.5deg); }
    100% { transform: rotate(0deg); }
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
    .moment { grid-template-columns: 2.6rem minmax(0, 1fr); gap: 1.6rem; }
    .moment::before, .moment.right::before { right: auto; left: 1.3rem; }
    .moment .plate { grid-column: 1; }
    .moment .card, .moment.right .card { grid-column: 2; }
    .deeper, .the-end { text-align: left; margin-left: 0; }
  }
</style>
