// Attaches to a <input type="file">, computes SHA-256 checksums on each change,
// creates optional base64 thumbnails for image/* and video/* files, and notifies a .NET instance
// method with an array of PendingFileDto-shaped objects.
//
// PendingFileDto in .NET:
// public sealed record PendingFileDto(string Name, long Size, string Type, string Checksum, string Base64Thumbnail);

const _fileInputStates = new WeakMap();

export function ensureFileInput(options = {}) {
    const { inputId, dotNetRef, accept, capture, hidden, multiple = true, algorithm, thumbnail } = options;
    if (!inputId || !dotNetRef) {
        throw new Error("ensureFileInput: inputId and dotNetRef are required.");
    }

    let input = document.getElementById(inputId);
    if (!(input instanceof HTMLInputElement) || input.type !== "file") {
        if (input && input.parentElement) {
            input.parentElement.removeChild(input);
        }
        input = document.createElement("input");
        input.type = "file";
    }

    document.body.appendChild(input);
    input.id = inputId;
    input.multiple = multiple !== false;
    if (typeof accept === "string") {
        input.setAttribute("accept", accept);
    } else {
        input.removeAttribute("accept");
    }

    if (typeof capture === "string" && capture.length > 0) {
        input.setAttribute("capture", capture);
    } else {
        input.removeAttribute("capture");
    }

    if (hidden) {
        input.style.display = "none";
        input.style.opacity = "";
    } else {
        input.style.display = "";
        input.style.opacity = "1";
    }

    let state = _fileInputStates.get(input);
    if (state?.changeHandler) {
        input.removeEventListener("change", state.changeHandler);
    }

    if (!state) {
        state = { generation: 0, disposed: false };
    }

    state.disposed = false;
    state.algorithm = algorithm || state.algorithm || "SHA-256";
    state.thumbnail = normalizeThumbnailOptions?.(thumbnail) ?? state.thumbnail ?? { enabled: false };

    const changeHandler = async () => {
        if (state.disposed) {
            return;
        }

        await inputChanged(input, dotNetRef, {
            algorithm: state.algorithm,
            thumbnail: state.thumbnail
        });
    };

    state.changeHandler = changeHandler;
    _fileInputStates.set(input, state);

    input.addEventListener("change", changeHandler);
}

/**
 * Direct onchange handler for <input type="file"> elements.
 * Can be called from Blazor via JS interop:
 *   await module.InvokeVoidAsync("inputChanged", elementRef, dotNetRef, "OnPendingFilesChanged", { algorithm: "SHA-256" })
 *
 * @param {HTMLInputElement|string} inputElOrId - The <input type="file"> element (or its id).
 * @param {DotNetObject} dotNetRef - DotNetObjectReference with invokeMethodAsync.
 * @param {object} [options] - { algorithm?: string, thumbnail?: object, callbackMethod?: string }
 */
export async function inputChanged(inputElOrId, dotNetRef, options = {}) {
    const inputEl = resolveInputEl(inputElOrId);
    if (!(inputEl instanceof HTMLInputElement) || inputEl.type !== "file") {
        throw new Error("inputChanged: first argument must be an <input type=\"file\"> element or its id.");
    }
    if (!dotNetRef || typeof dotNetRef.invokeMethodAsync !== "function") {
        throw new Error("inputChanged: dotNetRef must be a DotNetObjectReference (with invokeMethodAsync).");
    }
    let callbackMethod = options.callbackMethod || "OnPendingFilesChanged";
    const settings = {
        algorithm: options.algorithm || "SHA-256",
        thumbnail: normalizeThumbnailOptions?.(options.thumbnail) ?? { enabled: false },
    };

    // Initialize or fetch existing state for this input element
    let state = _fileInputStates.get(inputEl);
    if (!state) {
        state = { generation: 0, disposed: false, algorithm: settings.algorithm, thumbnail: settings.thumbnail };
        _fileInputStates.set(inputEl, state);
    } else {
        // Keep latest algorithm/thumbnail options updated per call
        state.algorithm = settings.algorithm;
        state.thumbnail = settings.thumbnail;
    }

    if (state.disposed) return; // no-op if already disposed

    // Bump generation so any previous in-flight work will be ignored when it completes
    const myGen = ++state.generation;

    const fileList = inputEl.files ? Array.from(inputEl.files) : [];
    const payload = [];

    for (const file of fileList) {
        const [checksum, base64Thumb] = await Promise.all([
            computeChecksum(file, state.algorithm),
            state.thumbnail?.enabled ? generateThumbnailBase64(file, state.thumbnail) : Promise.resolve("")
        ]);

        // If another change happened while processing, stop and ignore these results
        if (state.generation !== myGen || state.disposed) return;

        payload.push({
            // Use constructor/property names exactly as in PendingFileDto
            Name: file.name,
            Size: file.size,
            Type: typeof file.type === "string" ? file.type : "",
            Checksum: checksum,
            Thumbnail: base64Thumb
        });
    }

    if (state.generation === myGen && !state.disposed) {
        await dotNetRef.invokeMethodAsync(callbackMethod, payload);
    }
}

