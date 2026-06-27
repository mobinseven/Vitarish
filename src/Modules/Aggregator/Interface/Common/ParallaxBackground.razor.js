export function attachParallaxGlobal(anchorEl, opts) {
    const options = Object.assign({
        imageUrl: "",
        backgroundPosition: "center",
        zIndex: 0,
        maxShiftVh: 6,
        minShiftPx: 12,
        maxShiftPx: 60,
        safetyPx: 40,
        respectReducedMotion: true,
        enablePolling: true,
        pollMs: 1200,
        scrollTargetSelector: null
    }, opts || {});

    const reduceMotion = options.respectReducedMotion &&
        window.matchMedia &&
        window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Create the fixed background layer
    const layer = document.createElement('div');
    layer.className = 'tp-global-layer';
    Object.assign(layer.style, {
        position: 'fixed',
        pointerEvents: 'none',
        left: '0', top: '0', right: '0', bottom: '0',
        zIndex: String(options.zIndex),
        willChange: 'transform',
        backgroundRepeat: 'no-repeat',
        backgroundSize: 'cover',
        backgroundPosition: options.backgroundPosition
    });
    layer.style.backgroundImage = options.imageUrl ? `url("${options.imageUrl}")` : 'none';
    document.body.insertBefore(layer, document.body.firstChild || null);

    // ---- Scroll target resolution
    let useRootScroll = true;
    let scrollEl = null;

    function isScrollable(el) {
        const cs = getComputedStyle(el);
        const oy = cs.overflowY;
        if (!(oy === 'auto' || oy === 'scroll')) return false;
        return el.scrollHeight > (el.clientHeight + 1);
    }

    function resolveScrollTarget() {
        // explicit selector takes priority
        if (options.scrollTargetSelector) {
            const found = document.querySelector(options.scrollTargetSelector);
            if (found) {
                scrollEl = found;
                useRootScroll = false;
                return;
            }
        }
        // auto-detect common containers
        const candidates = document.querySelectorAll('[data-scroll],[data-scroll-container],main,#main,.main,.content,.scroll,.scroll-container');
        for (const c of candidates) {
            if (isScrollable(c)) {
                scrollEl = c;
                useRootScroll = false;
                return;
            }
        }
        // default to root/window scroll
        scrollEl = null;
        useRootScroll = true;
    }

    resolveScrollTarget();

    // ---- Parallax math
    let strength = 0; // px shift per px of scroll
    let ticking = false;

    function getScrollTop() {
        if (useRootScroll) {
            const se = document.scrollingElement || document.documentElement;
            return se.scrollTop;
        }
        return scrollEl.scrollTop;
    }

    function getMaxScroll() {
        if (useRootScroll) {
            const se = document.scrollingElement || document.documentElement;
            return Math.max(1, se.scrollHeight - window.innerHeight);
        }
        return Math.max(1, scrollEl.scrollHeight - scrollEl.clientHeight);
    }

    function desiredMaxShift() {
        const vCap = (options.maxShiftVh / 100) * window.innerHeight;
        return Math.min(options.maxShiftPx, Math.max(options.minShiftPx, vCap));
    }

    function recomputeStrength() {
        const targetShift = desiredMaxShift();
        const maxScroll = getMaxScroll();
        strength = targetShift / maxScroll;

        // Overscan so edges never show
        const overscan = Math.ceil(targetShift + options.safetyPx);
        layer.style.top = `${-overscan}px`;
        layer.style.left = `${-overscan}px`;
        layer.style.right = `${-overscan}px`;
        layer.style.bottom = `${-overscan}px`;
    }

    function update() {
        const y = getScrollTop();
        const offsetY = reduceMotion ? 0 : -y * strength;
        layer.style.transform = `translate3d(0, ${offsetY.toFixed(2)}px, 0)`;
        ticking = false;
    }

    function onScroll() {
        if (!ticking) {
            requestAnimationFrame(update);
            ticking = true;
        }
    }

    const scheduleRecompute = (() => {
        let pending = false;
        return () => {
            if (pending) return;
            pending = true;
            requestAnimationFrame(() => {
                pending = false;
                recomputeStrength();
                update();
            });
        };
    })();

    // ---- Listeners
    function bindScroll() {
        if (useRootScroll) {
            window.addEventListener('scroll', onScroll, { passive: true });
        } else {
            scrollEl.addEventListener('scroll', onScroll, { passive: true });
        }
    }
    function unbindScroll() {
        if (useRootScroll) {
            window.removeEventListener('scroll', onScroll);
        } else if (scrollEl) {
            scrollEl.removeEventListener('scroll', onScroll);
        }
    }

    scheduleRecompute();
    bindScroll();

    window.addEventListener('resize', scheduleRecompute, { passive: true });
    if (window.visualViewport) {
        visualViewport.addEventListener('resize', scheduleRecompute, { passive: true });
        visualViewport.addEventListener('scroll', scheduleRecompute, { passive: true });
    }

    let ro = null, mo = null, pollId = null;

    if ('ResizeObserver' in window) {
        ro = new ResizeObserver(scheduleRecompute);
        (useRootScroll ? document.documentElement : scrollEl) && ro.observe(useRootScroll ? document.documentElement : scrollEl);
        document.body && ro.observe(document.body);
    }
    if ('MutationObserver' in window) {
        mo = new MutationObserver(scheduleRecompute);
        mo.observe(document.documentElement, { childList: true, subtree: true, attributes: true });
    }

    window.addEventListener('load', scheduleRecompute);
    try {
        if (document.fonts && document.fonts.addEventListener) {
            document.fonts.addEventListener('loadingdone', scheduleRecompute);
        }
    } catch { /* no-op */ }

    if (options.enablePolling) {
        let lastH = getMaxScroll();
        pollId = setInterval(() => {
            const h = getMaxScroll();
            if (h !== lastH) {
                lastH = h;
                scheduleRecompute();
            }
        }, Math.max(250, options.pollMs | 0));
    }

    function updateOptions(newOpts) {
        Object.assign(options, newOpts || {});
        layer.style.backgroundPosition = options.backgroundPosition || 'center';
        layer.style.zIndex = String(options.zIndex ?? 0);
        layer.style.backgroundImage = options.imageUrl ? `url("${options.imageUrl}")` : 'none';

        const prevUseRoot = useRootScroll;
        const prevEl = scrollEl;
        resolveScrollTarget();
        if (prevUseRoot !== useRootScroll || prevEl !== scrollEl) {
            unbindScroll();
            bindScroll();
            if (ro) ro.disconnect();
            if ('ResizeObserver' in window) {
                ro = new ResizeObserver(scheduleRecompute);
                (useRootScroll ? document.documentElement : scrollEl) && ro.observe(useRootScroll ? document.documentElement : scrollEl);
                document.body && ro.observe(document.body);
            }
        }
        scheduleRecompute();
    }

    return {
        updateOptions,
        dispose() {
            unbindScroll();
            window.removeEventListener('resize', scheduleRecompute);
            if (window.visualViewport) {
                visualViewport.removeEventListener('resize', scheduleRecompute);
                visualViewport.removeEventListener('scroll', scheduleRecompute);
            }
            if (ro) ro.disconnect();
            if (mo) mo.disconnect();
            window.removeEventListener('load', scheduleRecompute);
            try {
                if (document.fonts && document.fonts.removeEventListener) {
                    document.fonts.removeEventListener('loadingdone', scheduleRecompute);
                }
            } catch { /* no-op */ }
            if (pollId) clearInterval(pollId);
            layer.remove();
        }
    };
}
