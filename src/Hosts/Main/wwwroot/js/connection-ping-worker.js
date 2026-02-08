// Pings a health endpoint with jittered backoff; posts 'health-up' when 200 OK is observed.
const ctx = self;
const HEALTH_URL = '/health'; // Override at build-time if you like

// Simple jittered backoff
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const backoff = (n) => Math.floor(Math.random() * Math.min(30000, 500 * 2 ** n));

(async () => {
  let attempt = 0;
  // Loops forever; main page can ignore messages when not reconnecting.
  for (;;) {
    try {
      const res = await fetch(HEALTH_URL, { credentials: 'same-origin', cache: 'no-store' });
      if (res.ok) ctx.postMessage({ type: 'health-up' });
    } catch { /* ignore */ }
    await sleep(backoff(attempt++));
  }
})();
