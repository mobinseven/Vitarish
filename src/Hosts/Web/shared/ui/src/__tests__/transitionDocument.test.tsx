import { render, cleanup, act } from "@testing-library/react";
import { renderToString } from "react-dom/server";
import { hydrateRoot } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { NavigationTransitionDocument, navigationTransitionDocument, type NavigationTransitionRuntime } from "../navigation/transitionDocument";
import { navigationTransitionPolicy, navigationTransitionQueryKey, navigationTransitionStorageKey } from "../navigation/transitionContext";
import { navigationTransitionMessages } from "../navigation/navigationMessages";
import { themeModeAttribute, themePreferenceStorageKey } from "../themeMode";
import { useNavigationTransitionParticipant } from "../navigation/transitionRuntime";
import type { NavigationTransitionOutcome } from "../navigation/transitionContract";

function Participant({ pending, outcome }: { pending: boolean; outcome?: NavigationTransitionOutcome }) {
  useNavigationTransitionParticipant(pending, outcome);
  return null;
}

function boot() {
  const result = render(<><NavigationTransitionDocument signInHref="/login" homeHref="/" nonce="test-nonce" /><div {...{ [navigationTransitionDocument.contentAttribute]: "" }} /></>);
  const script = result.container.querySelector("script")!;
  expect(script.nonce).toBe("test-nonce");
  expect(result.container.querySelector("style")!.nonce).toBe("test-nonce");
  // Evaluate independently of module scope, as a browser does before hydration.
  new Function(script.innerHTML)();
  return (window as unknown as Record<string, NavigationTransitionRuntime>)[navigationTransitionDocument.runtime];
}

afterEach(() => {
  const runtime = (window as unknown as Record<string, NavigationTransitionRuntime>)[navigationTransitionDocument.runtime];
  runtime?.finish("cancelled");
  cleanup();
  vi.useRealTimers();
  sessionStorage.clear();
  localStorage.clear();
  history.replaceState(null, "", "/");
});

