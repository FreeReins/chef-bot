import { api } from './api.js';
import { onLive } from './live.svelte.js';
import { app, run, setNoteSink } from './state.svelte.js';

// ---------- the inbox ----------
// The messages behind the bell in the header (Inbox.svelte). Notes from toast() go here instead of the corner while
// someone is signed in, and ChefBot adds its own, such as a plan the weekly schedule staged. Each person has one inbox,
// kept by ChefBot, so every device shows the same messages. A note shows at once as a local entry, which the saved one
// replaces; one made offline stays local until the page is reloaded.

let nextLocalId = 0;

/** Takes a note for the inbox when someone is signed in and past setup, which has no bell. */
function takeNote(text) {
  if (!app.session?.user || app.screen.startsWith('setup-')) return false;
  const entry = { id: `local-${nextLocalId++}`, text, createdAt: new Date().toISOString(), read: false, local: true };
  app.inboxLocal.unshift(entry);
  if (!app.offline) {
    api('POST', '/api/inbox', { text })
      .then((inbox) => { forget(entry.id); app.inbox = inbox; })
      .catch(() => { /* kept on this page only, until it is reloaded */ });
  }
  return true;
}

setNoteSink(takeNote);

const forget = (id) => (app.inboxLocal = app.inboxLocal.filter((entry) => entry.id !== id));

/** Loads the signed-in person's inbox. Failures stay quiet: the bell keeps what it had. */
export async function loadInbox() {
  if (!app.session?.user || app.offline) return;
  try {
    app.inbox = await api('GET', '/api/inbox');
  } catch { /* the next change or reload tries again */ }
}

/** Every message, newest first: the saved ones and those only on this page. */
export const inboxEntries = () =>
  [...app.inboxLocal, ...app.inbox.entries].sort((a, b) => b.createdAt.localeCompare(a.createdAt));

export const inboxUnread = () => app.inbox.unread + app.inboxLocal.filter((entry) => !entry.read).length;

/** The bell was opened: everything in it is read now. */
export function readInbox() {
  for (const entry of app.inboxLocal) entry.read = true;
  if (!app.inbox.unread || app.offline) return;
  app.inbox = { ...app.inbox, unread: 0, entries: app.inbox.entries.map((entry) => ({ ...entry, read: true })) };
  api('POST', '/api/inbox/read').then((inbox) => (app.inbox = inbox)).catch(() => { /* unread again after a reload */ });
}

export function clearInbox() {
  return run(async () => {
    app.inboxLocal = [];
    app.inbox = app.offline ? { entries: [], unread: 0 } : await api('DELETE', '/api/inbox');
  });
}

/** Signing out: the next person starts with their own inbox. */
export function forgetInbox() {
  app.inbox = { entries: [], unread: 0 };
  app.inboxLocal = [];
}

// A message arrived, was read or was cleared, on this device or another.
onLive('inbox', () => loadInbox());
