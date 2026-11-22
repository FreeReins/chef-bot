<!-- Settings, AI assistants, admins only: the AI models ChefBot itself uses, such as for reading recipe photos, which
     one does each task, and the sweep that asks for suggestions on every recipe that is missing details. Saved with a
     button, like the to-do apps, since API keys are typed in once and never shown again. -->
<script>
  import { run, toast, ask, loadAiFeatures } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { plural } from '../lib/format.js';
  import Plus from '@lucide/svelte/icons/plus';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import Sparkles from '@lucide/svelte/icons/sparkles';

  /** Starting points for a new model. They only fill in the fields; anything can be changed after. */
  const PRESETS = [
    { key: 'claude', label: 'Claude (Anthropic)', name: 'Claude', provider: 'Anthropic', model: 'claude-opus-5-5', endpoint: '' },
    { key: 'openai', label: 'OpenAI', name: 'OpenAI', provider: 'OpenAiCompatible', model: '', endpoint: 'https://api.openai.com/v1' },
    { key: 'ollama', label: 'Ollama on your network', name: 'Ollama', provider: 'OpenAiCompatible', model: 'qwen2.5vl', endpoint: 'http://ollama.local:11434/v1' },
    { key: 'other', label: 'Another OpenAI-compatible service', name: '', provider: 'OpenAiCompatible', model: '', endpoint: '' },
  ];

  const PROVIDERS = [
    ['Anthropic', 'Anthropic (Claude)'],
    ['OpenAiCompatible', 'OpenAI-compatible (OpenAI, Ollama, LM Studio…)'],
  ];

  let settings = $state(null);
  let models = $state([]);
  let defaultModel = $state('');
  let routes = $state({});
  let saving = $state(false);
  let preset = $state('claude');
  let sweep = $state(null);

  const draftsOf = (saved) => saved.models.map((model) => ({ ...model, apiKey: '' }));
  const routesOf = (saved) => Object.fromEntries(saved.tasks.map((task) => [task.task, task.model ?? '']));
  const snapshot = () => JSON.stringify([models, defaultModel, routes]);
  let saved = $state('');
  const changed = $derived(settings !== null && snapshot() !== saved);

  function show(response) {
    settings = response;
    models = draftsOf(response);
    defaultModel = response.defaultModel ?? '';
    routes = routesOf(response);
    saved = snapshot();
  }

  const loadSweep = async () => { sweep = await api('GET', '/api/ai/sweeps/recipe-details'); };

  run(async () => {
    show(await api('GET', '/api/ai/settings'));
    await loadSweep();
  });

  function addModel() {
    const start = PRESETS.find((option) => option.key === preset);
    const taken = new Set(models.map((model) => model.name.toLowerCase()));
    let name = start.name;
    for (let n = 2; name && taken.has(name.toLowerCase()); n++) name = `${start.name} ${n}`;
    models.push({ id: null, name, provider: start.provider, model: start.model, endpoint: start.endpoint, hasApiKey: false, apiKey: '' });
    if (!defaultModel) defaultModel = name;
  }

  function remove(index) {
    const [gone] = models.splice(index, 1);
    if (gone.name === defaultModel) defaultModel = models[0]?.name ?? '';
    for (const task of Object.keys(routes)) if (routes[task] === gone.name) routes[task] = '';
  }

  async function save(event) {
    event.preventDefault();
    saving = true;
    await run(async () => {
      show(await api('PUT', '/api/ai/settings', {
        // An empty key field keeps the saved key.
        models: models.map(({ id, name, provider, model, endpoint, apiKey }) => ({ id, name, provider, model, endpoint, apiKey: apiKey.trim() || null })),
        defaultModel: defaultModel || null,
        tasks: Object.entries(routes).map(([task, model]) => ({ task, model: model || null })),
      }));
      await Promise.all([loadAiFeatures(), loadSweep()]);
      toast('AI models saved.');
    });
    saving = false;
  }

  const providerName = (provider) => (provider === 'Anthropic' ? 'Anthropic' : 'OpenAI-compatible');

  /** The model a task uses in the draft: its own choice, else the default (fallback). */
  function usedBy(task) {
    const chosen = routes[task];
    if (chosen && models.some((model) => model.name === chosen)) return { name: chosen, fallback: false };
    return { name: defaultModel || null, fallback: true };
  }

  /** The labels of the tasks a model does in the draft. */
  const tasksFor = (model) => (model.name ? settings.tasks.filter((task) => usedBy(task.task).name === model.name).map((task) => task.label) : []);

  /** Whether a model is new or differs from what is saved. */
  function modelChanged(model) {
    const before = settings.models.find((other) => other.id === model.id);
    if (!before) return true;
    return model.apiKey.trim() !== '' || ['name', 'provider', 'model', 'endpoint'].some((field) => (model[field] ?? '') !== (before[field] ?? ''));
  }

  const test = (model) => run(async () => {
    const result = await api('POST', `/api/ai/models/${model.id}/test`);
    if (result.worked) toast(`${model.name} answered. It's ready to use.`);
    else toast(`${model.name} didn't answer. ${result.problem}`, 'error');
  });

  async function startSweep() {
    if (!(await ask({
      title: `Ask ${sweep.modelName} about ${plural(sweep.sparse, 'recipe')}?`,
      message: `Each recipe that is missing details is sent to ${sweep.modelName}, one at a time in the background. `
        + 'This may cost money with a paid service. Nothing changes until someone applies the suggestions on a recipe.',
      confirmLabel: 'Ask for suggestions',
    }))) return;
    return run(async () => {
      const started = await api('POST', '/api/ai/sweeps/recipe-details');
      toast(`Queued ${plural(started.queued, 'recipe')}. Suggestions appear on each recipe as they're ready.`);
      await loadSweep();
    });
  }
