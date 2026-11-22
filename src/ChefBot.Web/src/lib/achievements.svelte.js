import { api } from './api.js';
import { app, toast } from './state.svelte.js';
import { unlockMessages } from './achievements.js';
import { onLive } from './live.svelte.js';

// ---------- achievements ----------
// The household's first steps, badges and chef rank (AchievementsResponse) in app.achievements. They are a treat,
// not a feature anything depends on: loading them never shows an error, and offline they just stay as they were.
// What each person has been told about is kept by ChefBot; the cards they put away are kept in this browser only.

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

/**
 * Loads the achievements, and shows what was reached that this person hasn't been shown yet, on any device. ChefBot
 * keeps track of that, so a second tab or a new phone doesn't show it again.
 */
export async function loadAchievements() {
  let next;
  try {
    next = await api('GET', '/api/achievements');
  } catch {
    return;
  }
  app.achievements = next;
  const unseen = next.unseen ?? [];
  if (!unseen.length) return;
  for (const message of unlockMessages(next, seenBefore(next, unseen))) toast(message);
  api('POST', '/api/achievements/seen', { upTo: unseen.at(-1).id }).catch(() => { /* shown again next time */ });
}

// An achievement was earned, by anyone: show it straight away.
onLive('achievements', () => loadAchievements());

/** The tiers as they were before the unseen ones, in the shape unlockMessages() compares against. */
function seenBefore(next, unseen) {
  const seen = Object.fromEntries([...next.starter, ...next.badges].map((achievement) => [achievement.kind, achievement.tier]));
  seen.rank = next.rank;
  for (const unlock of unseen) {
    if (unlock.kind == null) seen.rank = Math.min(seen.rank, unlock.tier - 1);
    else seen[unlock.kind] = Math.min(seen[unlock.kind] ?? 0, unlock.tier - 1);
  }
  return seen;
}
