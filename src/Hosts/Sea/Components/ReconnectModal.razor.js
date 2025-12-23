// Set up event handlers
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

const retryButton = document.getElementById("components-reconnect-button");
retryButton.addEventListener("click", retry);

const resumeButton = document.getElementById("components-resume-button");
resumeButton.addEventListener("click", resume);

const serverStatusCheckIntervalMs = 10000;
const serverStatusCheckTimeoutMs = 8000;
const maxServerFailuresBeforeReload = 6;

let serverStatusIntervalId = null;
let consecutiveServerFailures = 0;

function handleReconnectStateChanged(event) {
    if (event.detail.state === "show") {
        reconnectModal.showModal();
    } else if (event.detail.state === "hide") {
        reconnectModal.close();
    } else if (event.detail.state === "failed") {
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } else if (event.detail.state === "rejected") {
        location.reload();
    }
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);

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
                location.reload();
            } else {
                reconnectModal.close();
            }
        }
    } catch (err) {
        // We got an exception, server is currently unavailable
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    }
}

async function resume() {
    try {
        const successful = await Blazor.resumeCircuit();
        if (!successful) {
            location.reload();
        }
    } catch {
        location.reload();
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
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
        location.reload();
        return;
    }

    retry();
}

function handleVisibilityChangeForStatusCheck() {
    if (document.visibilityState === "visible") {
        checkServerStatus();
    }
}
