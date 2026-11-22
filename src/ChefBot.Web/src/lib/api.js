/**
 * Calls ChefBot's JSON API. Throws an Error carrying the server's message and `status` when the request fails, or one
 * with `offline` set when ChefBot can't be reached at all.
 */
export async function api(method, path, body) {
  return answer(await reach(path, {
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
  return answer(await reach(path, { method: 'POST', headers: { 'Content-Type': contentType }, body: file }));
}

/** fetch(), except that a request that never reached ChefBot (no signal, the server is off) fails with `offline`. */
async function reach(path, init) {
  try {
    return await fetch(path, init);
  } catch {
    const error = new Error("ChefBot can't be reached.");
    error.offline = true;
    throw error;
  }
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
