<!-- Setup step: how many people to plan for, and how weeks are planned. Every starter style is offered and kept, so another can be picked for
     any week; the one chosen here is the default. They can be changed, removed or added to here or later under
     Settings, Plan styles. -->
<script>
  import { app, run, saveStyles, savePlanning, moveSetup } from '../lib/app.svelte.js';
  import { ruleText, styleSummary } from '../lib/styles.js';
  import SetupNav from '../components/SetupNav.svelte';
  import PlanStyleEditor from '../components/PlanStyleEditor.svelte';

  // Kept in app state rather than here, so going back a step and returning keeps the choices made so far.
  const styles = $derived(app.styleDraft ?? []);
  const chosen = $derived(styles.find((style) => style.isDefault) ?? styles[0]);

  function choose(style) {
    for (const other of app.styleDraft) other.isDefault = other === style;
  }

  const people = $derived(app.peopleDraft);
  const peopleValid = $derived(Number.isInteger(people) && people >= 1 && people <= 20);

  function next() {
    return run(async () => {
      if (!peopleValid) throw new Error('Enter how many people you usually cook for, from 1 to 20.');
      if (people !== app.planning.people || !app.planning.isSaved) await savePlanning({ people });
      if (styles.length || app.styles?.styles.length) await saveStyles($state.snapshot(styles));
      app.styleDraft = null;
      app.peopleDraft = null;
      await moveSetup(1);
    });
  }
</script>

<section class="gate wide">
  <h2>How do you like to plan a week?</h2>
  <p class="muted">
    A plan style sets the days you cook dinner, what else to plan, and rules for the recipes, such as a slow cooker
    meal or no meat. Choose the one for most weeks. You can pick another for any week, and change them later under
    Settings, Plan styles.
  </p>

  <div class="people card">
    <label>How many people do you usually cook for?
      <input id="setup-people" type="number" min="1" max="20" step="1" required bind:value={app.peopleDraft}
             aria-describedby="setup-people-about">
    </label>
    <p id="setup-people-about" class="muted">
      {#if peopleValid}Each breakfast, lunch and dinner is planned for {people === 1 ? '1 person' : `${people} people`}.{/if}
      You can change this for weeks to come under Settings, Planning.
    </p>
  </div>

  {#if styles.length}
    <fieldset class="choices">
      <legend class="sr-only">Your usual plan style</legend>
      {#each styles as style, index (index)}
        <label class="choice" class:chosen={style === chosen}>
          <input type="radio" name="usual-style" checked={style === chosen} onchange={() => choose(style)}
                 aria-labelledby="style-name-{index}" aria-describedby="style-about-{index}">
          <span class="text">
            <strong id="style-name-{index}">{style.name}</strong>
            <span id="style-about-{index}">
              {#if style.description}<span class="about">{style.description}</span>{/if}
              <span class="muted">{[styleSummary(style), ...style.rules.map(ruleText)].join(' · ')}</span>
            </span>
          </span>
        </label>
      {/each}
    </fieldset>
  {/if}

  <details class="customise card" open={styles.length === 0}>
    <summary>Change these styles, or make your own</summary>
    <PlanStyleEditor bind:styles={app.styleDraft} />
  </details>

  <SetupNav label={styles.length ? 'Next' : 'Skip for now'} primary={styles.length > 0} onnext={next}>
    {#if chosen}Weeks are planned as {chosen.name} unless you choose another.
    {:else}No styles: weeks are planned as the Classic week.{/if}
  </SetupNav>
</section>

<style>
  .people { display: grid; gap: .35rem; margin-bottom: .75rem; }
  .people input { max-width: 6rem; }
  .people p { margin: 0; }
  .choices { border: 0; margin: 0 0 .75rem; padding: 0; display: grid; gap: .5rem; grid-template-columns: repeat(auto-fill, minmax(min(19rem, 100%), 1fr)); }
  .choice { display: flex; gap: .65rem; align-items: flex-start; padding: .75rem .9rem; border: 1px solid var(--line); border-radius: .5rem;
    background: var(--panel); color: var(--ink); font-size: .95rem; cursor: pointer; }
  .choice:hover { border-color: var(--accent); }
  .choice.chosen { border-color: var(--accent); box-shadow: inset 0 0 0 1px var(--accent); }
  .choice input { margin-top: .3rem; }
  .text { display: grid; gap: .15rem; }
  .text .about, .text .muted { display: block; }
  .text .muted { font-size: .8rem; margin-top: .15rem; }
  .customise summary { cursor: pointer; font-weight: 600; }
  .customise[open] summary { margin-bottom: .5rem; }
</style>
