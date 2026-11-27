// Accessibility tests for the web page: real Chromium, with the API answered from tests/fixtures.js, so no ChefBot
// or database is needed. Run with ./dev.sh web-test, which uses the Playwright container (see CONTRIBUTING.md).
// The page is a production build, served by `vite preview`: each test opens a few bundled files, where Vite's dev
// server would send hundreds of separate modules (every icon is one) to every page. It is built into
// node_modules/.cache, so dist/ is left alone.
import { defineConfig, devices } from '@playwright/test';

const port = 4173;
const outDir = 'node_modules/.cache/playwright-web';
/** Set by ./dev.sh web-shot: the address to screenshot. Then only tests/screenshot.spec.js runs. */
const shooting = Boolean(process.env.SHOT_PATH);

export default defineConfig({
  testDir: 'tests',
  forbidOnly: true,
  retries: 0,
  // Each test opens its own page with its own stubs, so tests in one file can run side by side. Otherwise a file
  // runs in a single worker, and a11y.spec.js's hundred-odd rows queue up behind each other.
  fullyParallel: true,
  // Playwright's default is half the cores. A test mostly waits on its own browser, so a CI runner (CI=1, also set
  // by ./dev.sh web-test) uses all of them.
  workers: process.env.CI ? '100%' : undefined,
  // A dot per test in the container (CI=1), so a full run prints a few lines; pass --reporter=list to see each test.
  reporter: process.env.CI ? 'dot' : 'list',
  testMatch: shooting ? 'screenshot.spec.js' : '*.spec.js',
  testIgnore: shooting ? [] : ['screenshot.spec.js'],
  // Less motion, as some people ask their system for: the page's transitions finish at once (lib/motion.js), so axe
  // never checks something half faded in and nothing waits on an animation.
  use: { baseURL: `http://localhost:${port}`, locale: 'en-US', timezoneId: 'UTC', contextOptions: { reducedMotion: 'reduce' },
    // The build registers public/sw.js, whose fetches page.route() can't see; the stubs must answer every request.
    serviceWorkers: 'block' },
  // Light and dark mode use different colours, so contrast is checked in both. Phones lay the page out differently
  // (the tabs move to a bar at the bottom of the window).
  projects: [
    { name: 'desktop-light', use: { ...devices['Desktop Chrome'], colorScheme: 'light' } },
    { name: 'desktop-dark', use: { ...devices['Desktop Chrome'], colorScheme: 'dark' } },
    { name: 'phone', use: { ...devices['Pixel 7'], colorScheme: 'light' } },
  ],
  webServer: {
    command: `npx vite build --outDir ${outDir} --emptyOutDir --logLevel warn && npx vite preview --outDir ${outDir} --port ${port} --strictPort`,
    url: `http://localhost:${port}`,
    reuseExistingServer: false,
    timeout: 120_000,
  },
});