describe("pre-paint transition document", () => {
  it("reads the existing theme preference without taking ownership of the application mode", () => {
    document.documentElement.removeAttribute(themeModeAttribute);
    localStorage.setItem(themePreferenceStorageKey, "dark");
    const runtime = boot();
    expect(runtime.context.mode).toBe("dark");
    expect(document.documentElement.getAttribute(themeModeAttribute)).toBeNull();
  });
  it.each(["fa", "en"])("keeps the pre-painted %s surface through hydration without regenerating it", async locale => {
    document.documentElement.lang = locale;
    const element = <NavigationTransitionDocument signInHref="/login" homeHref="/" />;
    const container = document.createElement("div");
    container.innerHTML = renderToString(element);
    document.body.append(container);
    const original = container.querySelector(`#${navigationTransitionDocument.surfaceId}`);
    const text = original?.textContent;
    new Function(container.querySelector("script")!.innerHTML)();
    const errors: unknown[] = [];
    let root: ReturnType<typeof hydrateRoot>;
    await act(async () => { root = hydrateRoot(container, element, { onRecoverableError: error => errors.push(error) }); });
    expect(errors).toEqual([]);
    expect(container.querySelector(`#${navigationTransitionDocument.surfaceId}`)).toBe(original);
    expect(original?.textContent).toBe(text);
    await act(async () => root!.unmount());
    container.remove();
  });
  it("adopts the carried mode/locale before hydration and preserves the credential fragment", () => {
    const context = { version: 1, attempt: crypto.randomUUID(), started: Date.now(), mode: "dark", locale: "en" };
    const query = new URLSearchParams({ request: "opaque", [navigationTransitionQueryKey]: JSON.stringify(context) });
    history.replaceState(null, "", `/continue?${query}#ticket`);
    const runtime = boot();
    expect(runtime.context).toEqual(context);
    expect(location.search).toBe("?request=opaque");
    expect(location.hash).toBe("#ticket");
    expect(document.getElementById(navigationTransitionDocument.surfaceId)).toHaveAttribute(navigationTransitionDocument.modeAttribute, "dark");
    expect(document.getElementById(navigationTransitionDocument.statusId)).toHaveTextContent(navigationTransitionMessages.en.waiting);
  });

  it("requires every participant and never reopens a completed attempt", async () => {
    vi.useFakeTimers();
    const runtime = boot();
    runtime.hold("journey");
    runtime.release("shell");
    expect(runtime.active()).toBe(true);
    runtime.release("unrelated");
    expect(runtime.active()).toBe(true);
    runtime.release("journey");
    await Promise.resolve();
    expect(runtime.active()).toBe(false);
    runtime.hold("late-query");
    vi.advanceTimersByTime(navigationTransitionPolicy.revealMilliseconds);
    expect(document.getElementById(navigationTransitionDocument.surfaceId)).toHaveAttribute("hidden");
    expect(sessionStorage.getItem(navigationTransitionStorageKey)).toBeNull();
  });

  it("uses the carried clock and exposes recovery even if React never hydrates", () => {
    vi.useFakeTimers();
    const elapsed = navigationTransitionPolicy.slowMilliseconds - 1;
    const context = { version: 1, attempt: crypto.randomUUID(), started: Date.now() - elapsed, mode: "light", locale: "en" };
    sessionStorage.setItem(navigationTransitionStorageKey, JSON.stringify(context));
    const runtime = boot();
    vi.advanceTimersByTime(1);
    expect(document.getElementById(navigationTransitionDocument.surfaceId)).toHaveAttribute(navigationTransitionDocument.slowAttribute, "true");
    vi.advanceTimersByTime(navigationTransitionPolicy.recoveryMilliseconds - navigationTransitionPolicy.slowMilliseconds);
    expect(runtime.active()).toBe(false);
    expect(document.getElementById(navigationTransitionDocument.recoveryId)).not.toHaveAttribute("hidden");
    expect(document.getElementById(navigationTransitionDocument.surfaceId)).toHaveAttribute("aria-busy", "false");
    expect(document.activeElement).toHaveAttribute("href", "/login");
    expect(runtime.signal.aborted).toBe(true);
  });

  it("ignores a queued completion from a cancelled attempt when a fresh entry starts", async () => {
    const runtime = boot();
    const oldSignal = runtime.signal;
    runtime.release(navigationTransitionDocument.shellParticipant);
    runtime.finish("cancelled");
    const next = { ...runtime.context, attempt: crypto.randomUUID(), started: Date.now() };
    runtime.start(next);
    await Promise.resolve();
    expect(oldSignal.aborted).toBe(true);
    expect(runtime.signal.aborted).toBe(false);
    expect(runtime.context.attempt).toBe(next.attempt);
    expect(runtime.active()).toBe(true);
    expect(document.getElementById(navigationTransitionDocument.surfaceId)).not.toHaveAttribute("hidden");
  });

  it("restores bfcache content without resurrecting its pending cover or callbacks", () => {
    const runtime = boot();
    const event = new Event("pageshow");
    Object.defineProperty(event, "persisted", { value: true });
    window.dispatchEvent(event);
    expect(runtime.active()).toBe(false);
    expect(runtime.signal.aborted).toBe(true);
    expect(document.getElementById(navigationTransitionDocument.surfaceId)).toHaveAttribute("hidden");
    expect(sessionStorage.getItem(navigationTransitionStorageKey)).toBeNull();
  });

  it("lets a mounted participant join a fresh retry and expose its failure immediately", () => {
    const runtime = boot();
    const view = render(<Participant pending />);
    runtime.release(navigationTransitionDocument.shellParticipant);
    view.rerender(<Participant pending={false} outcome="failed" />);
    expect(runtime.active()).toBe(false);
    runtime.start({ ...runtime.context, attempt: crypto.randomUUID(), started: Date.now() });
    view.rerender(<Participant pending />);
    view.rerender(<Participant pending={false} outcome="failed" />);
    expect(runtime.active()).toBe(false);
    expect(document.getElementById(navigationTransitionDocument.surfaceId)).toHaveAttribute("hidden");
  });
});


it("keeps failed chunk recovery terminal when later participants complete", () => {
  const runtime = boot();
  const chunk = document.createElement("script");
  document.body.append(chunk);
  chunk.dispatchEvent(new Event("error"));
  runtime.release(navigationTransitionDocument.shellParticipant);
  expect(runtime.state).toBe("failed");
  expect(runtime.signal.aborted).toBe(true);
  expect(document.getElementById(navigationTransitionDocument.recoveryId)).not.toHaveAttribute("hidden");
  expect(document.getElementById(navigationTransitionDocument.surfaceId)).not.toHaveAttribute("hidden");
  chunk.remove();
});

it("does not let a completed reveal timer hide a fresh waiting segment", async () => {
  vi.useFakeTimers();
  const runtime = boot();
  runtime.release(navigationTransitionDocument.shellParticipant);
  await Promise.resolve();
  expect(runtime.state).toBe("ready");
  runtime.start({ ...runtime.context, attempt: crypto.randomUUID(), started: Date.now() });
  vi.advanceTimersByTime(navigationTransitionPolicy.revealMilliseconds);
  expect(runtime.active()).toBe(true);
  expect(document.getElementById(navigationTransitionDocument.surfaceId)).not.toHaveAttribute("hidden");
});
