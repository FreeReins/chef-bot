// The History timeline's noodle, drawn as one SVG path so the mouse can push it aside. Each point down the noodle is a
// small spring: the pointer nudges the nearby ones away, and they wobble back into place when it leaves. With reduced
// motion, or without a mouse, the noodle is drawn once and stays still. Decoration only: the SVG is aria-hidden.
import { celebrate, reducedMotion } from './motion.js';

const HALF = 45;       // the wave repeats every 45px: out to the right and back across to the left
const STEPS = 12;      // points per half-wave
const CENTRE = 40;     // half the width of .noodle in History.svelte
const REACH = 34;      // how close (px) the pointer gets before the noodle starts to give
const MAX_PUSH = 16;   // how far it gives, so it stays under the plates
const SPREAD = 42;     // how far up and down the noodle a push is felt
const STIFFNESS = 0.14;
const DAMPING = 0.8;

const bezier = (a, b, c, d, t) => {
  const u = 1 - t;
  return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
};

// One half-wave of the noodle's curve (C 25 22, 1 23, 13 45 from 13 0), as offsets from its centre line.
const HALF_WAVE = Array.from({ length: STEPS }, (_, index) => {
  const t = index / STEPS;
  return [bezier(0, 12, -12, 0, t), bezier(0, 22, 23, 45, t)];
});

/**
 * Draws and moves the noodle inside a timeline. Use as `use:noodle` on the timeline: it needs an `svg.noodle` child
 * holding the paths to draw (the pasta, and its glow) and a `radialGradient` for the glow to follow the pointer.
 * While a mouse is over the timeline the node has `data-pointer`. Clicking a `.plate` gives it a small burst.
 */
export function noodle(node) {
  const svg = node.querySelector('svg.noodle');
  const paths = svg.querySelectorAll('path');
  const glow = svg.querySelector('radialGradient');
  let xs = [];
  let ys = [];
  let push = new Float32Array(0);
  let speed = new Float32Array(0);
  let pointer = null;
  let frame = 0;

  function shape(height) {
    xs = [];
    ys = [];
    for (let top = 0; top < height + HALF; top += HALF) {
      for (const [x, y] of HALF_WAVE) { xs.push(x); ys.push(top + y); }
    }
    push = new Float32Array(xs.length);
    speed = new Float32Array(xs.length);
    draw();
  }

  function draw() {
    let d = '';
    for (let index = 0; index < xs.length; index++) {
      d += `${index ? 'L' : 'M'}${(CENTRE + xs[index] + push[index]).toFixed(1)} ${ys[index].toFixed(1)}`;
    }
    for (const path of paths) path.setAttribute('d', d);
  }

  // The side of the noodle the pointer is on, judged where the noodle is now, so it slips past once pushed too far.
  function side() {
    const near = Math.min(xs.length - 1, Math.max(0, Math.round((pointer.y / HALF) * STEPS)));
    return Math.sign(pointer.x - xs[near] - push[near]) || 1;
  }

  function step() {
    frame = 0;
    const from = pointer ? side() : 0;
    let moving = false;
    for (let index = 0; index < xs.length; index++) {
      let target = 0;
      if (pointer) {
        const gap = from * (pointer.x - xs[index]);
        const along = (ys[index] - pointer.y) / SPREAD;
        target = -from * Math.min(MAX_PUSH, Math.max(0, REACH - gap)) * Math.exp(-along * along);
      }
      speed[index] = (speed[index] + (target - push[index]) * STIFFNESS) * DAMPING;
      push[index] += speed[index];
      if (Math.abs(speed[index]) > 0.01 || Math.abs(target - push[index]) > 0.05) moving = true;
    }
    draw();
    if (moving) frame = requestAnimationFrame(step);
  }

  const wake = () => { if (!frame) frame = requestAnimationFrame(step); };

  const move = (event) => {
    if (event.pointerType !== 'mouse') return;
    const box = svg.getBoundingClientRect();
    pointer = { x: event.clientX - box.left - CENTRE, y: event.clientY - box.top };
    glow?.setAttribute('cy', String(Math.round(pointer.y)));
    node.dataset.pointer = '';
    wake();
  };
  const leave = () => {
    pointer = null;
    delete node.dataset.pointer;
    wake();
  };
  const click = (event) => {
    const plate = event.target.closest?.('.plate');
    if (plate && node.contains(plate)) celebrate(plate, { count: 8 });
  };

  const resized = new ResizeObserver(() => shape(svg.clientHeight));
  resized.observe(node);
  const lively = !reducedMotion();
  if (lively) {
    node.addEventListener('pointermove', move);
    node.addEventListener('pointerleave', leave);
    node.addEventListener('click', click);
  }
  return {
    destroy() {
      resized.disconnect();
      cancelAnimationFrame(frame);
      if (!lively) return;
      node.removeEventListener('pointermove', move);
      node.removeEventListener('pointerleave', leave);
      node.removeEventListener('click', click);
    },
  };
}
