// The page's movement: Svelte transitions for steps, views and lists, and a small burst for real milestones.
// Everything here is instant when the person has asked their system for less motion, and the tests ask for that too
// (playwright.config.js), so nothing is half-faded while it is checked.
import { fly, slide } from 'svelte/transition';
import { cubicOut, backOut } from 'svelte/easing';

/** True when the person has asked for less motion. */
export const reducedMotion = () => globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

const calm = (options) => (reducedMotion() ? { ...options, duration: 0, delay: 0 } : options);

/** A setup step arriving: from the right going forward (direction 1), from the left going back (-1). */
export const stepIn = (node, { direction = 1 } = {}) =>
  fly(node, calm({ x: 40 * direction, duration: 260, delay: 60, easing: cubicOut }));

/** A setup step leaving the other way. */
export const stepOut = (node, { direction = 1 } = {}) =>
  fly(node, calm({ x: -40 * direction, duration: 160, easing: cubicOut }));

/** A view or row rising into place. */
export const rise = (node, { duration = 180, delay = 0 } = {}) =>
  fly(node, calm({ y: 8, duration, delay, easing: cubicOut }));

/** A card dealt onto the table: position `index` waits a moment longer, so a set of cards arrives in turn. */
export const deal = (node, { index = 0 } = {}) =>
  fly(node, calm({ y: 12, duration: 240, delay: Math.min(index, 12) * 35, easing: backOut }));

/** A row folding away once it is done with, such as a shopping line once it is bought. */
export const collapse = (node, { duration = 260, delay = 150 } = {}) => slide(node, calm({ duration, delay, easing: cubicOut }));

/** A chip or badge popping into place, such as "Cooked" on a meal just marked. */
export const popIn = (node, { duration = 320, delay = 0 } = {}) => calm({
  duration, delay, easing: backOut,
  css: (t) => `transform: scale(${0.5 + 0.5 * t}); opacity: ${Math.min(1, t * 2)}`,
});

// The food-group colours from styles.css, so the burst looks like a scatter of herbs and vegetables.
const SPRINKLES = ['--group-vegetable', '--group-fruit', '--group-herb', '--group-protein', '--group-wholegrain', '--group-dairy', '--noodle'];

/**
 * A small burst of coloured dots from the middle of `element`, for something worth marking: an account made, a plan
 * confirmed, the last thing bought. The dots are decoration only and remove themselves when they land.
 */
export function celebrate(element, { count = 14 } = {}) {
  if (!element?.getBoundingClientRect || reducedMotion() || !document.body.animate) return;
  const box = element.getBoundingClientRect();
  const x = box.left + box.width / 2;
  const y = box.top + box.height / 2;
  const style = getComputedStyle(document.documentElement);
  for (let index = 0; index < count; index++) {
    const dot = document.createElement('span');
    dot.className = 'sprinkle';
    dot.setAttribute('aria-hidden', 'true');
    const size = 5 + Math.random() * 5;
    Object.assign(dot.style, {
      left: `${x}px`, top: `${y}px`, width: `${size}px`, height: `${size * (Math.random() < 0.5 ? 1 : 2.2)}px`,
      background: style.getPropertyValue(SPRINKLES[index % SPRINKLES.length]).trim() || 'currentColor',
    });
    document.body.append(dot);
    const angle = (index / count) * Math.PI * 2 + Math.random() * 0.4;
    const distance = 40 + Math.random() * 50;
    const dx = Math.cos(angle) * distance;
    const dy = Math.sin(angle) * distance;
    const spin = Math.random() * 360 - 180;
    dot.animate([
      { transform: 'translate(-50%, -50%) scale(.4) rotate(0deg)', opacity: 1 },
      { transform: `translate(calc(-50% + ${dx}px), calc(-50% + ${dy}px)) scale(1) rotate(${spin}deg)`, opacity: 1, offset: 0.6 },
      { transform: `translate(calc(-50% + ${dx * 1.1}px), calc(-50% + ${dy + 24}px)) scale(.8) rotate(${spin * 1.4}deg)`, opacity: 0 },
    ], { duration: 700 + Math.random() * 250, easing: 'cubic-bezier(.2,.7,.3,1)' }).finished.finally(() => dot.remove());
  }
}

/**
 * Plays the `.pop` animation on an element again, for a count or chip that has just changed. Use as
 * `use:popOn={value}`: it pops whenever the value changes, not when it first appears.
 */
export function popOn(node, value) {
  let last = value;
  return {
    update(next) {
      if (next === last) return;
      last = next;
      node.classList.remove('pop');
      void node.offsetWidth; // so removing and adding the class starts the animation again
      node.classList.add('pop');
    },
  };
}
