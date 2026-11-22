<!-- Settings, Planning: how many people plans are for, when next week is planned by itself, and how long leftovers
     keep. Anyone signed in can change them, and changes save by themselves. Weeks not yet confirmed follow a new number
     of people; confirmed weeks keep theirs. -->
<script>
  import { app, savePlanning } from '../lib/app.svelte.js';
  import { autosave } from '../lib/autosave.svelte.js';
  import { DAYS, fullDate } from '../lib/format.js';
  import SaveStatus from '../components/SaveStatus.svelte';

  const settings = $derived(app.planning);

  let form;

  // Saved shortly after each change; a number is changed once it is left or Enter is pressed.
  const saver = autosave(async () => {
    const data = new FormData(form);
    await savePlanning({
      people: Number(data.get('people')),
      scheduleEnabled: data.get('scheduleEnabled') === 'on',
      scheduleDay: data.get('scheduleDay') ?? settings.scheduleDay,
      scheduleTime: data.get('scheduleTime') ?? settings.scheduleTime,
      maxLeftoverDays: Number(data.get('maxLeftoverDays')),
    });
  }, { validate: () => form.checkValidity() });

  let scheduleOn = $state(app.planning.scheduleEnabled);

  function submit(event) {
    event.preventDefault();
    saver.retry();
  }
</script>

<h2>Planning</h2>
<form class="stack card" bind:this={form} onchange={saver.schedule} onsubmit={submit}>
  <label>People you usually cook for
    <input id="planning-people" name="people" type="number" min="1" max="20" step="1" required value={settings.people}
           aria-describedby="planning-people-about">
  </label>
  <p id="planning-people-about" class="muted note">
    Each breakfast, lunch and dinner is planned for this many. Weeks you haven't confirmed yet are resized, with
    their shopping lists; confirmed weeks keep the number they were planned for.
  </p>

  <label class="check">
    <input id="planning-schedule" name="scheduleEnabled" type="checkbox" class="switch" bind:checked={scheduleOn}>
    <span>Plan next week automatically
      <span class="muted" style="display:block">Stages next week in your default plan style, ready to review.</span>
    </span>
  </label>
  {#if scheduleOn}
    <div class="row when">
      <label>On
        <select id="planning-day" name="scheduleDay" value={settings.scheduleDay}>
          {#each DAYS as day (day)}<option value={day}>{day}</option>{/each}
        </select>
      </label>
      <label>At
        <input id="planning-time" name="scheduleTime" type="time" required value={settings.scheduleTime}>
      </label>
    </div>
    {#if settings.nextStaging}<p class="muted note">Next: {fullDate(null, settings.nextStaging)}.</p>{/if}
  {/if}

  <label>Leftovers keep for (days)
    <input id="planning-leftovers" name="maxLeftoverDays" type="number" min="0" max="7" step="1" required
           value={settings.maxLeftoverDays} aria-describedby="planning-leftovers-about">
  </label>
  <p id="planning-leftovers-about" class="muted note">
    A weekday lunch uses the leftovers of a dinner cooked this recently. 0 plans no leftover lunches.
  </p>

  <SaveStatus {saver} invalid="Not saved yet: plan for 1 to 20 people, and keep leftovers for 0 to 7 days." />
</form>

<style>
  .note { margin: -.25rem 0 .25rem; font-size: .85rem; }
  input[type='number'] { max-width: 6rem; }
  .when { gap: .75rem; flex-wrap: wrap; }
</style>
