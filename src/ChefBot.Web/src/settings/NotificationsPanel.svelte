<script>
  import { app, run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { autosave } from '../lib/autosave.svelte.js';
  import SaveStatus from '../components/SaveStatus.svelte';

  const REMINDERS = [
    ['planReady', 'A new plan is ready', 'Sent whenever a week is staged or regenerated.'],
    ['slowCooker', 'Start the slow cooker', 'On the morning of a slow cooker dinner.'],
    ['expiringFood', 'Food to use soon', 'When pantry food is about to expire, noting what no meal uses.'],
    ['shopping', 'Confirm the plan or finish shopping', 'The day before a week starts and on its first day.'],
    ['mealsToday', "Today's meals", 'A daily summary of what is planned.'],
  ];

  const settings = $derived(app.notifications);

  let form;

  // Saved shortly after each change; a field is changed once it is left or Enter is pressed.
  const saver = autosave(async () => {
    const data = new FormData(form);
    const changes = { webhookUrl: data.get('webhookUrl'), dailyTime: data.get('dailyTime') };
    for (const [key] of REMINDERS) changes[key] = data.get(key) === 'on';
    app.notifications = await api('PUT', '/api/system/notifications', { ...settings, ...changes });
  }, { validate: () => form.checkValidity() });

  function submit(event) {
    event.preventDefault();
    saver.retry();
  }

  const send = (path, describe) => run(async () => {
    toast(describe(await api('POST', path)));
  });

  const describeTest = (result) => (result.delivered ? 'The test notification was delivered.' : `The test wasn't delivered. ${result.problem}`);
  const describeRun = (result) => [
    result.sent.length ? `Sent: ${result.sent.join(', ')}.` : 'Nothing needed sending today.',
    result.problems.length ? `Not delivered: ${result.problems.join('; ')}` : '',
  ].join(' ').trim();
</script>

<h2>Notifications</h2>
<form class="stack card" bind:this={form} onchange={saver.schedule} onsubmit={submit}>
  <p class="muted">ChefBot posts reminders as JSON to a webhook, such as a Home Assistant webhook, which can turn them into phone notifications or announcements.</p>
  <label>Webhook address (leave empty to turn notifications off)
    <input id="notify-url" name="webhookUrl" type="url" value={settings.webhookUrl} placeholder="http://homeassistant.local:8123/api/webhook/chefbot">
  </label>
  <label>Send daily reminders at
    <input id="notify-time" name="dailyTime" type="time" required value={settings.dailyTime}>
  </label>
  {#each REMINDERS as [key, title, detail] (key)}
    <label class="check">
      <input id="notify-{key}" name={key} type="checkbox" class="switch" checked={settings[key]}>
      <span>{title}<span class="muted" style="display:block">{detail}</span></span>
    </label>
  {/each}
  <SaveStatus {saver} />
  <div class="row-actions">
    <button type="button" onclick={() => send('/api/system/notifications/test', describeTest)}>Send a test</button>
    <button type="button" onclick={() => send('/api/system/notifications/send-today', describeRun)}>Send today's reminders now</button>
  </div>
</form>