/**
 * Optional helper to clean up state for an input element when you no longer need it.
 * Safe to call multiple times.
 */
export function disposeFileInput(inputElOrId) {
    const el = resolveInputEl(inputElOrId);
    if (!(el instanceof HTMLInputElement)) {
        return;
    }

    const state = _fileInputStates.get(el);
    if (state?.changeHandler) {
        el.removeEventListener("change", state.changeHandler);
    }

    if (state) {
        state.disposed = true;
        _fileInputStates.delete(el);
    }

    document.body.removeChild(el);
}

function resolveInputEl(inputElOrId) {
    if (typeof inputElOrId === "string") {
        return document.getElementById(inputElOrId);
    }
    return inputElOrId;
}


/**
 * Remove a file from the input's FileList by name and notify .NET of the change.
 * @param {HTMLInputElement} inputEl
 * @param {string} name
 */
export function removeFile(inputElId, name) {
    const inputEl = resolveInputEl(inputElId);
    if (!(inputEl instanceof HTMLInputElement) || inputEl.type !== "file") {
        return;
    }
    const dt = new DataTransfer();
    const files = inputEl?.files ? Array.from(inputEl.files) : [];
    for (const file of files) {
        if (file.name !== name) {
            dt.items.add(file);
        }
    }
    inputEl.files = dt.files;
    const s = _fileInputStates.get(inputEl);
    if (s?.changeHandler) {
        s.changeHandler();
    }
}

export async function getFileStream(inputElId, name) {
    const inputEl = resolveInputEl(inputElId);
    const fileList = inputEl.files ? Array.from(inputEl.files) : [];
    const file = fileList.find(f => f.name === name);
    if (!file) return null;

    // Return an ArrayBuffer (or a typed array). Both are valid.
    // return await file.arrayBuffer();
    return new Uint8Array(await file.arrayBuffer());
}

/**
 * Convenience: hash a single File/Blob.
 * @param {File|Blob} file
 * @param {string} [algorithm="SHA-256"]
 * @returns {Promise<string>} hex-encoded checksum
 */
export async function hashFile(file, algorithm = "SHA-256") {
    return computeChecksum(file, algorithm);
}

/**
 * Create base64 thumbnails for the currently selected files in an <input type="file">.
 * Returns a map-like array of { name, base64 } for image/video files; other types return empty strings.
 *
 * @param {HTMLInputElement} inputEl
 * @param {Object} [thumbnailOptions] - Same shape as options.thumbnail in attachFileInput.
 * @returns {Promise<Array<{name:string, base64:string}>>}
 */
export async function getSelectedThumbnails(inputElId, thumbnailOptions = {}) {
    let inputEl = document.getElementById(inputElId);
    if (!(inputEl instanceof HTMLInputElement) || inputEl.type !== "file") {
        throw new Error("getSelectedThumbnails: inputEl must be an <input type=\"file\"> element.");
    }
    const opts = normalizeThumbnailOptions(thumbnailOptions);
    const files = inputEl.files ? Array.from(inputEl.files) : [];
    const results = [];
    for (const file of files) {
        const base64 = opts.enabled ? await generateThumbnailBase64(file, opts) : "";
        results.push({ name: file.name, base64 });
    }
    return results;
}

