// Screenshots one address of the page, answered from the stubs in fixtures.js, for a person or an agent to look at.
// Runs only through ./dev.sh web-shot <address>, which sets SHOT_PATH; playwright.config.js skips it otherwise.
import { test } from './fixtures.js';

test('screenshot', async ({ page }) => {
  const address = process.env.SHOT_PATH;
  await page.goto(address);
  await page.waitForLoadState('networkidle');
  await page.waitForFunction(() => document.getAnimations().every((animation) => animation.playState !== 'running'));
  const name = address.replace(/^\/+/, '').replace(/[^a-z0-9]+/gi, '-').replace(/-+$/, '') || 'home';
  await page.screenshot({ path: `shots/${name}-${test.info().project.name}.png`, fullPage: true });
});
