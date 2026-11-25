// Helpers for cooking mode (tabs/RecipeCook.svelte) that hold no state of their own.

/** The list with the index added, or taken out if it was already there. */
export const toggle = (list, index) => (list.includes(index) ? list.filter((item) => item !== index) : [...list, index]);

/**
 * The step's own words cut around each time it gives, so each time can be shown as a button that starts a timer.
 * A time not found in the words (the text changed) is marked loose and shown after them.
 */
export function partsOf(step) {
  const found = [];
  let from = 0;
  for (const timer of step.timers) {
    const index = step.text.indexOf(timer.text, from);
    if (index < 0) { found.push({ timer, loose: true }); continue; }
    found.push({ text: step.text.slice(from, index) }, { timer });
    from = index + timer.text.length;
  }
  found.push({ text: step.text.slice(from) });
  return found;
}

/** "4:05", or "1:04:05" past an hour. */
export function clock(ms) {
  const total = Math.ceil(ms / 1000);
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const seconds = String(total % 60).padStart(2, '0');
  return hours ? `${hours}:${String(minutes).padStart(2, '0')}:${seconds}` : `${minutes}:${seconds}`;
}

/** Three short beeps on the given AudioContext, if there is one. */
export function chime(audio) {
  if (!audio) return;
  try {
    const start = audio.currentTime;
    for (const offset of [0, 0.25, 0.5]) {
      const tone = audio.createOscillator();
      const volume = audio.createGain();
      tone.frequency.value = 880;
      volume.gain.setValueAtTime(0.0001, start + offset);
      volume.gain.exponentialRampToValueAtTime(0.3, start + offset + 0.02);
      volume.gain.exponentialRampToValueAtTime(0.0001, start + offset + 0.2);
      tone.connect(volume).connect(audio.destination);
      tone.start(start + offset);
      tone.stop(start + offset + 0.22);
    }
  } catch { /* no sound; the timer still shows and vibrates */ }
}