// ===================== internal helpers =====================

function normalizeThumbnailOptions(thumbnail) {
    const t = thumbnail || {};
    return {
        enabled: t.enabled !== false,
        maxEdge: Number.isFinite(t.maxEdge) && t.maxEdge > 0 ? Math.floor(t.maxEdge) : 192,
        mimeType: typeof t.mimeType === "string" && t.mimeType ? t.mimeType : "image/webp",
        quality: typeof t.quality === "number" ? Math.min(1, Math.max(0, t.quality)) : 0.8,
        stripDataUrlPrefix: t.stripDataUrlPrefix !== false ? false : t.stripDataUrlPrefix,
        captureTimeSec: (t.captureTimeSec === "auto" || typeof t.captureTimeSec === "number") ? t.captureTimeSec : "auto"
    };
}

async function computeChecksum(file, algorithm) {
    const cryptoObj = /** @type {SubtleCrypto | undefined} */ (globalThis.crypto && globalThis.crypto.subtle);
    if (!cryptoObj || typeof cryptoObj.digest !== "function") {
        console.warn("fileHasher: Web Crypto Subtle API not available; returning empty checksum.");
        return "";
    }
    // NOTE: This reads the entire file into memory. For very large files, consider chunking
    // with a streaming hash implementation once widely available, or perform hashing server-side.
    const buffer = await file.arrayBuffer();
    const hashBuffer = await cryptoObj.digest(algorithm, buffer);
    return toHex(new Uint8Array(hashBuffer));
}

function toHex(bytes) {
    let out = "";
    for (let i = 0; i < bytes.length; i++) {
        const b = bytes[i].toString(16);
        out += b.length === 1 ? "0" + b : b;
    }
    return out;
}

/**
 * Generate a base64 thumbnail for an image OR video File/Blob.
 * - image/*: draws the image respecting EXIF via createImageBitmap when available
 * - video/*: seeks to a capture time (auto or explicit) and draws the frame
 * @param {File|Blob} file
 * @param {{enabled:boolean,maxEdge:number,mimeType:string,quality:number,stripDataUrlPrefix:boolean,captureTimeSec:number|"auto"}} opts
 * @returns {Promise<string>} base64 (no data: prefix if stripDataUrlPrefix is true)
 */
async function generateThumbnailBase64(file, opts) {
    try {
        if (!opts.enabled) return "";
        const mime = typeof file.type === "string" ? file.type : "";
        if (mime.startsWith("image/")) {
            return await generateImageThumbnail(file, opts);
        }
        if (mime.startsWith("video/")) {
            return await generateVideoThumbnail(file, opts);
        }
        return ""; // thumbnails only for image/video
    } catch {
        return ""; // Fail-safe: never throw during thumbnail generation
    }
}

async function generateImageThumbnail(file, opts) {
    // Prefer ImageBitmap for speed + EXIF orientation handling when supported
    let bmp = null;
    try {
        if (typeof createImageBitmap === "function") {
            // imageOrientation: 'from-image' respects EXIF rotation in supporting browsers
            bmp = await createImageBitmap(file, { imageOrientation: "from-image" });
        }
    } catch { bmp = null; }

    let width, height, drawSource;
    if (bmp) {
        width = bmp.width; height = bmp.height; drawSource = bmp;
    } else {
        // Fallback to HTMLImageElement
        const url = URL.createObjectURL(file);
        try {
            const img = await loadImage(url);
            width = img.naturalWidth || img.width; height = img.naturalHeight || img.height; drawSource = img;
        } finally {
            URL.revokeObjectURL(url);
        }
    }

    if (!(width > 0 && height > 0)) return "";

    return await drawSourceToBase64(drawSource, width, height, opts);
}

