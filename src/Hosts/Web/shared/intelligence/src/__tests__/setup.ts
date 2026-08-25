import "@testing-library/jest-dom/vitest";

if (!Element.prototype.getAnimations) {
  Object.defineProperty(Element.prototype, "getAnimations", {
    configurable: true,
    value: (): Animation[] => [],
  });
}

if (!window.matchMedia) {
  Object.defineProperty(window, "matchMedia", {
    writable: true,
    value: (query: string): MediaQueryList => ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => false,
    }),
  });
}

if (!window.URL.createObjectURL) {
  Object.defineProperty(window.URL, "createObjectURL", {
    writable: true,
    value: () => "blob:vitest",
  });
}

if (!window.URL.revokeObjectURL) {
  Object.defineProperty(window.URL, "revokeObjectURL", {
    writable: true,
    value: () => undefined,
  });
}
