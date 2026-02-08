// Top-tier Blazor connection manager with jittered backoff, online/offline & visibility awareness,
// cross-tab coordination, optional worker-based health pings — now with a console watcher that
// detects the specific SignalR message "Failed to complete negotiation with the server"
// **and** the message "Information: Connection disconnected." and uses them as triggers to
// aggressively reconnect or refresh.

// ===== Utilities =====
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/** Full-jitter backoff: random delay in [0, min(cap, base*2^attempt)] */
function jitterBackoff(attempt, { base = 500, cap = 30000 } = {}) {
  const max = Math.min(cap, base * Math.pow(2, attempt));
  return Math.floor(Math.random() * max);
}

// Promise that resolves when the browser comes back online
function waitForOnline() {
  if (navigator.onLine) return Promise.resolve();
  return new Promise((resolve) => {
    const on = () => { window.removeEventListener('online', on); resolve(); };
    window.addEventListener('online', on, { once: true });
  });
}

// ===== Console watchers for specific connection errors =====
const NEGOTIATION_ERR_PATTERN = /Failed to complete negotiation with the server/i;
const DISCONNECTED_INFO_PATTERN = /Information:\s*Connection disconnected\.?/i;

class ConsoleNegotiationWatcher {
  constructor({ pattern, onTrigger, log = (...a) => console.debug('[ConnWatch]', ...a) } = {}) {
    this.pattern = pattern instanceof RegExp ? pattern : new RegExp(String(pattern), 'i');
    this.onTrigger = onTrigger;
    this.log = log;
    this._installed = false;
    this._fired = false;
    this._orig = {};
    this._onRejection = (ev) => {
      const msg = ev?.reason?.message ?? String(ev?.reason ?? '');
      this._inspect([msg], 'unhandledrejection');
    };
    this._onError = (ev) => {
      const msg = ev?.message ?? '';
      this._inspect([msg], 'error');
    };
  }

  install() {
    if (this._installed) return;
    this._installed = true;
    ['error', 'warn', 'info', 'log'].forEach((m) => this._wrap(m));
    window.addEventListener('unhandledrejection', this._onRejection);
    window.addEventListener('error', this._onError);
    this.log('ConsoleNegotiationWatcher installed.');
  }

  uninstall() {
    if (!this._installed) return;
    ['error', 'warn', 'info', 'log'].forEach((m) => { if (this._orig[m]) console[m] = this._orig[m]; });
    window.removeEventListener('unhandledrejection', this._onRejection);
    window.removeEventListener('error', this._onError);
    this._installed = false;
    this.log('ConsoleNegotiationWatcher uninstalled.');
  }

  _wrap(methodName) {
    const orig = console[methodName];
    this._orig[methodName] = orig;
    const self = this;
    console[methodName] = function(...args) {
      try { self._inspect(args, `console.${methodName}`); } catch { /* noop */ }
      return orig.apply(console, args);
    };
  }

  _stringifyArg(a) {
    if (typeof a === 'string') return a;
    if (a && typeof a.message === 'string') return a.message;
    try { return JSON.stringify(a); } catch { return String(a); }
  }

  _inspect(args, source = 'console') {
    if (this._fired) return;
    const text = args.map((a) => this._stringifyArg(a)).join(' ');
    if (this.pattern.test(text)) {
      this._fired = true; // fire once per pageload
      this.log(`Matched connection pattern from ${source}:`, text);
      try { this.onTrigger?.(text, source); } catch { /* noop */ }
    }
  }
}

// ===== Cross-tab coordination (so dozens of tabs don’t stampede the server) =====
// BroadcastChannel is widely supported and works with workers too.
const channelName = 'blazor-conn';
const bc = ('BroadcastChannel' in self) ? new BroadcastChannel(channelName) : null;
// Messages: { type: 'down'|'up'|'reloaded'|'attempt'|'negfail', attempt?:number }