</script>

<p class="muted">
  ChefBot can use an AI model to read recipes from photos and to suggest what a recipe is missing. One model is the
  default; any task can use a different one, such as a local model for photos. A recipe is sent to a model only when
  someone asks.
</p>

{#if settings}
  <div class="card glance">
    <h4>At a glance</h4>
    <dl>
      {#each settings.tasks as task (task.task)}
        {@const used = usedBy(task.task)}
        <div>
          <dt>{task.label}</dt>
          <dd>
            {#if used.name}
              <strong>{used.name}</strong>
              {#if used.fallback}<span class="chip">Default</span>{/if}
            {:else}
              <span class="muted">Not set up</span>
            {/if}
          </dd>
        </div>
      {/each}
    </dl>
  </div>

  <form class="stack" onsubmit={save}>
    <h4>Models</h4>
    {#each models as model, index (model.id ?? `new-${index}`)}
      <details class="card model" open={model.id === null}>
        <summary>
          <span class="title">
            <strong>{model.name || 'New model'}</strong>
            <span class="muted">{providerName(model.provider)}{model.model ? ` · ${model.model}` : ''}</span>
          </span>
          <span class="chips">
            {#if model.name && model.name === defaultModel}<span class="chip info">Default</span>{/if}
            {#if model.provider === 'Anthropic'}
              {#if model.hasApiKey || model.apiKey.trim()}<span class="chip good">Key saved</span>{:else}<span class="chip bad">No key</span>{/if}
            {/if}
            {#each tasksFor(model) as label (label)}<span class="chip">{label}</span>{/each}
            {#if modelChanged(model)}<span class="chip warn">Not saved</span>{/if}
          </span>
        </summary>
        <div class="body">
          <div class="fields">
            <label>Name <input bind:value={model.name} required maxlength="60" placeholder="Claude"></label>
            <label>Talks to
              <select bind:value={model.provider}>
                {#each PROVIDERS as [provider, title] (provider)}<option value={provider}>{title}</option>{/each}
              </select>
            </label>
            <label>Model <input bind:value={model.model} required autocomplete="off"
                                placeholder={model.provider === 'Anthropic' ? 'claude-opus-5-5' : 'qwen2.5vl'}></label>
            <label class="wide">{model.provider === 'Anthropic' ? 'Address (empty for Anthropic\'s own)' : 'Address, ending in /v1'}
              <input type="url" bind:value={model.endpoint} required={model.provider !== 'Anthropic'}
                     placeholder={model.provider === 'Anthropic' ? 'https://api.anthropic.com' : 'http://ollama.local:11434/v1'}>
            </label>
            <label class="wide">{model.provider === 'Anthropic' ? 'API key' : 'API key (if the service needs one)'}
              <input type="password" autocomplete="off" bind:value={model.apiKey} required={model.provider === 'Anthropic' && !model.hasApiKey}
                     placeholder={model.hasApiKey ? 'Saved. Type a new one to replace it' : ''}>
            </label>
            <label class="default"><input type="radio" name="default-model" value={model.name} bind:group={defaultModel}> Default model</label>
          </div>
          <p class="muted hint">
            {#if model.provider === 'Anthropic'}
              Make an API key in the Claude Console, under API keys.
            {:else}
              For Ollama, pull a model that can see images, such as <code>qwen2.5vl</code> or <code>llama3.2-vision</code>,
              for reading photos.
            {/if}
          </p>
          <div class="row-actions">
            {#if model.id}
              <button type="button" class="small" onclick={pendingClick(() => test(model))} disabled={changed}
                      title={changed ? 'Save your changes first' : 'Asks a short question to check the connection'}>Test</button>
            {/if}
            <button type="button" class="small with-icon" onclick={() => remove(index)}
                    aria-label="Remove {model.name || 'this model'}"><Trash2 size={14} />Remove</button>
          </div>
        </div>
      </details>
    {:else}
      <p class="card muted">No AI models yet. Add one to read recipes from photos and complete recipes.</p>
    {/each}

    <div class="row-actions add">
      <label>Start from
        <select bind:value={preset}>
          {#each PRESETS as option (option.key)}<option value={option.key}>{option.label}</option>{/each}
        </select>
      </label>
      <button type="button" class="with-icon" onclick={addModel}><Plus size={16} />Add a model</button>
    </div>

    {#if models.length > 1}
      <fieldset class="card">
        <legend>Which model does each task</legend>
        <div class="fields">
          {#each settings.tasks as task (task.task)}
            <label>{task.label}
              <select bind:value={routes[task.task]}>
                <option value="">The default model{defaultModel ? ` (${defaultModel})` : ''}</option>
                {#each models.filter((model) => model.name) as model (model.name)}<option value={model.name}>{model.name}</option>{/each}
              </select>
              <span class="muted hint">{task.description}</span>
            </label>
          {/each}
        </div>
      </fieldset>
    {/if}

    <div class="row-actions">
      <button type="submit" class="primary" disabled={!changed || saving}>{saving ? 'Saving…' : 'Save models'}</button>
      {#if changed}<span class="muted unsaved">{plural(models.length, 'model')}, not saved yet</span>{/if}
    </div>
  </form>

  {#if sweep?.modelName}
    <h4>Recipe tools</h4>
    <div class="card stack sweep">
      <h5 class="with-icon"><Sparkles size={16} />Complete sparse recipes</h5>
      {#if sweep.sparse > 0}
        <p>{plural(sweep.sparse, 'recipe')} {sweep.sparse === 1 ? 'is' : 'are'} missing details, such as times, tags, amounts or clear steps.
          {sweep.modelName} can suggest what to add; you choose what to keep on each recipe.</p>
        <div class="row-actions"><button type="button" onclick={pendingClick(startSweep)}>Ask for suggestions</button></div>
      {:else}
        <p class="muted">Every recipe has its details, or already has suggestions.</p>
      {/if}
      {#if sweep.waiting > 0}
        <p class="muted">{plural(sweep.waiting, 'recipe')} {sweep.waiting === 1 ? 'has' : 'have'} suggestions waiting. Find them on the
          Recipes tab with the Has suggestions filter.</p>
      {/if}
    </div>
  {/if}
{/if}

<style>
  h4 { margin: 1.25rem 0 .5rem; font-size: 1rem; }
  .glance h4 { margin: 0 0 .5rem; }
  .glance dl { margin: 0; display: grid; gap: .4rem; }
  .glance dl div { display: flex; justify-content: space-between; align-items: center; gap: .5rem 1rem; flex-wrap: wrap; }
  .glance dt { color: var(--muted); }
  .glance dd { margin: 0; display: flex; align-items: center; gap: .4rem; }
  .model { border: 1px solid var(--line); }
  .model summary { cursor: pointer; display: flex; justify-content: space-between; align-items: center; gap: .5rem 1rem; flex-wrap: wrap; }
  .model summary .title { display: flex; flex-direction: column; }
  .model summary .chips { margin: 0; }
  .model .body { margin-top: .75rem; padding-top: .75rem; border-top: 1px solid var(--line); }
  legend { font-weight: 600; padding: 0 .25rem; }
  .fields { display: grid; gap: .5rem .75rem; grid-template-columns: repeat(auto-fit, minmax(min(14rem, 100%), 1fr)); }
  .fields .wide { grid-column: 1 / -1; }
  .default { display: flex; gap: .4rem; align-items: center; }
  .hint { margin: .5rem 0; display: block; font-size: .9rem; }
  .unsaved { align-self: center; }
  .add { align-items: end; }
  .add label { display: grid; gap: .25rem; }
  .sweep h5 { margin: 0; font-size: 1rem; display: flex; align-items: center; gap: .4rem; }
  .sweep p { margin: 0; }
</style>
