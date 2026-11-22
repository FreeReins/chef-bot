import { api } from './api.js';
import { app, toast } from './state.svelte.js';
import { unlockMessages } from './achievements.js';

// ---------- achievements ----------
// The household's first steps, badges and chef rank (AchievementsResponse) in app.achievements. They are a treat,
// not a feature anything depends on: loading them never shows an error, and offline they just stay as they were.
// What this viewer has already been told about, and the cards they put away, are kept in this browser only.

const key = (name) => `chefbot.achievements.${name}.${app.session?.user?.id ?? 'anyone'}`;

function read(name) {
  try { return localStorage.getItem(key(name)); } catch { return null; }
}

function write(name, value) {
  try { localStorage.setItem(key(name), value); } catch { /* private window or blocked storage: nothing to keep */ }
}

/** True once this viewer has put something away, such as the first-steps card ('quest-done'). */
export const isPutAway = (name) => read(name) === '1';

/** Remembers that this viewer put something away. */
export const putAway = (name) => write(name, '1');

/** Loads the achievements and congratulates on anything reached since this viewer last looked. */
export async function loadAchievements() {
  let next;
  try {
    next = await api('GET', '/api/achievements');
  } catch {
    return;
  }
  congratulate(next);
  app.achievements = next;
}

let timer = null;

/** Loads the achievements again shortly, after something that may have earned one. Many calls make one load. */
export function refreshAchievements() {
  if (!app.session?.user) return;
  clearTimeout(timer);
  timer = setTimeout(loadAchievements, 400);
}

/** Toasts what is new since the tiers last seen. The very first look only remembers them, so nobody is flooded. */
function congratulate(next) {
  const tiers = Object.fromEntries([...next.starter, ...next.badges].map((achievement) => [achievement.kind, achievement.tier]));
  tiers.rank = next.rank;
  const stored = read('seen');
  write('seen', JSON.stringify(tiers));
  if (!stored) return;
  let seen;
  try { seen = JSON.parse(stored); } catch { return; }
  for (const message of unlockMessages(next, seen)) toast(message);
}
