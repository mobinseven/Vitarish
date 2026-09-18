import { render, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ViewportReveal } from "../index";

type ObserverEntry = Pick<IntersectionObserverEntry, "isIntersecting">;

class IntersectionObserverStub {
  static instances: IntersectionObserverStub[] = [];
  readonly observe = vi.fn();
  readonly disconnect = vi.fn();
  private readonly callback: IntersectionObserverCallback;

  constructor(callback: IntersectionObserverCallback) {
    this.callback = callback;
    IntersectionObserverStub.instances.push(this);
  }

  trigger(entry: ObserverEntry) {
    this.callback(
      [entry as IntersectionObserverEntry],
      this as unknown as IntersectionObserver,
    );
  }
}

afterEach(() => {
  vi.unstubAllGlobals();
  IntersectionObserverStub.instances = [];
});

describe("ViewportReveal", () => {
  it("reveals a semantic region when it enters the viewport", async () => {
    vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);

    const { container } = render(
      <ViewportReveal as="section" className="feature-section" delayMs={120}>
        <h2>Feature story</h2>
      </ViewportReveal>,
    );
    const section = container.querySelector("section");
    const observer = IntersectionObserverStub.instances[0];

    expect(section).toHaveAttribute("data-state", "hidden");
    expect(section).toHaveAttribute("data-vitarish-viewport-reveal", "");
    expect(section).toHaveStyle("--vitarish-viewport-reveal-delay: 120ms");
    expect(observer?.observe).toHaveBeenCalledWith(section);

    observer?.trigger({ isIntersecting: true });

    await waitFor(() =>
      expect(section).toHaveAttribute("data-state", "revealed"),
    );
    expect(observer?.disconnect).toHaveBeenCalled();
  });

  it("falls back to visible content when IntersectionObserver is unavailable", async () => {
    vi.stubGlobal("IntersectionObserver", undefined);

    const { getByText } = render(
      <ViewportReveal>Content remains available.</ViewportReveal>,
    );

    await waitFor(() =>
      expect(
        getByText("Content remains available.").closest(
          "[data-vitarish-viewport-reveal]",
        ),
      ).toHaveAttribute("data-state", "revealed"),
    );
  });

  it("can replay the reveal when once is disabled", async () => {
    vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);

    const { container } = render(
      <ViewportReveal once={false}>Replayable content.</ViewportReveal>,
    );
    const region = container.firstElementChild;
    const observer = IntersectionObserverStub.instances[0];

    observer?.trigger({ isIntersecting: true });
    await waitFor(() => expect(region).toHaveAttribute("data-state", "revealed"));

    observer?.trigger({ isIntersecting: false });
    await waitFor(() => expect(region).toHaveAttribute("data-state", "hidden"));
  });
});
