/**
 * Calls ChefBot's JSON API. Throws an Error carrying the server's message and `status` when the request fails.
 */
export async function api(method, path, body) {
  return answer(await fetch(path, {
    method,
    headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  }));
}

/**
 * Sends a file to ChefBot's API as the raw request body, such as a backup .zip, and returns the JSON answer.
 * Fails the same way as `api`.
 */
export async function upload(path, file, contentType) {
  return answer(await fetch(path, { method: 'POST', headers: { 'Content-Type': contentType }, body: file }));
}

/** The response's JSON, or an Error carrying the server's message and `status` when the request failed. */
async function answer(response) {
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { /* not JSON */ }
  if (!response.ok) {
    const error = new Error(data?.detail || data?.title || failure(response.status));
    error.status = response.status;
    throw error;
  }
  return data;
}

function failure(status) {
  return status === 413 ? 'That file is too large to send to ChefBot.' : `The request failed (${status}).`;
}
