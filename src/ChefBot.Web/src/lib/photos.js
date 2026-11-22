// Photos chosen on this device, read the way the API takes them: base64 bytes in JSON.

/** The largest photo ChefBot keeps, as the server checks it. */
export const MAX_PHOTO_MB = 5;

/** The file's bytes as base64. */
export function base64(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result).split(',', 2)[1] ?? '');
    reader.onerror = () => reject(new Error(`${file.name} couldn't be read. Choose the file again.`));
    reader.readAsDataURL(file);
  });
}

/**
 * The photo made smaller for an AI model to read, as base64 JPEG: at most `longest` pixels on its longer side.
 * Phone photos are often over 5 MB and larger than models look at anyway. A file the browser can't draw is sent as it
 * is, and the server says whether it can use it.
 */
export async function shrinkPhoto(file, longest = 1600) {
  let bitmap;
  try {
    bitmap = await createImageBitmap(file);
  } catch {
    return base64(file);
  }
  const scale = Math.min(1, longest / Math.max(bitmap.width, bitmap.height));
  const canvas = document.createElement('canvas');
  canvas.width = Math.round(bitmap.width * scale);
  canvas.height = Math.round(bitmap.height * scale);
  canvas.getContext('2d').drawImage(bitmap, 0, 0, canvas.width, canvas.height);
  bitmap.close();
  const blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.85));
  return blob ? base64(blob) : base64(file);
}
