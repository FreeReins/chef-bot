// Builds ChefBot's web page into dist/. ChefBot.Api serves it from there when run from the source tree; the image
// copies it into the app's wwwroot.
// `npm run dev` serves it with live reload instead, passing API calls through to a running ChefBot.
import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';

// Set by ./dev.sh, which picks the ports.
const api = process.env.CHEFBOT_API || 'http://localhost:8080';
const port = Number(process.env.CHEFBOT_WEB_PORT || 5173);

export default defineConfig({
  plugins: [svelte()],
  server: {
    host: 'localhost',
    port,
    strictPort: true,
    proxy: Object.fromEntries(['/api', '/openapi', '/docs', '/health', '/mcp'].map((path) => [path, { target: api }])),
  },
});
