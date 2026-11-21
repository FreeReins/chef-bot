# AGENTS.md for the web page

Svelte 5 in plain JavaScript (no TypeScript), built by Vite into `src/ChefBot.Api/wwwroot/`. That folder is build
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

- The page's state (`app`) and the actions shared between screens live in `src/lib/app.svelte.js`; a form's own
  submit stays in its component.
- Wrap anything that calls the API in `run()`, so failures show as a message.
- A new screen or state gets a row in `VIEWS` in `tests/a11y.spec.js` (CONTRIBUTING.md, "Accessibility tests").
  The stubbed API answers are in `tests/data.js` and `tests/fixtures.js`.
- For a new tab, follow `.claude/skills/add-web-screen/SKILL.md`.
