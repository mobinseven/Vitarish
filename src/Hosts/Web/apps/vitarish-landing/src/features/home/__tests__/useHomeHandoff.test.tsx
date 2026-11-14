import { act, cleanup, renderHook } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { useHomeHandoff } from "../useHomeHandoff";

const fixture = vi.hoisted(() => ({
  issue: vi.fn(), finish: vi.fn(), controller: new AbortController(), attempt: "",
}));
vi.mock("@vitarish/platform", () => ({ landingIssueWorkspaceHandoff: fixture.issue, landingIssueOnboardingHandoff: fixture.issue }));
vi.mock("@vitarish/ui/navigation", () => ({
  takeNavigationTransition: () => { fixture.attempt = crypto.randomUUID(); fixture.controller = new AbortController(); return { attempt: fixture.attempt }; },
  navigationTransitionSignal: () => fixture.controller.signal,
  isCurrentNavigationTransition: (attempt: { attempt: string }) => attempt.attempt === fixture.attempt && !fixture.controller.signal.aborted,
  carryNavigationTransition: (target: string) => target,
}));
vi.mock("@vitarish/ui/navigation-runtime", () => ({
  navigationTransitionStates: { Cancelled: "cancelled", Failed: "failed" },
  navigationTransitionRuntime: () => ({ finish: fixture.finish }),
}));

afterEach(() => { cleanup(); vi.clearAllMocks(); });
const target = { kind: "open", tenantId: "fixture-workspace" } as const;

it("issues once for repeated activation and ignores an abandoned result after a fresh start", async () => {
  const abandoned = Promise.withResolvers<never>();
  fixture.issue.mockReturnValueOnce(abandoned.promise).mockReturnValueOnce(new Promise(() => {}));
  const view = renderHook(() => useHomeHandoff());
  act(() => { view.result.current.start(target); view.result.current.start(target); });
  expect(fixture.issue).toHaveBeenCalledTimes(1);
  const oldSignal = fixture.controller.signal;
  act(() => fixture.controller.abort());
  expect(view.result.current.pending).toBeNull();
  act(() => view.result.current.start(target));
  expect(fixture.issue).toHaveBeenCalledTimes(2);
  expect(oldSignal.aborted).toBe(true);
  await act(async () => abandoned.reject(new Error("abandoned fixture request")));
  expect(view.result.current.pending).toEqual(target);
  expect(view.result.current.failed).toBeNull();
  expect(fixture.finish).not.toHaveBeenCalled();
});

it("restores source actions on a definitive failure and permits an explicit fresh attempt", async () => {
  fixture.issue.mockRejectedValueOnce(new Error("fixture unavailable")).mockReturnValueOnce(new Promise(() => {}));
  const view = renderHook(() => useHomeHandoff());
  await act(async () => view.result.current.start(target));
  expect(view.result.current.pending).toBeNull();
  expect(view.result.current.failed).toEqual(target);
  expect(fixture.finish).toHaveBeenCalledExactlyOnceWith("failed");
  act(() => view.result.current.start(target));
  expect(view.result.current.pending).toEqual(target);
  expect(view.result.current.failed).toBeNull();
  view.unmount();
  expect(fixture.finish).toHaveBeenLastCalledWith("cancelled");
});
