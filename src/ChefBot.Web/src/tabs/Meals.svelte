<!-- What the plan cooks, grouped by slot. Leftover lunches are left out: they follow from the dinners. -->
<script>
  import { app } from '../lib/app.svelte.js';
  import { SLOT_TITLES } from '../lib/format.js';
  import MealCard from './MealCard.svelte';

  const groups = $derived(Object.entries(SLOT_TITLES)
    .map(([slot, title]) => ({ slot, title, meals: app.plan.meals.filter((meal) => !meal.isLeftover && meal.slot === slot) }))
    .filter((group) => group.meals.length > 0));
</script>

{#each groups as group (group.slot)}
  <h2>{group.title}</h2>
  {#each group.meals as meal (meal.id)}
    <MealCard {meal} />
  {/each}
{/each}
