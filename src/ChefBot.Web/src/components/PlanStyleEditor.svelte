<!-- The household's plan styles: add starter ones or your own, choose the default, and set each one's dinner days,
     other meals and rules. It edits a list the parent owns (bind:styles) and saves nothing itself. Used by the plan
     style setup step and by Settings, Plan styles. -->
<script>
  import { app } from '../lib/app.svelte.js';
  import { DAYS } from '../lib/format.js';
  import { LIMITS, RULE_SLOTS, matchesFor, FAVOURITE_LEANS, newRule, newStyle, copyStyle, ruleText, styleSummary } from '../lib/styles.js';
  import Chip from './Chip.svelte';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import Plus from '@lucide/svelte/icons/plus';
  import Pencil from '@lucide/svelte/icons/pencil';
  import Check from '@lucide/svelte/icons/check';

  /** styles: PlanStyleDetails[], changed in place. */
  let { styles = $bindable() } = $props();

  const COUNTS = [['breakfasts', 'Breakfasts', 5], ['snacks', 'Snacks', 7], ['sweets', 'Sweets', 7], ['drinks', 'Drinks', 7]];

  let editing = $state(null); // index of the style whose details are open
  let ownName = $state('');

  const known = $derived(app.styles?.known ?? []);
  const has = (name) => styles.some((style) => style.name.trim().toLowerCase() === name.trim().toLowerCase());

  function add(style) {
    styles.push({ ...copyStyle(style), id: null, isDefault: styles.length === 0 });
  }

  function addOwn(event) {
    event.preventDefault();
    const name = ownName.trim();
    if (!name || has(name)) return;
    styles.push({ ...newStyle(name), isDefault: styles.length === 0 });
    ownName = '';
    editing = styles.length - 1;
  }

  function remove(index) {
    const [gone] = styles.splice(index, 1);
    if (gone.isDefault && styles.length) styles[0].isDefault = true;
    editing = null;
  }

  function makeDefault(style) {
    for (const other of styles) other.isDefault = other === style;
  }

  function setDay(style, day, on) {
    style.cookingDays = DAYS.filter((other) => (other === day ? on : style.cookingDays.includes(other)));
  }

  // A rule for every kind of meal can only say "no"; a day only suits "at least" and "exactly" dinner rules.
  function setLimit(rule, limit) {
    rule.limit = limit;
    if (limit !== 'None' && !rule.slot) rule.slot = 'Dinner';
    if (limit === 'None' || limit === 'AtMost') rule.day = null;
  }

  function setSlot(rule, slot) {
    rule.slot = slot || null;
    if (!slot) rule.limit = 'None';
    if (slot !== 'Dinner') rule.day = null;
  }

  const cookbooks = $derived(app.styles?.cookbooks ?? []);

  function setMatch(rule, match) {
    // Tags and ingredients are words; a cookbook rule holds a cookbook's id. Neither carries over to the other.
    if ((match === 'Cookbook') !== (rule.match === 'Cookbook')) rule.values = match === 'Cookbook' && cookbooks.length ? [cookbooks[0].id] : [];
    rule.match = match;
    if (match === 'ReadyWithin' && !rule.minutes) rule.minutes = 30;
  }

  const setValues = (rule, text) => { rule.values = text.split(',').map((value) => value.trim()).filter(Boolean); };
</script>

