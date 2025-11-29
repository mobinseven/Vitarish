
let dotNetRef = null;       // DotNetObjectReference for callbacks

export function initializePwaInstall(btnElement, dotNetObjRef) {
    dotNetRef = dotNetObjRef;
    onBIP();
    window.addEventListener('appinstalled', onInstalled);
}

function onBIP() {
    let deferredPrompt = window['installprompt'];
    if (!deferredPrompt) return;
    dotNetRef.invokeMethodAsync('OnInstallAvailable');
}

export async function triggerInstall() {
    let deferredPrompt = window['installprompt'];
    if (!deferredPrompt) return;

    deferredPrompt.prompt();                      // show native prompt
    const { outcome } = await deferredPrompt.userChoice;
    deferredPrompt = null;                        // can only prompt once
    dotNetRef.invokeMethodAsync('OnAppInstalled', outcome);
}

function onInstalled() {
    deferredPrompt = null;
    dotNetRef.invokeMethodAsync('OnAppInstalled', 'accepted');
}

export function dispose() {
    window.removeEventListener('beforeinstallprompt', onBIP);
    window.removeEventListener('appinstalled', onInstalled);
    deferredPrompt = installButton = dotNetRef = null;
}
