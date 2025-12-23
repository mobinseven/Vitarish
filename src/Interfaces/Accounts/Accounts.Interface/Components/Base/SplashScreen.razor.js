export function registerSplashScreenLoadedHandler(dotNetRef) {
    const notifyHidden = () => {
        if (!dotNetRef) {
            return;
        }

        dotNetRef.invokeMethodAsync("HideAfterLoadAsync").catch(() => {
            // Ignore errors during teardown
        });
    };

    if (document.readyState === "complete") {
        notifyHidden();
        return {
            dispose: () => { }
        };
    }

    const onLoad = () => {
        notifyHidden();
    };

    window.addEventListener("load", onLoad, { once: true });

    return {
        dispose: () => window.removeEventListener("load", onLoad)
    };
}
