import "@testing-library/jest-dom/vitest";

if (typeof Element !== "undefined" && !Element.prototype.getAnimations) {
  Object.defineProperty(Element.prototype, "getAnimations", {
    configurable: true,
    value: (): Animation[] => [],
  });
}

if (typeof window !== "undefined" && !window.matchMedia) {
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

if (typeof window !== "undefined" && !window.URL.createObjectURL) {
  Object.defineProperty(window.URL, "createObjectURL", {
    writable: true,
    value: () => "blob:vitest",
  });
}

if (typeof window !== "undefined" && !window.URL.revokeObjectURL) {
  Object.defineProperty(window.URL, "revokeObjectURL", {
    writable: true,
    value: () => undefined,
  });
}
