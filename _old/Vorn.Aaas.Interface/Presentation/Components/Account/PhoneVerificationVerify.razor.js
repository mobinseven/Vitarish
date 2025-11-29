export function setupOtpInput(options = {}) {
    const {
        dotNetHelper,
        autoSubmitMethod = 'ValidSubmit',
        enableWebOtp = false,
        selectors = [
            '[autocomplete="one-time-code"]',
            'input[name*="otp" i],input[name*="code" i]'
        ],
    } = options ?? {};

    const candidateSelectors = Array.isArray(selectors) && selectors.length > 0
        ? selectors
        : ['[autocomplete="one-time-code"]', 'input[name*="otp" i],input[name*="code" i]'];

    let input = null;
    for (const selector of candidateSelectors) {
        if (typeof selector !== 'string') {
            continue;
        }

        input = document.querySelector(selector);
        if (input) {
            break;
        }
    }

    if (!input) return;

    const updateCaret = () => {
        const pos = typeof input.selectionStart === 'number' ? input.selectionStart : 0;
        input.style.setProperty('--_otp-digit', String(pos));
    };

    updateCaret();
    input.addEventListener('input', updateCaret, { passive: true });

    const ac = typeof AbortController !== 'undefined' ? new AbortController() : null;
    const form = typeof input.closest === 'function' ? input.closest('form') : null;
    if (form) {
        form.addEventListener(
            'submit',
            () => {
                if (ac) ac.abort();
            },
            { once: true }
        );
    }

    const cleanup = () => {
        input.removeEventListener('input', updateCaret);
    };

    if (ac) ac.signal.addEventListener('abort', cleanup, { once: true });

    const onPageHide = () => {
        if (ac) ac.abort();
        cleanup();
    };
    window.addEventListener('pagehide', onPageHide, { once: true });

    if (!enableWebOtp) return;

    const creds = navigator.credentials;
    const canWebOtp =
        typeof isSecureContext !== 'undefined' &&
        isSecureContext &&
        !!creds &&
        typeof creds.get === 'function' &&
        'OTPCredential' in window;

    if (!canWebOtp) return;

    const requestOpts = { otp: { transport: ['sms'] } };
    if (ac) requestOpts.signal = ac.signal;

    try {
        creds
            .get(requestOpts)
            .then((otp) => {
                if (!otp || !otp.code) return;
                input.value = otp.code;
                input.dispatchEvent(new Event('input', { bubbles: true }));
                if (
                    form &&
                    dotNetHelper &&
                    typeof dotNetHelper.invokeMethodAsync === 'function' &&
                    autoSubmitMethod
                ) {
                    queueMicrotask(() => dotNetHelper.invokeMethodAsync(autoSubmitMethod));
                }
            })
            .catch(() => {
                // Swallow AbortError / NotAllowedError silently
            });
    } catch (_) {
        // Some UAs may throw synchronously if not fully supported; ignore.
    }
}

export function setupDotnet(dotNetHelper) {
    setupOtpInput({ dotNetHelper, enableWebOtp: true });
}