<datalist id="style-tags">
  {#each app.styles?.tags ?? [] as tag (tag)}<option value={tag}></option>{/each}
</datalist>
<datalist id="style-ingredients">
  {#each app.styles?.ingredients ?? [] as name (name)}<option value={name}></option>{/each}
</datalist>

<h3>Your plan styles</h3>
{#if styles.length === 0}
  <p class="muted">No styles yet. Until you add one, weeks are planned in the Classic week style.</p>
{:else}
  <ul class="plain">
    {#each styles as style, index}
      <li>
        <div class="row">
          <div>
            <strong>{style.name || 'New style'}</strong>
            {#if style.isDefault}<Chip tone="good">Default</Chip>{/if}
            <p class="muted summary">{[styleSummary(style), ...style.rules.map((rule) => ruleText(rule, cookbooks))].join(' · ')}</p>
          </div>
          <div class="row-actions">
            {#if !style.isDefault}
              <button type="button" class="small" onclick={() => makeDefault(style)}>Make default</button>
            {/if}
            <button type="button" class="small with-icon" aria-expanded={String(editing === index)} aria-label="{editing === index ? 'Done editing' : 'Edit'} {style.name}"
                    onclick={() => (editing = editing === index ? null : index)}>
              {#if editing === index}<Check size={16} />Done{:else}<Pencil size={16} />Edit{/if}
            </button>
            <button type="button" class="icon remove" aria-label="Remove {style.name}" title="Remove" onclick={() => remove(index)}><Trash2 size={16} /></button>
          </div>
        </div>

        {#if editing === index}
          <div class="details">
            <div class="two">
              <label>Name <input bind:value={style.name} maxlength="60"></label>
              <label>What it is for <input bind:value={style.description} maxlength="200" placeholder="Busy weeks with quick dinners"></label>
            </div>

            <fieldset>
              <legend>Days you cook dinner</legend>
              <div class="days">
                {#each DAYS as day (day)}
                  <label class="day">
                    <input type="checkbox" checked={style.cookingDays.includes(day)} onchange={(event) => setDay(style, day, event.currentTarget.checked)}>
                    {day.slice(0, 3)}
                  </label>
                {/each}
              </div>
            </fieldset>
            <label class="check">
              <input type="checkbox" bind:checked={style.leftoverLunches}>
              <span>Leftovers for weekday lunches
                <span class="muted">Each dinner is cooked big enough for the next days' lunches.</span>
              </span>
            </label>

            <fieldset>
              <legend>Also plan</legend>
              <div class="counts">
                {#each COUNTS as [key, label, max] (key)}
                  <label>{label} <input type="number" min="0" {max} step="1" bind:value={style[key]}></label>
                {/each}
              </div>
            </fieldset>

            <fieldset>
              <legend>Lean on favourites</legend>
              <div class="leans">
                {#each FAVOURITE_LEANS as [key, label] (key)}
                  <label class="lean"><input type="radio" name="favourites-{index}" value={key} bind:group={style.favourites}>{label}</label>
                {/each}
              </div>
              <p class="muted hint">{FAVOURITE_LEANS.find(([key]) => key === (style.favourites ?? 'ALittle'))?.[2]}
                A recipe more people love counts for more. Meals planned recently are still avoided first.</p>
            </fieldset>

            <fieldset>
              <legend>Rules for the recipes</legend>
              {#each style.rules as rule, ruleIndex (ruleIndex)}
                <div class="rule" role="group" aria-label="Rule {ruleIndex + 1}">
                  <select aria-label="Rule {ruleIndex + 1}: how many" value={rule.limit} onchange={(event) => setLimit(rule, event.currentTarget.value)}>
                    {#each LIMITS as [key, label] (key)}<option value={key}>{label}</option>{/each}
                  </select>
                  {#if rule.limit !== 'None'}
                    <input type="number" min="0" max="7" step="1" aria-label="Rule {ruleIndex + 1}: number of meals" bind:value={rule.count}>
                  {/if}
                  <select aria-label="Rule {ruleIndex + 1}: kind of meal" value={rule.slot ?? ''} onchange={(event) => setSlot(rule, event.currentTarget.value)}>
                    {#each RULE_SLOTS as [key, label] (key)}<option value={key}>{label}</option>{/each}
                  </select>
                  <select aria-label="Rule {ruleIndex + 1}: what it looks for" value={rule.match} onchange={(event) => setMatch(rule, event.currentTarget.value)}>
                    {#each matchesFor(rule, cookbooks) as [key, label] (key)}<option value={key}>{label}</option>{/each}
                  </select>
                  {#if rule.match === 'Tag' || rule.match === 'Ingredient'}
                    <input class="values" list={rule.match === 'Tag' ? 'style-tags' : 'style-ingredients'}
                           aria-label="Rule {ruleIndex + 1}: {rule.match === 'Tag' ? 'tags' : 'ingredients'}, separated by commas"
                           placeholder={rule.match === 'Tag' ? 'quick, family favorite' : 'chicken, beef'}
                           value={(rule.values ?? []).join(', ')} onchange={(event) => setValues(rule, event.currentTarget.value)}>
                  {:else if rule.match === 'Cookbook'}
                    <select aria-label="Rule {ruleIndex + 1}: cookbook" value={rule.values?.[0] ?? ''} onchange={(event) => (rule.values = [event.currentTarget.value])}>
                      {#if !cookbooks.some((cookbook) => cookbook.id === rule.values?.[0])}<option value="" disabled>Choose a cookbook</option>{/if}
                      {#each cookbooks as cookbook (cookbook.id)}<option value={cookbook.id}>{cookbook.name}</option>{/each}
                    </select>
                  {:else if rule.match === 'ReadyWithin'}
                    <span class="minutes">
                      <input type="number" min="1" max="600" step="5" aria-label="Rule {ruleIndex + 1}: minutes" bind:value={rule.minutes}> minutes
                    </span>
                  {/if}
                  {#if rule.slot === 'Dinner' && (rule.limit === 'AtLeast' || rule.limit === 'Exactly')}
                    <select aria-label="Rule {ruleIndex + 1}: day" value={rule.day ?? ''} onchange={(event) => (rule.day = event.currentTarget.value || null)}>
                      <option value="">on any day</option>
                      {#each DAYS as day (day)}<option value={day}>on {day}</option>{/each}
                    </select>
                  {/if}
                  <button type="button" class="icon remove" aria-label="Remove rule {ruleIndex + 1}" title="Remove rule"
                          onclick={() => style.rules.splice(ruleIndex, 1)}><Trash2 size={16} /></button>
                </div>
              {:else}
                <p class="muted">No rules: any recipe of the right course can be chosen.</p>
              {/each}
              <button type="button" class="small with-icon" onclick={() => style.rules.push(newRule())}><Plus size={16} />Add a rule</button>
              <p class="muted hint">
                For example: at least 3 dinners slow cooker; no meals made with beef, pork; at least 2 dinners made with
                chicken; at least 4 dinners ready within 30 minutes. A rule your recipes can't meet is relaxed for that
                week, and the plan says so.
              </p>
            </fieldset>
          </div>
        {/if}
      </li>
    {/each}
  </ul>
{/if}

<h3 class="spaced">Add a starter style</h3>
<div class="known">
  {#each known as style (style.name)}
    <div class="known-style">
      <span class="known-text">
        <strong>{style.name}</strong>
        <span class="muted">{style.description}</span>
      </span>
      <button type="button" class="small with-icon" disabled={has(style.name)} aria-label="{has(style.name) ? 'Added' : 'Add'} {style.name}"
              onclick={() => add(style)}>{#if has(style.name)}<Check size={16} />Added{:else}<Plus size={16} />Add{/if}</button>
    </div>
  {/each}
</div>

<form class="inline spaced" onsubmit={addOwn}>
  <label>Or make your own
    <input bind:value={ownName} placeholder="Style name" maxlength="60">
  </label>
  <button type="submit" class="with-icon" disabled={!ownName.trim() || has(ownName)}><Plus size={16} />Add style</button>
</form>

<style>
  .spaced { margin-top: 1.25rem; }
  .summary { font-size: .85rem; overflow-wrap: anywhere; }
  .details { display: grid; gap: .75rem; margin: .5rem 0 .25rem; padding: .7rem .8rem; background: var(--bg); border-radius: .4rem; }
  .two { display: grid; gap: .5rem; grid-template-columns: repeat(auto-fit, minmax(min(14rem, 100%), 1fr)); }
  fieldset { border: 0; margin: 0; padding: 0; display: grid; gap: .4rem; }
  legend { padding: 0; margin-bottom: .3rem; font-size: .85rem; color: var(--muted); }
  .days { display: flex; flex-wrap: wrap; gap: .25rem .75rem; }
  .day { display: flex; gap: .3rem; align-items: center; color: var(--ink); font-size: .9rem; }
  .check { border-top: 0; padding: 0; }
  .check .muted { display: block; }
  .counts { display: flex; flex-wrap: wrap; gap: .5rem 1rem; }
  .counts input { width: 4.5rem; }
  .rule { display: flex; flex-wrap: wrap; gap: .35rem; align-items: center; padding: .4rem 0; border-top: 1px solid var(--line); }
  .rule:first-of-type { border-top: 0; }
  .rule input[type="number"] { width: 4rem; }
  .rule .values { flex: 1 1 10rem; }
  .minutes { display: inline-flex; gap: .3rem; align-items: center; font-size: .9rem; }
  .hint { font-size: .8rem; }
  .leans { display: flex; flex-wrap: wrap; gap: .25rem; }
  .lean { display: inline-flex; align-items: center; gap: .35rem; font-size: .85rem; padding: .25rem .7rem .25rem .5rem;
    border: 1px solid var(--line); border-radius: 1rem; background: var(--panel); cursor: pointer; }
  .lean:has(input:checked) { background: var(--accent-bg); border-color: var(--accent); }
  .known { display: grid; grid-template-columns: repeat(auto-fill, minmax(min(15rem, 100%), 1fr)); gap: 0 1.25rem; }
  .known-style { display: flex; gap: .55rem; align-items: flex-start; justify-content: space-between; padding: .4rem 0;
    border-top: 1px solid var(--line); }
  .known-text { display: grid; min-width: 0; flex: 1; }
  .known-text .muted { font-size: .8rem; }
</style>
