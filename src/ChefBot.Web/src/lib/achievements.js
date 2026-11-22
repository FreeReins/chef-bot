// The wording of the kitchen achievements. The server says what was reached (AchievementsResponse); this names it.
// Nothing here may depend on how many people are in the household: one person cooking for themselves is a full
// kitchen. Locked badges say what's next, never what's missing, and nothing is ever lost (a streak is the best run).

/** The first steps, in the order the server sends them, with where to do each (`href`) and the link's words (`cta`). */
export const STEPS = {
  FirstRecipe: {
    emoji: '📖', title: 'Stock the cookbook', href: '/recipes/new/link', cta: 'Add a recipe',
    hint: 'Add a recipe from a link, a file, or straight from memory.',
    done: 'The cookbook has its first page.',
  },
  FirstPlan: {
    emoji: '🗓️', title: 'Plan a week', href: '/plan', cta: 'Plan a week',
    hint: 'Let ChefBot deal a week of meals, then shuffle until it looks tasty.',
    done: 'A week of meals, dealt.',
  },
  PlanConfirmed: {
    emoji: '✅', title: 'Commit to the menu', href: '/plan', cta: 'Review the draft',
    hint: 'Happy with the draft? Confirm it, and the shopping list becomes real.',
    done: 'The menu is locked in.',
  },
  FirstShop: {
    emoji: '🛒', title: 'Do the big shop', href: '/shopping', cta: 'Go shopping',
    hint: 'Tick off everything on the shopping list in one go.',
    done: 'The fridge thanks you.',
  },
  FirstCook: {
    emoji: '🍳', title: 'Cook something', href: '/plan', cta: 'Open the week',
    hint: 'Mark a meal as cooked on the week board. Eating it is encouraged.',
    done: 'Something delicious happened.',
  },
  FirstFavourite: {
    emoji: '❤️', title: 'Pick a favourite', href: '/recipes', cta: 'Browse recipes',
    hint: "Press the heart on a recipe you'd happily eat every week.",
    done: 'A favourite is crowned.',
  },
  FirstGuest: {
    emoji: '🪑', title: 'Pull up a chair', href: '/people', cta: 'Invite someone',
    hint: 'Cooking for more than one? Invite them to the kitchen. Cooking for one is a full kitchen too.',
    done: 'Someone pulled up a chair.',
  },
};

/** The badges: a name and a quip for bronze, silver and gold, a teaser before the first, and what they count. */
export const BADGES = {
  Cookbook: {
    emoji: '📚', unit: ['recipe', 'recipes'],
    names: ['Recipe Collector', 'Cookbook Hoarder', 'Walking Library'],
    teaser: 'Every great cookbook starts with a blank page.',
    quips: ['A respectable shelf of recipes.', 'The shelf is groaning.', 'You could open a restaurant. Or just keep this one.'],
  },
  Dinners: {
    emoji: '🍽️', unit: ['dinner', 'dinners'],
    names: ['Regular Cook', 'Weeknight Hero', 'Dinner Machine'],
    teaser: 'Dinners marked as cooked land here.',
    quips: ['Dinner is served, reliably.', 'Saving weeknights, one pan at a time.', 'The stove has never been so warm.'],
  },
  Repertoire: {
    emoji: '🎨', unit: ['different recipe', 'different recipes'],
    names: ['Dabbler', 'Range Finder', 'Culinary Polymath'],
    teaser: 'Every new recipe you cook adds a colour to the palette.',
    quips: ['A little of this, a little of that.', 'Not a one-trick pony.', "There's no dish you won't try."],
  },
  WeeksPlanned: {
    emoji: '🗓️', unit: ['week planned', 'weeks planned'],
    names: ['Planner', 'Calendar Whisperer', 'Year of Plenty'],
    teaser: 'Each week you plan adds a page to the calendar.',
    quips: ['A month of knowing what\'s for dinner.', 'The calendar does what you say.', 'A whole year, planned. Legendary.'],
  },
  Hauls: {
    emoji: '🛍️', unit: ['big shop', 'big shops'],
    names: ['Cart Pusher', 'Aisle Navigator', 'Supermarket Sweeper'],
    teaser: 'Buy a whole shopping list in one go and it counts here.',
    quips: ['The trolley wobbles, but it obeys.', 'You know where the capers are.', 'In and out with everything. Every time.'],
  },
  BestStreak: {
    emoji: '🔥', unit: ['week in a row', 'weeks in a row'],
    names: ['Warming Up', 'On a Roll', 'Unstoppable Stove'],
    teaser: 'Cook in weeks back to back. Your best run is kept for good.',
    quips: ['The pilot light is on.', 'Week after week after week.', 'Half a year of good food, back to back.'],
  },
  Comebacks: {
    emoji: '🪃', unit: ['comeback', 'comebacks'],
    names: ['Nostalgic', 'Revival Act', 'Greatest Hits DJ'],
    teaser: 'Bring back a recipe you haven\'t cooked in three months.',
    quips: ['An old friend came back to the table.', 'The classics never die.', 'Every night is a reunion tour.'],
  },
  Critic: {
    emoji: '⭐', unit: ['review', 'reviews'],
    names: ['Armchair Critic', 'Food Columnist', 'Michelin Inspector'],
    teaser: 'Review a recipe after cooking it: stars, notes, the lot.',
    quips: ['Opinions have been served.', 'Your column is widely read.', 'The chefs tremble when you pick up a fork.'],
  },
  Favourites: {
    emoji: '💘', unit: ['favourite', 'favourites'],
    names: ['Smitten', 'Big Softie', 'Heart of the Kitchen'],
    teaser: 'Press the heart on the recipes you love.',
    quips: ['Love at first bite.', 'Easily wooed by a good sauce.', 'Your heart belongs to the kitchen.'],
  },
};

