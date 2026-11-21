// Light or dark as chosen in this browser, before the first paint; lib/theme.svelte.js keeps it after that.
// A file rather than inline in index.html, so the Content-Security-Policy can refuse every inline script.
try {
  const theme = localStorage.getItem('chefbot-theme');
  if (theme === 'light' || theme === 'dark') document.documentElement.dataset.theme = theme;
} catch { /* follow the device */ }
