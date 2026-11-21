// Saves a settings section by itself shortly after it changes, so there is no Save button to forget. Use it in a
// component's <script> (it sets up an effect), show its status with SaveStatus.svelte, and call schedule() when
// something changes:
//
//   const saver = autosave(async () => { app.backups = await api('PUT', ...); }, { validate: () => form.checkValidity() });
//   <form onchange={saver.schedule}>  ...  <SaveStatus {saver} />
//
// A change waiting to be saved is saved at once when the section closes or the page is left.
import { run } from './app.svelte.js';

/**
 * save: async () => void, sends the changes. Errors are shown by run() as usual.
 * validate: () => boolean, false while something can't be saved yet, such as a required field left empty.
 * delay: milliseconds to wait after the last change, so quick changes go in one save.
 */
export function autosave(save, { validate = () => true, delay = 600 } = {}) {
  /** 'idle' (nothing changed yet), 'saving', 'saved', 'invalid' or 'error'. */
  const state = $state({ status: 'idle' });
  let timer = null;
  let saving = null;
  let again = false;

  async function now() {
    clearTimeout(timer);
    timer = null;
    // One save at a time; a change made meanwhile is saved straight after.
    if (saving) {
      again = true;
      return saving;
    }
    if (!validate()) {
      state.status = 'invalid';
      return undefined;
    }
    state.status = 'saving';
    let done = false;
    saving = run(async () => {
      await save();
      done = true;
    });
    await saving;
    saving = null;
    state.status = done ? 'saved' : 'error';
    if (again) {
      again = false;
      return now();
    }
    return undefined;
  }

  function schedule() {
    clearTimeout(timer);
    state.status = validate() ? 'saving' : 'invalid';
    timer = setTimeout(now, delay);
  }

  /** Saves a waiting change now, if there is one. */
  const flush = () => (timer ? now() : saving);

  $effect(() => {
    addEventListener('pagehide', flush);
    return () => {
      removeEventListener('pagehide', flush);
      flush();
    };
  });

  return {
    get status() { return state.status; },
    schedule,
    flush,
    retry: now,
  };
}
