// Set up event handlers
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

const serverStatusCheckIntervalMs = 10000;
const serverStatusCheckTimeoutMs = 8000;
const maxServerFailuresBeforeReload = 6;
const autoReloadDelayMs = 2000;

let serverStatusIntervalId = null;
let consecutiveServerFailures = 0;
let reloadTimeoutId = null;

function handleReconnectStateChanged(event) {
    if (reloadTimeoutId) {
        clearTimeout(reloadTimeoutId);
        reloadTimeoutId = null;
    }

    if (event.detail.state === "show") {
        reconnectModal.showModal();
        retry();
    } else if (event.detail.state === "hide") {
        reconnectModal.close();
    } else if (event.detail.state === "failed") {
        scheduleReload();
    } else if (event.detail.state === "rejected") {
        location.reload();
    }
}

async function retry() {
    try {
        // Reconnect will asynchronously return:
        // - true to mean success
        // - false to mean we reached the server, but it rejected the connection (e.g., unknown circuit ID)
        // - exception to mean we didn't reach the server (this can be sync or async)
        const successful = await Blazor.reconnect();
        if (!successful) {
            // We have been able to reach the server, but the circuit is no longer available.
            // We'll reload the page so the user can continue using the app as quickly as possible.
            const resumeSuccessful = await Blazor.resumeCircuit();
            if (!resumeSuccessful) {
                scheduleReload();
            } else {
                reconnectModal.close();
            }
        }
    } catch (err) {
        // We got an exception, server is currently unavailable
    }
}

startServerStatusMonitoring();

function startServerStatusMonitoring() {
    if (serverStatusIntervalId) {
        return;
    }

    serverStatusIntervalId = setInterval(checkServerStatus, serverStatusCheckIntervalMs);
    checkServerStatus();

    window.addEventListener("online", checkServerStatus);
    document.addEventListener("visibilitychange", handleVisibilityChangeForStatusCheck);
}

async function checkServerStatus() {
    if (document.visibilityState === "hidden") {
        return;
    }

    if (navigator.onLine === false) {
        handleServerUnreachable();
        return;
    }

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), serverStatusCheckTimeoutMs);

    try {
        const response = await fetch("/_framework/blazor.web.js", {
            method: "HEAD",
            cache: "no-store",
            signal: controller.signal
        });

        clearTimeout(timeoutId);

        if (response.ok) {
            if (consecutiveServerFailures > 0) {
                await retry();
            }
            consecutiveServerFailures = 0;
            return;
        }

        handleServerUnreachable();
    } catch (error) {
        clearTimeout(timeoutId);
        handleServerUnreachable();
    }
}

function handleServerUnreachable() {
    consecutiveServerFailures += 1;

    if (consecutiveServerFailures >= maxServerFailuresBeforeReload) {
        scheduleReload();
        return;
    }

    retry();
}

function handleVisibilityChangeForStatusCheck() {
    if (document.visibilityState === "visible") {
        checkServerStatus();
    }
}

function scheduleReload() {
    if (reloadTimeoutId) {
        return;
    }

    reloadTimeoutId = setTimeout(() => location.reload(), autoReloadDelayMs);
}
