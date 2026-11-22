import { api } from './api.js';
import { app, run } from './state.svelte.js';

// ---------- reviews ----------
// Each answers the recipe's reviews and rating ({ rating, reviews }, RecipeReviewsResponse), or null when it failed
// and run() showed why. Reviews aren't kept for later while offline: they just fail like other recipe changes.

/** A recipe's reviews, newest first, with its rating. */
export async function loadReviews(recipeId) {
  let answer = null;
  await run(async () => { answer = await api('GET', `/api/recipes/${recipeId}/reviews`); });
  return answer;
}

/** Saves the signed-in person's review: { stars, notes }. With `reviewId` it changes that one; without, it adds one. */
export async function saveReview(recipeId, review, reviewId = null) {
  let answer = null;
  await run(async () => {
    answer = await api(reviewId ? 'PUT' : 'POST', `/api/recipes/${recipeId}/reviews${reviewId ? `/${reviewId}` : ''}`, review);
    app.recipesChanged++; // the library shows the new average
  });
  return answer;
}

/** Deletes a review: the signed-in person's own, or anyone's for an admin. */
export async function removeReview(recipeId, reviewId) {
  let answer = null;
  await run(async () => {
    answer = await api('DELETE', `/api/recipes/${recipeId}/reviews/${reviewId}`);
    app.recipesChanged++;
  });
  return answer;
}

/** "4.5 out of 5 from 2 reviews", for a rating's accessible name. */
export const ratingText = (rating) =>
  `Rated ${rating.average} out of 5 from ${rating.count} review${rating.count === 1 ? '' : 's'}`;