class BlazorConnectionManager {
  constructor(options = {}) {
    this.opts = {
      maxRetries: 300,
      baseDelayMs: 500,
      maxDelayMs: 30000,
      healthUrl: '/health/live',           // optional, for Worker ping
      useWorkerPing: true,            // set true if you have a health endpoint
      log: (...args) => console.debug('[ConnMgr]', ...args),
      ...options
    };
    this._reconnectAbort = null;
    this._reconnecting = false;
    this._worker = null;
    this._negRecovery = false; // cold-start recovery flag when negotiation fails

    // Visibility-aware backoff multiplier: longer delays when the tab is hidden.
    this._getVisibilityFactor = () => document.visibilityState === 'hidden' ? 2 : 1;

    // Cross-tab messages
    if (bc) {
      bc.addEventListener('message', (ev) => {
        const msg = ev.data || {};
        if (msg.type === 'up') this._cancelReconnection('another tab is up');
        if (msg.type === 'reloaded') this._cancelReconnection('another tab reloaded');
        if (msg.type === 'negfail') this._handleNegotiationFailure('peer');
      });
    }

    // Graceful PWA install prompt storage (optional)
    window.addEventListener('beforeinstallprompt', (e) => {
      e.preventDefault();
      window.installprompt = e; // same behavior you had
    });

    // Online/offline events (MDN-recommended)
    window.addEventListener('offline', () => this._showReconnectUI());
    window.addEventListener('online', () => this._hideReconnectUI());

    // Install console watchers that trigger recovery if certain messages appear
    this._consoleWatcherNeg = new ConsoleNegotiationWatcher({
      pattern: NEGOTIATION_ERR_PATTERN,
      onTrigger: () => this._handleNegotiationFailure('console'),
      log: (...a) => this.opts.log('[ConsoleWatch]', ...a)
    });
    this._consoleWatcherNeg.install();

    this._consoleWatcherDisc = new ConsoleNegotiationWatcher({
      pattern: DISCONNECTED_INFO_PATTERN,
      onTrigger: () => this._handleConsoleDisconnect('console'),
      log: (...a) => this.opts.log('[ConsoleWatch]', ...a)
    });
    this._consoleWatcherDisc.install();
  }

  async startBlazor() {
    // Optional worker: ping health endpoint in background to know when the server wakes up.
    if (this.opts.useWorkerPing && typeof Worker !== 'undefined') {
      const src = await (await fetch('/js/connection-ping-worker.js')).text();
      const blob = new Blob([src], { type: 'text/javascript' });
      this._worker = new Worker(URL.createObjectURL(blob), { type: 'module' });
      this._worker.onmessage = (e) => {
        if (e.data?.type === 'health-up') {
          this.opts.log('Health ping says server is up.');
          // If we were recovering from a negotiation failure or in reconnect loop, reload now to get a fresh circuit
          if (this._negRecovery || this._reconnecting) {
            this.opts.log('Reloading immediately due to health-up during recovery.');
            bc?.postMessage({ type: 'reloaded' });
            location.reload();
          }
        }
      };
      // Tell worker what to ping (if your worker expects it).
      try { this._worker.postMessage({ type: 'config', url: this.opts.healthUrl }); } catch { /* noop */ }
    }

    // Start Blazor with a custom reconnectionHandler (supported option).
    // See: docs for manual Blazor start and reconnection events. 
    // - onConnectionDown: fire when SignalR disconnects
    // - onConnectionUp: fire when (re)connected
    // (These hooks are part of the documented startup options.)
    try {
      // eslint-disable-next-line no-undef
      await Blazor.start({
        circuit: {
          reconnectionHandler: {
            onConnectionDown: (options, error) => {
              this.opts.log('Blazor connection down', error);
              bc?.postMessage({ type: 'down' });
              this._showReconnectUI();
              return this._startReconnectionProcess(); // must return { cancel() }-like
            },
            onConnectionUp: () => {
              this.opts.log('Blazor connection up');
              bc?.postMessage({ type: 'up' });
              this._hideReconnectUI();
              this._cancelReconnection('onConnectionUp');
            }
          }
        }
      });
    } catch (startErr) {
      // If startup fails (often with the negotiation error), kick off the same recovery path.
      this.opts.log('Blazor.start failed; entering negotiation failure recovery.', startErr);
      this._handleNegotiationFailure('start-catch');
    }
  }

  // ===== Reconnection loop (used when a circuit once existed) =====
  _startReconnectionProcess() {
    if (this._reconnecting) return this._reconnectProcess;
    this._reconnecting = true;
    this._reconnectAbort = new AbortController();

    const reconnectionLoop = (async () => {
      for (let attempt = 0; attempt < this.opts.maxRetries; attempt++) {
        if (this._reconnectAbort.signal.aborted) return;

        // If offline, don’t hammer the server; wait for ‘online’ first.
        if (!navigator.onLine) {
          this.opts.log('Offline; waiting for online to resume reconnect attempts.');
          await waitForOnline(); // resolves when online again
        }

        // Backoff with full jitter. Slow down if the tab is hidden.
        const delay = jitterBackoff(attempt, {
          base: this.opts.baseDelayMs * this._getVisibilityFactor(),
          cap: this.opts.maxDelayMs
        });
        this.opts.log(`Reconnect attempt #${attempt + 1} (waiting ${delay}ms)`);
        bc?.postMessage({ type: 'attempt', attempt });
        await sleep(delay);
        if (this._reconnectAbort.signal.aborted) return;

        try {
          // eslint-disable-next-line no-undef
          const ok = await Blazor.reconnect();
          if (ok) {
            this.opts.log('Reconnected successfully.');
            return;
          }
          // Server reachable but circuit rejected (stale); reload is the supported fix.
          this.opts.log('Server reached but circuit rejected; reloading.');
          bc?.postMessage({ type: 'reloaded' });
          location.reload();
          return;
        } catch (err) {
          // Didn’t reach server -> keep trying.
          this.opts.log('Reconnect attempt failed (network/server not reachable).', err);
        }
      }

      // Max retries exceeded — reload to rehydrate a fresh circuit.
      this.opts.log('Max retries exceeded; reloading.');
      bc?.postMessage({ type: 'reloaded' });
      location.reload();
    })();

    this._reconnectProcess = {
      cancel: () => this._cancelReconnection('explicit cancel'),
      _task: reconnectionLoop
    };
    return this._reconnectProcess;
  }