export const MEDALS = ['bronze', 'silver', 'gold'];

const MEDAL_EMOJI = ['🥉', '🥈', '🥇'];

/** The chef ranks, by the server's Rank (0 to 5). */
export const RANKS = [
  { name: 'Dishwasher', quip: 'Everyone starts at the sink. The best chefs did too.' },
  { name: 'Prep Cook', quip: 'Trusted with a knife. Mostly.' },
  { name: 'Line Cook', quip: 'Yes, chef! The tickets are flowing.' },
  { name: 'Sous Chef', quip: 'Second in command of a very tidy kitchen.' },
  { name: 'Head Chef', quip: 'Your kitchen, your rules, your menu.' },
  { name: 'Legend of the Pass', quip: "They'll name a sauce after you." },
];

/** How the first steps are going, from how many of the six are done. */
export function questLine(done, total) {
  if (done === 0) return 'Every kitchen starts with an empty counter.';
  if (done >= total) return "You've earned your apron.";
  if (done === total - 1) return 'One more and the apron is yours.';
  return done * 2 < total ? 'The kitchen is warming up.' : 'Something smells good in here.';
}

/** A number with thousands separators, for points. */
export const points = (value) => value.toLocaleString();

const unit = (badge, count) => badge.unit[count === 1 ? 0 : 1];

/** A badge's name: the tier reached, or the bronze one it is working towards. */
export const badgeName = (achievement) => BADGES[achievement.kind].names[Math.max(achievement.tier, 1) - 1];

/** What a badge has counted, such as "37 dinners". */
export const badgeCount = (achievement) => `${achievement.progress.toLocaleString()} ${unit(BADGES[achievement.kind], achievement.progress)}`;

/** The quip for the tier reached, or the teaser before the first. */
export const badgeQuip = (achievement) =>
  achievement.tier === 0 ? BADGES[achievement.kind].teaser : BADGES[achievement.kind].quips[achievement.tier - 1];

/** What the next tier asks for, such as "3 more dinners to Weeknight Hero", or null once gold. */
export function badgeNext(achievement) {
  if (achievement.goal == null) return null;
  const badge = BADGES[achievement.kind];
  const left = achievement.goal - achievement.progress;
  return `${left} more ${unit(badge, left)} to ${badge.names[achievement.tier]}`;
}

/**
 * The toasts for what was reached since `seen` ({ kind: tier, rank }): one per new badge tier or step, and one for a
 * new rank. Many at once (a big import) are folded into one, so the corner isn't flooded.
 */
export function unlockMessages(next, seen) {
  const messages = [];
  for (const step of next.starter) {
    if (step.tier > (seen[step.kind] ?? 0)) messages.push(`${STEPS[step.kind].emoji} ${STEPS[step.kind].title}: done! ${STEPS[step.kind].done}`);
  }
  for (const badge of next.badges) {
    if (badge.tier > (seen[badge.kind] ?? 0)) {
      messages.push(`${MEDAL_EMOJI[badge.tier - 1]} New ${MEDALS[badge.tier - 1]} badge: ${badgeName(badge)}. ${badgeQuip(badge)}`);
    }
  }
  if (messages.length > 3) {
    messages.splice(0, messages.length, `🏅 ${messages.length} new achievements! See them on the Achievements page.`);
  }
  if (next.rank > (seen.rank ?? 0)) messages.push(`👨‍🍳 Promoted to ${RANKS[next.rank].name}! ${RANKS[next.rank].quip}`);
  return messages;
}
