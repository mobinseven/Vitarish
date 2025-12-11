let resizeObserver;
let activeElement;
let svg, defs, mask, holeRect;
let updatetimeout, stoptimeout;
const MASK_ID = "fw-rect-mask";

const activeElementStyles = {
    transition: "all 0.2s ease"
};

function applyStyles(el, styles) {
    if (!el) return;
    for (const [k, v] of Object.entries(styles)) el.style[k] = v;
}

function removeStyles(el, styles) {
    if (!el) return;
    for (const k of Object.keys(styles)) el.style[k] = "";
}

function ensureMask() {
    if (svg && mask && holeRect) return;

    svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("xmlns", "http://www.w3.org/2000/svg");
    svg.setAttribute("width", "0");
    svg.setAttribute("height", "0");
    svg.style.position = "absolute";
    svg.style.left = "-9999px";

    defs = document.createElementNS(svg.namespaceURI, "defs");

    mask = document.createElementNS(svg.namespaceURI, "mask");
    mask.setAttribute("id", MASK_ID);
    mask.setAttribute("maskUnits", "userSpaceOnUse");
    mask.setAttribute("maskContentUnits", "userSpaceOnUse");

    const fullRect = document.createElementNS(svg.namespaceURI, "rect");
    fullRect.setAttribute("x", "0");
    fullRect.setAttribute("y", "0");
    fullRect.setAttribute("fill", "white");
    fullRect.setAttribute("id", "fw-full-rect");

    holeRect = document.createElementNS(svg.namespaceURI, "rect");
    holeRect.setAttribute("x", "0");
    holeRect.setAttribute("y", "0");
    holeRect.setAttribute("width", "0");
    holeRect.setAttribute("height", "0");
    holeRect.setAttribute("fill", "black");
    holeRect.setAttribute("shape-rendering", "crispEdges");
    holeRect.setAttribute("rx", "8");
    holeRect.setAttribute("ry", "8");

    mask.appendChild(fullRect);
    mask.appendChild(holeRect);
    defs.appendChild(mask);
    svg.appendChild(defs);
    document.body.appendChild(svg);

    updateMaskViewport();
}

function updateMaskViewport() {
    if (!mask) return;
    const w = Math.ceil(window.innerWidth);
    const h = Math.ceil(window.innerHeight);

    mask.setAttribute("x", "0");
    mask.setAttribute("y", "0");
    mask.setAttribute("width", String(w));
    mask.setAttribute("height", String(h));

    const fullRect = mask.querySelector("#fw-full-rect");
    if (fullRect) {
        fullRect.setAttribute("width", String(w));
        fullRect.setAttribute("height", String(h));
    }
}

function ensureOverlayStyles(overlay) {
    overlay.style.position = "fixed";
    overlay.style.left = "0";
    overlay.style.top = "0";
    overlay.style.right = "0";
    overlay.style.bottom = "0";
    overlay.style.zIndex = "10001";
    overlay.style.pointerEvents = "none";
    overlay.style.backgroundColor = "rgba(0,0,0,0.12)";
    overlay.style.backdropFilter = "blur(1px)";
    overlay.style.webkitBackdropFilter = "blur(1px)";
    overlay.style.transition = "opacity 0.1s ease-out";
}

function applyMaskToOverlay(overlay) {
    const url = `url(#${MASK_ID})`;
    overlay.style.mask = url;
    overlay.style.webkitMask = url;
}

function setRectHole(element) {
    const overlay = document.querySelector(".blur-overlay");
    if (!overlay || !element) return;

    ensureMask();
    ensureOverlayStyles(overlay);
    applyMaskToOverlay(overlay);

    const r = element.getBoundingClientRect();
    holeRect.setAttribute("x", String(Math.round(r.left)));
    holeRect.setAttribute("y", String(Math.round(r.top)));
    holeRect.setAttribute("width", String(Math.round(r.width)));
    holeRect.setAttribute("height", String(Math.round(r.height)));
}

function onUpdate() {
    const overlay = document.querySelector(".blur-overlay");
    if (!overlay) return;

    overlay.style.opacity = "0";
    if (updatetimeout) {
        clearTimeout(updatetimeout);
    }
    updatetimeout = setTimeout(() => {
        updateMaskViewport();
        if (activeElement) setRectHole(activeElement);

        overlay.style.opacity = ".85";
        if (stoptimeout) {
            clearTimeout(stoptimeout);
        }
        stoptimeout = setTimeout(stopTracking, 30000);
    }, 1000);
}

export function startTracking(element) {
    if (!element) return stopTracking();
    activeElement = element;
    setRectHole(element);
    applyStyles(element, activeElementStyles);

    window.addEventListener("scroll", onUpdate, true);
    window.addEventListener("resize", onUpdate);
    resizeObserver = new ResizeObserver(onUpdate);
    resizeObserver.observe(element);
    activeElement.addEventListener('focusout', stopTracking);
}

export function stopTracking() {
    window.removeEventListener("scroll", onUpdate, true);
    window.removeEventListener("resize", onUpdate);

    if (resizeObserver && activeElement) {
        resizeObserver.unobserve(activeElement);
        resizeObserver.disconnect();
    }
    removeStyles(activeElement, activeElementStyles);

    const overlay = document.querySelector(".blur-overlay");
    if (overlay) {
        overlay.style.opacity = "0";
        overlay.style.mask = "";
        overlay.style.webkitMask = "";
    }
    activeElement = null;
}
