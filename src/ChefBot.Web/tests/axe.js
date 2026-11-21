// Runs axe-core over the page and fails with one readable line per problem.
import AxeBuilder from '@axe-core/playwright';
import { expect } from '@playwright/test';

/** WCAG 2.2 A and AA, plus axe's best practices. No rule is turned off here. */
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'];

/**
 * Checks the whole page, or only `include` (a CSS selector), once transitions have finished, since a half-faded
 * element reads as low contrast. `disable` turns rules off for one call; say why beside the call.
 */
export async function expectAccessible(page, { include, disable = [] } = {}) {
  await page.waitForFunction(() => document.getAnimations().every((animation) => animation.playState !== 'running'));
  let builder = new AxeBuilder({ page }).withTags(TAGS);
  if (include) builder = builder.include(include);
  if (disable.length) builder = builder.disableRules(disable);
  const { violations } = await builder.analyze();
  const problems = violations.flatMap((violation) => violation.nodes.map((node) =>
    `${violation.id} (${violation.impact}): ${violation.help}\n    at ${node.target.join(' ')}\n    ${node.failureSummary?.replaceAll('\n', '\n    ')}`));
  expect(problems, `accessibility problems on ${page.url()}`).toEqual([]);
}
