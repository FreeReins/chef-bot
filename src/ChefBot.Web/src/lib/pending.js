// Keeps a form from being sent twice. Wrap a submit handler's work in pending(event, work): the button that sent the
// form shows a spinner (button[aria-busy] in styles.css) until the work is done, and submitting again meanwhile
// does nothing.

/** Runs `work` (which returns a promise, usually run(...)) unless the form's last submit is still under way. */
export async function pending(event, work) {
  const form = event.currentTarget;
  const button = event.submitter ?? form.querySelector?.('[type="submit"]');
  if (form.dataset.pending) return undefined;
  form.dataset.pending = 'true';
  button?.setAttribute('aria-busy', 'true');
  try {
    return await work();
  } finally {
    delete form.dataset.pending;
    button?.removeAttribute('aria-busy');
  }
}

/**
 * The same for a button outside a form: `onclick={pendingClick(() => confirmPlan())}`. The pressed button shows the
 * spinner until the promise `work` returns settles, and pressing it again meanwhile does nothing, so an action can't
 * be sent twice. `work` receives the click event.
 */
export function pendingClick(work) {
  // The mark is kept on the button rather than here: Svelte may make a new handler while the old one's work is
  // still under way, and both must see it.
  return async (event) => {
    const button = event?.currentTarget;
    if (button?.getAttribute?.('aria-busy') === 'true') return undefined;
    button?.setAttribute?.('aria-busy', 'true');
    try {
      return await work(event);
    } finally {
      button?.removeAttribute?.('aria-busy');
    }
  };
}
