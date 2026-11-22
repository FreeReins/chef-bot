# Testing

## The checks to run

```sh
dotnet build ChefBot.slnx -v q -nologo            # must have no warnings
dotnet run --project tests/ChefBot.Core.Tests     # all .NET tests, under a second
./dev.sh web                                      # build the web page
./dev.sh web-test                                 # accessibility and keyboard tests, ~90 s
```

## .NET tests

[TUnit](https://tunit.dev) tests in `tests/ChefBot.Core.Tests`. Assertions are awaited:
`await Assert.That(actual).IsEqualTo(expected);`.

- **Engines** are tested directly, with the small catalog and recipe library in `TestData.cs`.
- **Managers** are tested through `TestCore` (`new TestCore().PlanManager()`), wired to in-memory `Fake*` accessors
  that hand out copies the way the real ones do. `FakeMessages` records what was published.
- **Rules about the code itself:**

| Test | Checks |
|---|---|
| `ArchitectureTests` | Calls only go down the layers; only manager contracts are public |
| `FileSizeTests` | No `.cs`, `.svelte` or `.js` source file over 400 lines |
| `SchemaTests` | Every table is backed up or listed as left out; enums are stored by name |
| `JsonShapeTests` | Every contract is in its JSON context and reads JSON as reflection would |

Run one class with `--treenode-filter "/*/*/ShoppingTests/*"`.

## Web tests

`./dev.sh web-test` checks every screen with [axe](https://github.com/dequelabs/axe-core) (WCAG 2.2 A and AA) in
light, dark and phone layouts, plus keyboard behaviour, in Playwright's container. No ChefBot runs: `tests/fixtures.js`
answers every `/api` call from `tests/data.js`. A new screen gets a row in `VIEWS` in `tests/a11y.spec.js`.

`./dev.sh web-shot /plan` takes screenshots of one address into `src/ChefBot.Web/shots/`.

## Docs

`mkdocs build --strict` fails on a broken link or anchor between pages, and the link check workflow covers outside
links. See [Building and releasing](building-and-releasing.md#the-documentation).
