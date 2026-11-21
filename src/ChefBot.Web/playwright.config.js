// Accessibility tests for the web page: real Chromium, with the API answered from tests/fixtures.js, so no ChefBot
// or database is needed. Run with ./dev.sh web-test, which uses the Playwright container (see CONTRIBUTING.md).
// The page is served by Vite's dev server, so nothing is written to ChefBot.Api's wwwroot.
import { defineConfig, devices } from '@playwright/test';

const port = 4173;
/** Set by ./dev.sh web-shot: the address to screenshot. Then only tests/screenshot.spec.js runs. */
const shooting = Boolean(process.env.SHOT_PATH);

export default defineConfig({
  testDir: 'tests',
  forbidOnly: true,
  retries: 0,
  // A dot per test in the container (CI=1), so a full run prints a few lines; pass --reporter=list to see each test.
  reporter: process.env.CI ? 'dot' : 'list',
  testMatch: shooting ? 'screenshot.spec.js' : '*.spec.js',
  testIgnore: shooting ? [] : ['screenshot.spec.js'],
  use: { baseURL: `http://localhost:${port}`, locale: 'en-US', timezoneId: 'UTC' },
  // Light and dark mode use different colours, so contrast is checked in both. Phones lay the page out differently
  // (the tabs move to a bar at the bottom of the window).
  projects: [
    { name: 'desktop-light', use: { ...devices['Desktop Chrome'], colorScheme: 'light' } },
    { name: 'desktop-dark', use: { ...devices['Desktop Chrome'], colorScheme: 'dark' } },
    { name: 'phone', use: { ...devices['Pixel 7'], colorScheme: 'light' } },
  ],
  webServer: {
    command: `npx vite --port ${port} --strictPort --clearScreen false --logLevel warn`,
    url: `http://localhost:${port}`,
    reuseExistingServer: false,
  },
});
