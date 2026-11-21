<!-- The Plan tab: the week being planned or cooked from. With nothing planned it offers to plan a week. A draft
     shows the three steps to review it (meals, shopping list, confirm) with everything needed for each; a confirmed
     week is ticked off meal by meal and offers to plan the week after. When two weeks have plans, a switch at the
     top chooses which one is shown. -->
<script>
  import { app, ask, canPlanAhead, selectWeek, confirmPlan, replan, clearPlan, regenerate } from '../lib/app.svelte.js';
  import { shortDate, weekName } from '../lib/format.js';
  import { choosableStyles } from '../lib/styles.js';
  import PlanNextWeek from './PlanNextWeek.svelte';
  import ShoppingSummary from './ShoppingSummary.svelte';
  import WeekBoard from './WeekBoard.svelte';
  import SwapDialog from './SwapDialog.svelte';
  import Shuffle from '@lucide/svelte/icons/shuffle';

  const plan = $derived(app.plan);
  const draft = $derived(plan?.status === 'Staged');
  const name = $derived(plan ? weekName(plan.weekStart, plan.weekEnd) : '');
  const range = $derived(plan ? `${shortDate(plan.weekStart)} to ${shortDate(plan.weekEnd)}` : '');
  const choices = $derived(choosableStyles(app.styles));
  const keeping = $derived(app.keep.length);
  const meals = $derived(plan?.meals.filter((meal) => !meal.isOptional) ?? []);
  const done = $derived(meals.filter((meal) => meal.status !== 'Planned').length);

  /** Plans the draft's week again in another style, once the person agrees. The menu shows the plan's style until then. */
  async function changeStyle(event) {
    const select = event.currentTarget;
    const style = choices.find((choice) => choice.name === select.value);
    select.value = plan.styleName;
    if (!style || style.name === plan.styleName) return;
    const agreed = await ask({
      title: `Plan this week as ${style.name}?`,
      message: keeping
        ? `The draft is planned again in this style. The ${keeping} ${keeping === 1 ? 'meal' : 'meals'} marked Keep stay.`
        : 'The draft is planned again in this style.',
      confirmLabel: 'Plan again',
    });
    if (agreed) replan(style);
  }
</script>

{#if !plan}
  <PlanNextWeek
    title="Plan your week"
    intro="ChefBot proposes a week of meals in the style you choose, with a shopping list of what isn't already in your pantry." />
{:else}
  {#if app.plans.length > 1}
    <div class="segmented weeks" role="group" aria-label="Weeks with a plan">
      {#each app.plans as week (week.id)}
        <button type="button" aria-pressed={String(week.id === plan.id)} onclick={() => selectWeek(week.id)}>
          {weekName(week.weekStart, week.weekEnd)}{week.status === 'Staged' ? ', draft' : ''}
        </button>
      {/each}
    </div>
  {/if}

  {#if draft}
    <section class="card review" aria-labelledby="plan-heading">
      <div class="title-row">
        <h2 id="plan-heading">{name}: draft for {range}</h2>
        <span class="pill staged">Draft</span>
      </div>
      <p class="muted">
        Planned in the {plan.styleName || 'default'} style. Nothing is bought or cooked until you confirm it.
      </p>
      <ol class="steps">
        <li>
          <strong>Check the meals.</strong>
          <span class="muted">Drag a meal to another day, or use its Move menu. Swap any you don't want, and press Keep on
            the ones you like before shuffling the rest.</span>
        </li>
        <li>
          <strong>Check the shopping list.</strong>
          <ShoppingSummary />
        </li>
        <li>
          <strong>Confirm the plan.</strong>
          <span class="muted">Then tick items off as you shop, and mark meals as cooked.</span>
        </li>
      </ol>
      <div class="actions">
        <button class="primary" onclick={confirmPlan}>Confirm plan</button>
        <button class="with-icon" onclick={() => replan()}>
          <Shuffle size={15} />{keeping ? `Shuffle the rest (keeping ${keeping})` : 'Shuffle'}
        </button>
        {#if choices.length > 1}
          <label class="style-choice">
            <span class="sr-only">Plan style</span>
            <select value={plan.styleName} onchange={changeStyle}>
              {#if !choices.some((style) => style.name === plan.styleName)}<option value={plan.styleName}>{plan.styleName}</option>{/if}
              {#each choices as style (style.name)}<option value={style.name}>{style.name}</option>{/each}
            </select>
          </label>
        {/if}
        <button onclick={clearPlan}>Discard draft</button>
      </div>
    </section>
  {:else}
    <section class="card review" aria-labelledby="plan-heading">
      <div class="title-row">
        <h2 id="plan-heading">{name}: {range}</h2>
        <span class="pill confirmed">Confirmed</span>
      </div>
      <p class="muted">
        {done} of {meals.length} meals done. Mark each meal as cooked or skipped as you go: cooking takes its
        ingredients out of the pantry.
      </p>
      <ShoppingSummary />
      <div class="actions">
        <button title="Plan the rest of this week again around what is already in your pantry" onclick={regenerate}>Replan from pantry</button>
        <button title="Remove this plan and keep the pantry" onclick={clearPlan}>Clear plan</button>
      </div>
    </section>
  {/if}

  <WeekBoard />
  <SwapDialog />

  {#if canPlanAhead()}
    <PlanNextWeek
      title="The week after isn't planned yet"
      intro="Plan it now to see its meals and shopping list. This week's plan stays as it is." />
  {/if}
{/if}

<style>
  .weeks { margin: 0 0 .5rem; }
  .review { display: grid; gap: .5rem; padding: 1rem 1.1rem; }
  .title-row { display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; justify-content: space-between; }
  .review h2 { margin: 0; }
  .review p { margin: 0; }
  .steps { margin: 0; padding-left: 1.4rem; display: grid; gap: .4rem; }
  .steps li > strong { display: block; }
  .actions { align-items: center; margin-top: .25rem; }
  .with-icon { display: inline-flex; align-items: center; gap: .35rem; }
  .style-choice select { padding: .4rem .5rem; }
</style>
