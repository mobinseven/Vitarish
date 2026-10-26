import { act, cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { InitialPageLoader } from "@/components/loading/InitialPageLoader";

const session = vi.hoisted(() => ({ isLoading: true }));
vi.mock("@vitarish/auth", () => ({ useAuthSession: () => session }));
vi.mock("@/components/loading/LoadingScreen", () => ({
  LoadingScreen: () => <div role="status" />,
}));

describe("InitialPageLoader", () => {
  let settleFonts: () => void;
  let paint: FrameRequestCallback;

  beforeEach(() => {
    session.isLoading = true;
    const ready = new Promise<void>((resolve) => { settleFonts = resolve; });
    vi.spyOn(document, "readyState", "get").mockReturnValue("complete");
    vi.stubGlobal("document", document);
    Object.defineProperty(document, "fonts", { configurable: true, value: { ready } });
    vi.spyOn(window, "requestAnimationFrame").mockImplementation((callback) => {
      paint = callback;
      return 1;
    });
    vi.spyOn(window, "cancelAnimationFrame").mockImplementation(() => {});
  });

  afterEach(() => {
    cleanup();
    Reflect.deleteProperty(document, "fonts");
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it("keeps covering the page after session resolution until its fonts settle", async () => {
    const view = render(<InitialPageLoader />);
    session.isLoading = false;
    view.rerender(<InitialPageLoader />);
    act(() => paint(0));
    expect(screen.getByRole("status")).toBeInTheDocument();
    await act(async () => settleFonts());
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("does not dismiss for cached fonts before the session resolves", async () => {
    render(<InitialPageLoader />);
    await act(async () => settleFonts());
    expect(window.requestAnimationFrame).not.toHaveBeenCalled();
    expect(screen.getByRole("status")).toBeInTheDocument();
  });

  it("waits for the document load before discovering page fonts", async () => {
    vi.spyOn(document, "readyState", "get").mockReturnValue("interactive");
    session.isLoading = false;
    render(<InitialPageLoader />);
    await act(async () => settleFonts());
    expect(window.requestAnimationFrame).not.toHaveBeenCalled();
    act(() => { window.dispatchEvent(new Event("load")); });
    await act(async () => paint(0));
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("cancels font discovery when unmounted", () => {
    session.isLoading = false;
    const view = render(<InitialPageLoader />);
    view.unmount();
    expect(window.cancelAnimationFrame).toHaveBeenCalledWith(1);
  });
});
