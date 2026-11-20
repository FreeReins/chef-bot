---
name: tune-agents-md
description: Measure and improve the repo's AGENTS.md with Microsoft waza, checking its token cost and running eval tasks with and without it. Use when asked to improve, trim, test or evaluate AGENTS.md or agent instructions, or after an agent made a mistake that AGENTS.md should have prevented.
---

# Tune AGENTS.md with waza

AGENTS.md is loaded into every agent session. Each line should stop a mistake an agent would otherwise make,
for as few tokens as possible. This skill measures both and changes the file only when the numbers support it.

The suite lives in `evals/agents-md/`. `eval.yaml` runs each task with AGENTS.md given to the agent, and
`baseline.yaml` runs the same tasks without it. Settings, including the AGENTS.md token budget, are in
`.waza.yaml`.

## 1. Use the container

Run waza through `evals/waza.sh`, never a host install. The script builds a pinned waza image on first use
(it needs only Podman) and runs it with this repo at `/work`. Its container has no network, so only the offline
steps below work.

## 2. Measure size

```sh
evals/waza.sh tokens check AGENTS.md .claude/skills/tune-agents-md/SKILL.md --strict
```

This exits 1 when a file is over its budget in `.waza.yaml`. Over budget means cut before adding anything. If
the limits shown are 2000 for AGENTS.md and 500 for SKILL.md, waza ignored `.waza.yaml`; check that file
for a `telemetry:` section and remove it.

## 3. Check the suites

Copy the current file into the "with" context first; waza reads instruction files from the context directory.

```sh
cp AGENTS.md evals/agents-md/context/with/AGENTS.md
evals/waza.sh preflight evals/agents-md/eval.yaml --context-dir evals/agents-md/context/with
evals/waza.sh preflight evals/agents-md/baseline.yaml --context-dir evals/agents-md/context/without
```

Each should list all the tasks and exit 0. If preflight reports a schema error, fix the suite files and say
what you changed. Don't add `--strict`: it always fails offline, on the warning that the Copilot runtime
wasn't checked.

## 4. Run both suites

Real runs aren't set up yet. They need `evals/waza.sh` to allow network access and pass in a Copilot token
(waza drives agents only through GitHub Copilot). Stop after step 3 and tell the user, unless that has been
added. Once it has:

```sh
evals/waza.sh run evals/agents-md/eval.yaml -c evals/agents-md/context/with --trials 3 -o results/with.json
evals/waza.sh run evals/agents-md/baseline.yaml -c evals/agents-md/context/without --trials 3 -o results/without.json
evals/waza.sh compare results/with.json results/without.json
```

Real runs use the user's Copilot requests. Tell the user how many you plan before starting:
tasks × trials × 2 suites. First run `evals/waza.sh models` and confirm the `model` in both eval files is
listed. Run 3 trials, because one is too noisy to judge a single line. `waza run` exits 1 when tasks fail,
which is a normal result.

## 5. Read the results

| With AGENTS.md | Without | Meaning | Action |
|---|---|---|---|
| passes | fails | The line works | Keep it |
| passes | passes | The model already knows this | Try cutting the line, then rerun |
| fails | fails | The rule is missing or unclear | Reword it, or move it nearer the top |
| fails | passes | The file misleads | Find the contradiction and fix it |

Also read failing transcripts (`evals/waza.sh run ... -v`, or `--transcript-dir`). A wrong answer is often a wrong task:
fix the task when its expected text is too strict.

## 6. Change AGENTS.md

- Keep a change only when the "with" pass rate doesn't drop and the token count stays within budget.
- Write plain imperatives. Don't use capitals or "IMPORTANT" for emphasis.
- Link to CONTRIBUTING.md or other docs instead of copying from them.
- Every command in the file must be one you have run in this repo.
- Commit AGENTS.md together with any task changes, so the suite always matches the file.

## Adding tasks

When an agent gets something wrong in real work, add a task in `evals/agents-md/tasks/` that reproduces it,
check that it fails in both suites, then add the rule to AGENTS.md and check that it passes "with".
Follow the existing tasks: a prompt that names ChefBot and asks for a short answer, plus
`expected.output_contains` with the text a correct answer must include. Use a `text` grader with
`not_contains` for things the answer must not say.

These tasks test whether the guidance reaches the agent, not whole coding sessions. For a full coding
task, copy the files it needs into a context directory and grade with `tool_constraint` or `file` graders;
see https://microsoft.github.io/waza/guides/graders/.

## Report back

Give the token count before and after, the pass rate for both suites before and after, and each AGENTS.md line
you added, cut or reworded, with the task result behind it.
