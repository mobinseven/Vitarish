import { chromium } from "playwright";
import { mkdir, writeFile } from "node:fs/promises";
import { resolve } from "node:path";
import assert from "node:assert/strict";

// Capture the real shared component; never replace or slow its CSS for recording.
const [url, outputDirectory] = process.argv.slice(2);
if (!url || !outputDirectory) throw new Error("Usage: node scripts/record-viewport-reveal.mjs <url> <output-directory>");
const output = resolve(outputDirectory);
await mkdir(output, { recursive: true });
const browser = await chromium.launch();
try {
  const context = await browser.newContext({
    viewport: { width: 1280, height: 800 },
    reducedMotion: "no-preference",
    recordVideo: { dir: output, size: { width: 1280, height: 800 } },
  });
  const page = await context.newPage();
  await page.goto(url, { waitUntil: "networkidle" });
  const reveal = page.locator("[data-vitarish-viewport-reveal]");
  await reveal.waitFor({ state: "attached" });
  await page.waitForFunction(() => document.querySelector("[data-vitarish-viewport-reveal-enhanced]"));
  // Stage just before entry, then make one natural scroll gesture and let it settle.
  await reveal.evaluate(element => window.scrollTo(0, element.getBoundingClientRect().top + scrollY - innerHeight + 30));
  await page.waitForTimeout(1200);
  const samples = await reveal.evaluate(async element => {
    const frames = [];
    const start = performance.now();
    window.scrollBy({ top: 430, behavior: "smooth" });
    await new Promise(done => {
      function sample(now) {
        const style = getComputedStyle(element);
        frames.push({ ms: now - start, opacity: Number(style.opacity), y: new DOMMatrix(style.transform).m42 });
        if (now - start < 2000) requestAnimationFrame(sample);
        else done();
      }
      requestAnimationFrame(sample);
    });
    return frames;
  });
  assert(samples.some(frame => frame.opacity > 0.1 && frame.opacity < 0.9), "Fade must contain intermediate frames");
  assert(samples.some(frame => frame.y > 0 && frame.y < 24), "Translation must settle continuously");
  assert.equal(samples.at(-1).opacity, 1);
  assert.equal(samples.at(-1).y, 0);
  await page.waitForTimeout(1800);
  const video = page.video();
  await context.close();
  await video.saveAs(resolve(output, "viewport-reveal-refined.webm"));
  await video.delete();
  await writeFile(resolve(output, "motion-samples.json"), JSON.stringify(samples, null, 2));
  for (const reducedMotion of ["reduce", "no-preference"]) {
    const fallback = await browser.newContext({ reducedMotion, javaScriptEnabled: reducedMotion === "reduce" });
    const fallbackPage = await fallback.newPage();
    await fallbackPage.goto(url, { waitUntil: "networkidle" });
    const opacity = await fallbackPage.locator("[data-vitarish-viewport-reveal]").evaluate(element => getComputedStyle(element).opacity);
    assert.equal(opacity, "1", `${reducedMotion} fallback must remain visible`);
    await fallback.close();
  }
  console.log(`Recorded and checked ${samples.length} frames: ${output}`);
} finally {
  await browser.close();
}