  // ===== Negotiation-failure (cold start) recovery =====
  _handleNegotiationFailure(origin = 'console') {
    if (this._negRecovery) return; // already handling it
    this._negRecovery = true;
    this.opts.log(`Negotiation failure detected via ${origin}; entering cold recovery.`);
    this._showReconnectUI();
    bc?.postMessage({ type: 'negfail' });

    // Try a best-effort reconnect if API is available — sometimes a transient race.
    (async () => {
      try {
        // eslint-disable-next-line no-undef
        if (typeof Blazor !== 'undefined' && typeof Blazor.reconnect === 'function') {
          const ok = await Blazor.reconnect();
          if (ok) {
            this.opts.log('Recovered via Blazor.reconnect after negotiation failure.');
            this._negRecovery = false;
            return;
          }
        }
      } catch { /* ignore and fall through to reload */ }

      // If offline, pause until connectivity resumes
      if (!navigator.onLine) {
        this.opts.log('Offline during negotiation recovery; waiting for online.');
        await waitForOnline();
      }

      // If a health worker is active, give it a short window (1–3s jitter) to report healthy before reloading
      const delay = jitterBackoff(1, { base: 1000, cap: 3000 });
      this.opts.log(`Scheduling page reload in ~${delay}ms (cold recovery).`);
      setTimeout(() => {
        try { bc?.postMessage({ type: 'reloaded' }); } catch { /* noop */ }
        location.reload();
      }, delay);
    })();
  }

  // ===== Console-driven (runtime) disconnect recovery =====
  async _handleConsoleDisconnect(origin = 'console') {
    if (this._reconnecting) return; // already on it
    this.opts.log(`Disconnect message detected via ${origin}; attempting immediate reconnect.`);
    this._showReconnectUI();
    bc?.postMessage({ type: 'down' });

    try {
      // eslint-disable-next-line no-undef
      if (typeof Blazor !== 'undefined' && typeof Blazor.reconnect === 'function') {
        const ok = await Blazor.reconnect();
        if (ok) {
          this.opts.log('Immediate reconnect succeeded after disconnect message.');
          this._negRecovery = false;
          this._hideReconnectUI();
          return;
        }
      }
    } catch (e) {
      this.opts.log('Immediate reconnect threw; entering reconnection loop.', e);
    }

    // Fall back to the regular reconnection loop (with backoff & eventual reload).
    this._startReconnectionProcess();
  }

  _cancelReconnection(reason = '') {
    if (!this._reconnecting) return;
    this.opts.log('Cancelling reconnection', reason);
    this._reconnecting = false;
    this._reconnectAbort?.abort();
    this._reconnectAbort = null;
    this._reconnectProcess = null;
  }

  _showReconnectUI() {
    // Toggle your custom modal if present
    const m1 = document.getElementById('reconnect-modal');
    if (m1) m1.style.opacity = 1;
  }

  _hideReconnectUI() {
    const m1 = document.getElementById('reconnect-modal');
    if (m1) m1.style.opacity = 0;
  }
}

// Boot when DOM is ready (after blazor.web.js is loaded with autostart=false)
// and install the connection failure watchers immediately so we can catch startup failures.
document.addEventListener('DOMContentLoaded', async () => {
  // Prefer slower backoff when the tab is backgrounded (visibility API).
  document.addEventListener('visibilitychange', () => { /* hook kept for clarity */ });

  const mgr = new BlazorConnectionManager({
    maxRetries: 300,
    baseDelayMs: 750,    // start gentle to avoid thundering herds
    maxDelayMs: 30000,
    useWorkerPing: true, // set true if you provide /health
    healthUrl: '/health' // ensure your worker pings this
  });

  await mgr.startBlazor();
});