async function generateVideoThumbnail(file, opts) {
    const url = URL.createObjectURL(file);
    try {
        const video = document.createElement("video");
        video.preload = "metadata";
        video.muted = true; // avoid autoplay restrictions if we ever play
        video.playsInline = true;
        video.src = url;

        // Wait for metadata to get dimensions & duration
        await once(video, "loadedmetadata", 4000);
        const vWidth = video.videoWidth;
        const vHeight = video.videoHeight;
        if (!(vWidth > 0 && vHeight > 0)) return "";

        // Decide capture time
        let t;
        if (opts.captureTimeSec === "auto") {
            const dur = Number.isFinite(video.duration) && video.duration > 0 ? video.duration : 0;
            t = Math.min(1, Math.max(0, dur * 0.1)); // 10% in, capped to 1s
        } else {
            t = Math.max(0, Number(opts.captureTimeSec || 0));
        }

        // Seek without playing
        if (video.readyState >= 2 && Math.abs(video.currentTime - t) < 0.01) {
            // already near desired time
        } else {
            // set currentTime and await seeked (with a timeout)
            try { video.currentTime = t; } catch { }
            await once(video, "seeked", 4000);
        }

        return await drawSourceToBase64(video, vWidth, vHeight, opts);
    } catch {
        return "";
    } finally {
        try { URL.revokeObjectURL(url); } catch { }
    }
}

async function drawSourceToBase64(drawSource, width, height, opts) {
    const scale = Math.min(1, opts.maxEdge / Math.max(width, height));
    const outW = Math.max(1, Math.round(width * scale));
    const outH = Math.max(1, Math.round(height * scale));

    // Use OffscreenCanvas when available to avoid layout dependencies
    const canvas = (typeof OffscreenCanvas !== "undefined") ? new OffscreenCanvas(outW, outH) : document.createElement("canvas");
    if (!(canvas instanceof OffscreenCanvas)) {
        canvas.width = outW; canvas.height = outH;
    }
    const ctx = /** @type {CanvasRenderingContext2D | OffscreenCanvasRenderingContext2D | null} */ (canvas.getContext("2d"));
    if (!ctx) return "";
    ctx.clearRect(0, 0, outW, outH);
    // Draw with high-quality scaling when supported
    try {
        ctx.imageSmoothingEnabled = true;
        // @ts-ignore - not in all TS lib versions
        ctx.imageSmoothingQuality = "high";
    } catch { }
    ctx.drawImage(drawSource, 0, 0, outW, outH);

    let dataUrl;
    if (canvas instanceof OffscreenCanvas) {
        const blob = await canvas.convertToBlob({ type: opts.mimeType, quality: opts.quality });
        dataUrl = await blobToDataURL(blob);
    } else {
        // HTMLCanvasElement
        // @ts-ignore quality is valid for toDataURL with lossy formats
        dataUrl = /** @type {HTMLCanvasElement} */(canvas).toDataURL(opts.mimeType, opts.quality);
    }

    if (typeof drawSource.close === "function") { try { drawSource.close(); } catch { } }

    if (typeof dataUrl !== "string" || !dataUrl.startsWith("data:")) return "";
    return opts.stripDataUrlPrefix ? dataUrl.split(",", 2)[1] ?? "" : dataUrl;
}

function loadImage(url) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.decoding = "async";
        img.onload = () => resolve(img);
        img.onerror = reject;
        img.src = url;
    });
}

function once(el, event, timeoutMs = 3000) {
    return new Promise((resolve, reject) => {
        const onOk = () => { cleanup(); resolve(); };
        const onErr = () => { cleanup(); reject(new Error(event + " failed")); };
        const timer = setTimeout(() => { cleanup(); reject(new Error(event + " timeout")); }, timeoutMs);
        const cleanup = () => {
            clearTimeout(timer);
            el.removeEventListener(event, onOk);
            el.removeEventListener("error", onErr);
            el.removeEventListener("abort", onErr);
        };
        el.addEventListener(event, onOk, { once: true });
        el.addEventListener("error", onErr, { once: true });
        el.addEventListener("abort", onErr, { once: true });
    });
}

function blobToDataURL(blob) {
    return new Promise((resolve, reject) => {
        const fr = new FileReader();
        fr.onload = () => resolve(/** @type {string} */(fr.result));
        fr.onerror = reject;
        fr.readAsDataURL(blob);
    });
}

// Optional: help GC if the input element is removed from DOM without explicit detach.
// Browsers will GC WeakMap entries when keys are unreachable. No action needed here.
