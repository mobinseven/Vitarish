const states = new Map();

const edgeEpsilon = 0.5;

function findScrollContainer(element) {
    let parent = element.parentElement;
    while (parent) {
        const style = getComputedStyle(parent);
        const overflowY = style.overflowY;
        if ((overflowY === "auto" || overflowY === "scroll" || overflowY === "overlay") && parent.scrollHeight > parent.clientHeight) {
            return parent;
        }

        parent = parent.parentElement;
    }

    return document.scrollingElement || document.documentElement;
}

function applyTransition(element, duration) {
    element.style.transitionDuration = `${duration}ms`;
}

function applyHeight(element, state) {
    const target = state.active ? `${state.height}px` : "0px";
    if (element.style.height !== target) {
        element.style.height = target;
    }

    if (state.active) {
        element.classList.add("end-of-scroll--active");
    } else {
        element.classList.remove("end-of-scroll--active");
    }
}

function updateActive(element, state, force = false) {
    const shouldBeActive = state.reachedEnd;
    if (!force && state.active === shouldBeActive) {
        return;
    }

    state.active = shouldBeActive;
    applyHeight(element, state);
}

function getContainerRect(container) {
    if (container === document.scrollingElement || container === document.documentElement) {
        const height = window.innerHeight || document.documentElement.clientHeight;
        return { top: 0, bottom: height, height };
    }

    const rect = container.getBoundingClientRect();
    return { top: rect.top, bottom: rect.bottom, height: rect.height };
}

function determineOrientation(element, container) {
    const elementRect = element.getBoundingClientRect();
    const containerRect = getContainerRect(container);

    const elementCenter = elementRect.top + elementRect.height / 2;
    const containerCenter = containerRect.top + containerRect.height / 2;

    if (!Number.isFinite(elementCenter) || !Number.isFinite(containerCenter)) {
        return "end";
    }

    return elementCenter <= containerCenter ? "start" : "end";
}

function getScrollMetrics(container) {
    if (container === document.scrollingElement || container === document.documentElement) {
        const scrollingElement = document.scrollingElement || document.documentElement;
        return {
            scrollTop: scrollingElement.scrollTop,
            scrollHeight: scrollingElement.scrollHeight,
            clientHeight: window.innerHeight || scrollingElement.clientHeight
        };
    }

    return {
        scrollTop: container.scrollTop,
        scrollHeight: container.scrollHeight,
        clientHeight: container.clientHeight
    };
}

function isAtEnd(container, orientation) {
    const { scrollTop, scrollHeight, clientHeight } = getScrollMetrics(container);

    if (orientation === "start") {
        return scrollTop <= edgeEpsilon;
    }

    return scrollHeight - clientHeight - scrollTop <= edgeEpsilon;
}

function setup(element, height, duration) {
    dispose(element);

    element.dataset.endOfScroll = "true";
    element.classList.add("vorn-end-of-scroll");
    element.style.height = "0px";
    applyTransition(element, duration);

    const scrollContainer = findScrollContainer(element);
    const listenerTarget =
        scrollContainer === document.scrollingElement || scrollContainer === document.documentElement
            ? window
            : scrollContainer;
    const observer = new IntersectionObserver(
        () => {
            const state = states.get(element);
            if (!state) {
                return;
            }

            const orientation = determineOrientation(element, state.container);
            if (orientation !== state.orientation) {
                state.orientation = orientation;
                state.reachedEnd = isAtEnd(state.container, state.orientation);
            }

            updateActive(element, state);
        },
        {
            root: scrollContainer === document.scrollingElement || scrollContainer === document.documentElement
                ? null
                : scrollContainer,
            threshold: [0, 0.01]
        }
    );

    const state = {
        element,
        observer,
        container: scrollContainer,
        listenerTarget,
        height,
        duration,
        active: false,
        orientation: determineOrientation(element, scrollContainer),
        reachedEnd: false,
        onScroll: null
    };

    state.onScroll = () => {
        const reachedEnd = isAtEnd(state.container, state.orientation);
        if (state.reachedEnd === reachedEnd) {
            return;
        }

        state.reachedEnd = reachedEnd;
        updateActive(element, state);
    };

    states.set(element, state);
    observer.observe(element);
    state.listenerTarget.addEventListener("scroll", state.onScroll, { passive: true });
    state.reachedEnd = isAtEnd(state.container, state.orientation);
    updateActive(element, state, true);
}

export function initialize(element, height, duration) {
    setup(element, height, duration);
}

export function update(element, height, duration) {
    const state = states.get(element);
    if (!state) {
        setup(element, height, duration);
        return;
    }

    state.height = height;
    state.duration = duration;
    state.orientation = determineOrientation(element, state.container);
    state.reachedEnd = isAtEnd(state.container, state.orientation);
    applyTransition(element, duration);
    updateActive(element, state, true);
}

export function dispose(element) {
    const state = states.get(element);
    if (!state) {
        return;
    }

    state.observer.disconnect();
    if (state.onScroll) {
        state.listenerTarget.removeEventListener("scroll", state.onScroll);
        state.onScroll = null;
    }
    states.delete(element);
    element.classList.remove("end-of-scroll--active");
    delete element.dataset.endOfScroll;
}
