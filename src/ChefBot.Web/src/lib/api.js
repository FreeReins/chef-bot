/**
 * Calls ChefBot's JSON API. Throws an Error carrying the server's message and `status` when the request fails.
 */
export async function api(method, path, body) {
  const response = await fetch(path, {
    method,
    headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { /* not JSON */ }
  if (!response.ok) {
    const error = new Error(data?.detail || data?.title || `The request failed (${response.status}).`);
    error.status = response.status;
    throw error;
  }
  return data;
}
