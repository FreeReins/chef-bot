<!-- The sample recipe picker on its own page, reached from the Recipes tab. Setup step three uses SamplePicker too. -->
<script>
  import { app } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import SamplePicker from '../components/SamplePicker.svelte';

  async function added(count) {
    await go('/recipes');
    app.notes = [count ? `Added ${count} sample recipes. Stage a week when you are ready.` : 'No sample recipes were added.'];
  }
</script>

<section class="gate wide">
  <h2>Sample recipes</h2>
  <p class="muted">Tick the ones you want to add to your recipes. Ones you already have are marked.</p>
  <div class="card"><SamplePicker onadded={added} /></div>
  <div class="row-actions" style="margin-top:1rem">
    <a href="/recipes">Back to your recipes</a>
  </div>
</section>
