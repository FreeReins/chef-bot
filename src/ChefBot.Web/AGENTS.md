# AGENTS.md for the web page

Svelte 5 in plain JavaScript (no TypeScript), built by Vite into `src/ChefBot.Web/dist/`, which ChefBot.Api serves. That folder is build
output: never edit it or add it to git. CONTRIBUTING.md's "The web page" has the detail.

## Check your work

Run these from the repository root. A web change is done when both pass with no warnings.

```sh
./dev.sh web                              # build the page; exits
./dev.sh web-test                         # accessibility tests in light, dark and phone, ~90s; exits
./dev.sh web-test --grep "history"        # only the rows whose name matches
./dev.sh web-shot /history                # screenshots into src/ChefBot.Web/shots/ (one per project)
```

`web-test` prints a dot per test; add `--reporter=list` to see names. There is no Node on the host: don't run
`npm` or `npx` directly.

## Rules

- The page's state (`app`) and `run()` live in `src/lib/state.svelte.js`; the actions shared between screens in
  `src/lib/<area>.svelte.js` (plan, shopping, recipes, setup, settings, session). Components import all of it
  from `src/lib/app.svelte.js`, which re-exports them; modules in `src/lib/` import from each other directly.
  A form's own submit stays in its component.
- Wrap anything that calls the API in `run()`, so failures show as a message.
- When ChefBot can't be reached, `api()` throws with `error.offline` and `run()` switches the page to offline
  mode (`app.offline`). Only the shopping actions queue changes in the outbox (`src/lib/outbox.svelte.js`). Any
  other action just fails offline. The plan's recipes are kept to read offline (`offline.svelte.js`); their page hides
  what needs ChefBot when `app.offline`.
- An async button uses `onclick={pendingClick(() => action())}` and a form's submit uses `pending(event, ...)`,
  both from `src/lib/pending.js`. They give a spinner and ignore a second press. The action must return its
  `run()` promise. Keep busy buttons clickable (no `pointer-events: none`): `keyboard.spec.js` clicks twice.
- Movement comes from `src/lib/motion.js`: `stepIn`/`stepOut`, `rise`, `deal`, `collapse`, `popIn`, `popOn`
  and `celebrate()`. Put CSS animations inside `@media (prefers-reduced-motion: no-preference)`. The tests run
  with reduced motion, so anything that still animates can fail axe halfway through.
- A component style for an attribute set at runtime, such as `[aria-busy]`, needs `:global(...)`. Otherwise the
  build warns about an unused selector.
- A new screen or state gets a row in `VIEWS` in `tests/a11y.spec.js` (CONTRIBUTING.md, "Accessibility tests").
  The stubbed API answers are in `tests/data.js` and `tests/fixtures.js`.
- For a new tab, follow `.claude/skills/add-web-screen/SKILL.md`.
