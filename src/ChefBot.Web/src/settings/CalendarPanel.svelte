<!-- Settings, Calendar: the meal plan as a calendar feed to subscribe to. Everyone signed in can copy its address;
     admins turn it on, give it a new address (the old one stops working) or turn it off, and set the meal times. -->
<script>
  import { pendingClick } from '../lib/pending.js';
  import { run, toast, ask, isAdmin } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { autosave } from '../lib/autosave.svelte.js';
  import SaveStatus from '../components/SaveStatus.svelte';
  import { copyText } from '../lib/exporting.js';
  import Copy from '@lucide/svelte/icons/copy';
  import CalendarPlus from '@lucide/svelte/icons/calendar-plus';

  let feed = $state(null);
  let times = $state(null);
  run(async () => {
    [feed, times] = await Promise.all([api('GET', '/api/calendar/feed'), api('GET', '/api/calendar/times')]);
  });

  const MEALS = [['breakfastTime', 'Breakfast'], ['lunchTime', 'Lunch'], ['dinnerTime', 'Dinner']];
  let timesForm = $state();

  // Saved shortly after each change, admins only; a field is changed once it is left or Enter is pressed.
  const saver = autosave(async () => {
    const data = new FormData(timesForm);
    times = await api('PUT', '/api/calendar/times', {
      breakfastTime: data.get('breakfastTime'),
      lunchTime: data.get('lunchTime'),
      dinnerTime: data.get('dinnerTime'),
      mealMinutes: Number(data.get('mealMinutes')),
      slowCookerHours: Number(data.get('slowCookerHours')),
    });
  }, { validate: () => timesForm.checkValidity() });

  function submitTimes(event) {
    event.preventDefault();
    saver.retry();
  }

  // Calendar apps on this device open webcal: links themselves and subscribe in one step.
  const webcal = $derived(feed?.url?.replace(/^https?:/, 'webcal:'));
  const made = $derived(feed?.createdAt ? new Date(feed.createdAt).toLocaleDateString(undefined, { dateStyle: 'medium' }) : null);

  const copy = () => run(async () => {
    await copyText(feed.url);
    toast('The calendar address was copied. Add it in your calendar app as a subscribed calendar.');
  });

  async function start(replacing) {
    if (replacing && !(await ask({
      title: 'Make a new calendar address?',
      message: 'Calendars subscribed to the current address stop updating, and need the new one added instead.',
      confirmLabel: 'Make a new address',
    }))) return;
    return run(async () => {
      feed = await api('POST', '/api/calendar/feed');
      toast(replacing ? 'The calendar has a new address. The old one no longer works.' : 'The calendar feed is on. Copy its address into your calendar app.');
    });
  }

  async function stop() {
    if (!(await ask({ title: 'Turn the calendar feed off?', message: 'Subscribed calendars stop updating, and its address stops working.',
      confirmLabel: 'Turn off', danger: true }))) return;
    return run(async () => {
      feed = await api('DELETE', '/api/calendar/feed');
      toast('The calendar feed is off.');
    });
  }
</script>

<h2>Calendar</h2>
<p class="muted">
  Subscribe to the meal plan in your calendar app to see each confirmed week's breakfasts, lunches and dinners at
  mealtimes, with a link to each recipe and a reminder to start the slow cooker. It updates by itself as the plan
  changes, about once an hour.
</p>

{#if feed}
  <div class="card stack">
    {#if feed.url}
      <label>Calendar address
        <input id="calendar-url" readonly value={feed.url} onfocus={(event) => event.currentTarget.select()}>
      </label>
      <div class="row-actions">
        <button type="button" class="with-icon" onclick={copy}><Copy size={16} />Copy address</button>
        <a class="button with-icon" href={webcal}><CalendarPlus size={16} />Subscribe on this device</a>
      </div>
      <p class="muted">
        Anyone with this address can see the meal plan, so share it only with the household.
        {#if made}It was made on {made}.{/if}
        Calendar apps need to reach ChefBot, so this works on your home network, and with ChefBot's own certificate
        only once the device trusts it (see HTTPS).
      </p>
      {#if isAdmin()}
        <div class="row-actions">
          <button type="button" onclick={pendingClick(() => start(true))}>Make a new address</button>
          <button type="button" class="bad-text" onclick={pendingClick(stop)}>Turn off</button>
        </div>
      {/if}
    {:else if isAdmin()}
      <p>The calendar feed is off.</p>
      <div class="row-actions"><button type="button" class="primary" onclick={pendingClick(() => start(false))}>Turn on the calendar feed</button></div>
    {:else}
      <p class="muted">The calendar feed is off. Ask an admin to turn it on under Settings, Calendar.</p>
    {/if}
  </div>
{/if}

{#if times}
  <h3>Meal times</h3>
  <form class="card stack" bind:this={timesForm} onchange={saver.schedule} onsubmit={submitTimes}>
    <fieldset class="times" disabled={!isAdmin()}>
      <legend class="sr-only">When each meal starts</legend>
      {#each MEALS as [key, title] (key)}
        <label>{title}<input id="calendar-{key}" name={key} type="time" required value={times[key]}></label>
      {/each}
    </fieldset>
    <fieldset class="times" disabled={!isAdmin()}>
      <legend class="sr-only">How long meals last and the slow cooker reminder</legend>
      <label>Each meal lasts (minutes)
        <input id="calendar-minutes" name="mealMinutes" type="number" min="5" max="240" step="5" required value={times.mealMinutes}>
      </label>
      <label>Slow cooker reminder (hours before dinner)
        <input id="calendar-slow-cooker" name="slowCookerHours" type="number" min="0" max="12" step="1" required
               value={times.slowCookerHours} aria-describedby="calendar-slow-cooker-about">
      </label>
    </fieldset>
    <p id="calendar-slow-cooker-about" class="muted">
      0 hours turns the slow cooker reminder off. Subscribed calendars show new times when they next update.
    </p>
    {#if isAdmin()}
      <SaveStatus {saver} invalid="Not saved yet: give each meal a time, an event of 5 to 240 minutes, and a reminder of 0 to 12 hours." />
    {:else}
      <p class="muted">Ask an admin to change the meal times.</p>
    {/if}
  </form>
{/if}

<style>
  .times { border: 0; margin: 0; padding: 0; display: grid; gap: .5rem .75rem; grid-template-columns: repeat(auto-fill, minmax(min(11rem, 100%), 1fr)); }
  .times input[type='number'] { max-width: 6rem; }
  #calendar-url { font-family: ui-monospace, monospace; font-size: .85rem; }
</style>
