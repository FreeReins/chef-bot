// Light or dark. The page follows the device's setting until someone picks one, with the sun and moon button in the
// header (ThemeToggle.svelte) or in Settings, Appearance. The choice is kept in this browser only, under KEY, and
// shown by setting data-theme on <html>, which styles.css reads. index.html applies it before the page first paints,
// so a dark choice never flashes light.
const KEY = 'chefbot-theme';
const dark = typeof matchMedia === 'function' ? matchMedia('(prefers-color-scheme: dark)') : null;

function remembered() {
  try {
    const value = localStorage.getItem(KEY);
    return value === 'light' || value === 'dark' ? value : null;
  } catch {
    return null;
  }
}

/** choice: 'light', 'dark', or null to follow the device. deviceDark: whether the device asks for dark now. */
export const theme = $state({ choice: remembered(), deviceDark: dark?.matches ?? false });

dark?.addEventListener('change', (event) => { theme.deviceDark = event.matches; });

/** Whether the page is dark now, by choice or by following the device. */
export const isDark = () => (theme.choice ?? (theme.deviceDark ? 'dark' : 'light')) === 'dark';

/** Picks light or dark, or null to follow the device again. */
export function setTheme(choice) {
  theme.choice = choice;
  if (choice) document.documentElement.dataset.theme = choice;
  else delete document.documentElement.dataset.theme;
  try {
    if (choice) localStorage.setItem(KEY, choice);
    else localStorage.removeItem(KEY);
  } catch { /* kept for this visit only */ }
}

/** Switches to the other of light and dark from what is showing now. */
export const toggleTheme = () => setTheme(isDark() ? 'light' : 'dark');
