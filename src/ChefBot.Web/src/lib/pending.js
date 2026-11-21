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
