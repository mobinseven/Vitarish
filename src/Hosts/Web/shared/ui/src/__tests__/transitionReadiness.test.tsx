import { act, cleanup, render } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { NavigationTransitionShellReady } from "../navigation/transitionRuntime";
import { navigationTransitionDocument } from "../navigation/transitionContract";

describe("critical shell font readiness", () => {
  let frames: FrameRequestCallback[];
  let settleFonts: () => void;
  let rejectFonts: () => void;
  const release = vi.fn();
  const readFonts = vi.fn();

  beforeEach(() => {
    frames = [];
    const ready = new Promise<void>((resolve, reject) => { settleFonts = resolve; rejectFonts = () => reject(new Error("fixture font failure")); });
    Object.defineProperty(document, "fonts", { configurable: true, value: { get ready() { readFonts(); return ready; } } });
    Object.assign(window, { [navigationTransitionDocument.runtime]: { context: { attempt: crypto.randomUUID() }, release } });
    vi.spyOn(document.body, "getBoundingClientRect");
    vi.spyOn(window, "requestAnimationFrame").mockImplementation(callback => { frames.push(callback); return frames.length; });
    vi.spyOn(window, "cancelAnimationFrame").mockImplementation(() => {});
  });

  afterEach(() => {
    cleanup();
    Reflect.deleteProperty(document, "fonts");
    Reflect.deleteProperty(window, navigationTransitionDocument.runtime);
    vi.restoreAllMocks();
    vi.clearAllMocks();
  });

  const paint = () => act(() => frames.shift()!(0));

  it("does not release cached fonts before the session/access prerequisite settles", async () => {
    render(<NavigationTransitionShellReady pending />);
    await act(async () => settleFonts());
    expect(window.requestAnimationFrame).not.toHaveBeenCalled();
    expect(release).not.toHaveBeenCalled();
  });

  it("discovers mounted text before reading fonts and releases only after the committed font frame", async () => {
    const view = render(<NavigationTransitionShellReady pending />);
    view.rerender(<NavigationTransitionShellReady pending={false} />);
    expect(readFonts).not.toHaveBeenCalled();
    paint();
    expect(document.body.getBoundingClientRect).toHaveBeenCalledBefore(readFonts);
    expect(release).not.toHaveBeenCalled();
    await act(async () => settleFonts());
    expect(release).not.toHaveBeenCalled();
    paint();
    expect(release).toHaveBeenCalledExactlyOnceWith(navigationTransitionDocument.shellParticipant);
  });

  it("does not wait for document load and unrelated images", async () => {
    vi.spyOn(document, "readyState", "get").mockReturnValue("interactive");
    render(<NavigationTransitionShellReady pending={false} />);
    paint();
    await act(async () => settleFonts());
    paint();
    expect(release).toHaveBeenCalledExactlyOnceWith(navigationTransitionDocument.shellParticipant);
  });

  it("detaches discovery and ignores a late font completion after unmount", async () => {
    const view = render(<NavigationTransitionShellReady pending={false} />);
    paint();
    view.unmount();
    await act(async () => settleFonts());
    expect(window.cancelAnimationFrame).toHaveBeenCalledWith(1);
    expect(window.requestAnimationFrame).toHaveBeenCalledTimes(1);
    expect(release).not.toHaveBeenCalled();
  });

  it("allows a usable fallback when font readiness fails", async () => {
    render(<NavigationTransitionShellReady pending={false} />);
    paint();
    await act(async () => rejectFonts());
    paint();
    expect(release).toHaveBeenCalledExactlyOnceWith(navigationTransitionDocument.shellParticipant);
  });
});
