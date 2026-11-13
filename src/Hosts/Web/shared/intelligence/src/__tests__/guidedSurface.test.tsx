import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ActionReviewGroup, type ActionReviewItem } from "../components/ActionReviewGroup";
import { GoalChecklist } from "../components/GoalChecklist";
import { GuidedSurfaceBlockKindResponse } from "../api/generated/intelligence/model";
import type { GuidedSurfaceBlockResponse } from "../api/generated/intelligence/model";
import { toGuidedSurface } from "../interaction/guidedSurface";

afterEach(cleanup);

describe("toGuidedSurface", () => {
  const question = (maxSelections: unknown, prompt = "Which?"): GuidedSurfaceBlockResponse => ({
    kind: GuidedSurfaceBlockKindResponse.Question,
    question: {
      prompt,
      options: [{ key: "a", label: "A" }, { key: "b", label: "B" }],
      maxSelections: maxSelections as string,
    },
  });

  it("maps every block kind", () => {
    const surface = toGuidedSurface([
      question("2"),
      { kind: GuidedSurfaceBlockKindResponse.NextAction, nextAction: "ShiftsAndBookingRules" },
      { kind: GuidedSurfaceBlockKindResponse.ActionGroup, actionGroup: { agentRunId: "run-1", actionIds: ["x", "y"] } },
    ]);

    expect(surface.question).toEqual({
      prompt: "Which?",
      options: [{ key: "a", label: "A" }, { key: "b", label: "B" }],
      maxSelections: 2,
    });
    expect(surface.nextAction).toBe("ShiftsAndBookingRules");
    expect(surface.actionGroups).toEqual([{ agentRunId: "run-1", actionIds: ["x", "y"] }]);
  });

  it("returns an empty surface without blocks", () => {
    expect(toGuidedSurface(undefined)).toEqual({ question: null, nextAction: null, actionGroups: [] });
    expect(toGuidedSurface(null).question).toBeNull();
    expect(toGuidedSurface([]).actionGroups).toEqual([]);
  });

  it("lets the last question win", () => {
    expect(toGuidedSurface([question("1", "First"), question("1", "Second")]).question?.prompt).toBe("Second");
  });

  it("reads maxSelections from a number or a string and falls back to one when invalid", () => {
    const max = (value: unknown) => toGuidedSurface([question(value)]).question?.maxSelections;
    expect(max("3")).toBe(3);
    expect(max(4)).toBe(4);
    expect(max("abc")).toBe(1);
    expect(max("0")).toBe(1);
    expect(max("-2")).toBe(1);
  });

  it("ignores incomplete blocks", () => {
    const surface = toGuidedSurface([
      { kind: GuidedSurfaceBlockKindResponse.Question },
      { kind: GuidedSurfaceBlockKindResponse.NextAction },
      { kind: GuidedSurfaceBlockKindResponse.ActionGroup },
      { kind: GuidedSurfaceBlockKindResponse.ActionGroup, actionGroup: { agentRunId: "run-1", actionIds: [] } },
    ]);
    expect(surface).toEqual({ question: null, nextAction: null, actionGroups: [] });
  });
});

describe("ActionReviewGroup", () => {
  const item = (id: string, state: ActionReviewItem["state"], retryable = false): ActionReviewItem => ({
    id,
    state,
    retryable,
    content: <span>{`card-${id}`}</span>,
  });
  const confirmAll = () => screen.getByRole("button", { name: /actionGroup\.confirmAll$/ });

  it("renders only its content for a single item", () => {
    render(<ActionReviewGroup items={[item("a", "pending")]} confirm={vi.fn()} />);

    expect(screen.getByText("card-a")).toBeInTheDocument();
    expect(screen.queryByRole("button")).toBeNull();
    expect(screen.queryByText(/actionGroup\.title/)).toBeNull();
  });

  it("shows progress and every item's card", () => {
    render(<ActionReviewGroup items={[item("a", "done"), item("b", "pending")]} confirm={vi.fn()} />);

    expect(screen.getByText(/actionGroup\.progress/)).toBeInTheDocument();
    expect(screen.getByText("card-a")).toBeInTheDocument();
    expect(screen.getByText("card-b")).toBeInTheDocument();
  });

  it("confirms the pending items one by one in order, the next only after the previous resolved", async () => {
    const calls: string[] = [];
    const resolvers: Record<string, () => void> = {};
    const confirm = vi.fn((id: string) => {
      calls.push(id);
      return new Promise<void>((resolve) => { resolvers[id] = resolve; });
    });
    render(<ActionReviewGroup items={[item("a", "pending"), item("b", "done"), item("c", "pending"), item("d", "pending")]} confirm={confirm} />);

    fireEvent.click(confirmAll());
    await waitFor(() => expect(calls).toEqual(["a"]));
    resolvers.a!();
    await waitFor(() => expect(calls).toEqual(["a", "c"]));
    resolvers.c!();
    await waitFor(() => expect(calls).toEqual(["a", "c", "d"]));
    resolvers.d!();
    await waitFor(() => expect(confirmAll()).not.toBeDisabled());
    expect(confirm).toHaveBeenCalledTimes(3);
  });

  it("continues past a rejected confirm", async () => {
    const confirm = vi.fn((id: string) => (id === "a" ? Promise.reject(new Error("boom")) : Promise.resolve()));
    render(<ActionReviewGroup items={[item("a", "pending"), item("b", "pending")]} confirm={confirm} />);

    fireEvent.click(confirmAll());

    await waitFor(() => expect(confirm.mock.calls.map(([id]) => id)).toEqual(["a", "b"]));
  });

  it("is disabled while running so a double tap confirms each item once", async () => {
    let release: () => void = () => undefined;
    const confirm = vi.fn((id: string) => {
      void id;
      return new Promise<void>((resolve) => { release = resolve; });
    });
    render(<ActionReviewGroup items={[item("a", "pending"), item("b", "pending")]} confirm={confirm} />);

    fireEvent.click(confirmAll());
    await waitFor(() => expect(confirm).toHaveBeenCalledTimes(1));
    expect(confirmAll()).toBeDisabled();
    fireEvent.click(confirmAll());
    release();
    await waitFor(() => expect(confirm).toHaveBeenCalledTimes(2));
    release();
    await waitFor(() => expect(confirmAll()).not.toBeDisabled());

    expect(confirm.mock.calls.map(([id]) => id)).toEqual(["a", "b"]);
  });

  it("retries only the failed items that may be retried", async () => {
    const confirm = vi.fn((id: string) => {
      void id;
      return Promise.resolve();
    });
    render(
      <ActionReviewGroup
        items={[item("a", "failed", true), item("b", "failed", false), item("c", "done"), item("d", "closed"), item("e", "failed", true)]}
        confirm={confirm}
      />,
    );

    expect(screen.queryByRole("button", { name: /actionGroup\.confirmAll$/ })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: /actionGroup\.retryFailed$/ }));

    await waitFor(() => expect(confirm).toHaveBeenCalledTimes(2));
    expect(confirm.mock.calls.map(([id]) => id)).toEqual(["a", "e"]);
  });

  it("explains a partial outcome when nothing is pending and something is not done", () => {
    const { rerender } = render(<ActionReviewGroup items={[item("a", "done"), item("b", "closed")]} confirm={vi.fn()} />);
    expect(screen.getByRole("status")).toHaveTextContent(/actionGroup\.partial/);

    rerender(<ActionReviewGroup items={[item("a", "done"), item("b", "done")]} confirm={vi.fn()} />);
    expect(screen.queryByRole("status")).toBeNull();

    rerender(<ActionReviewGroup items={[item("a", "done"), item("b", "pending")]} confirm={vi.fn()} />);
    expect(screen.queryByRole("status")).toBeNull();
  });
});

describe("GoalChecklist", () => {
  const goals = [
    { key: "one", label: "First", done: true },
    { key: "two", label: "Second", done: false },
    { key: "three", label: "Third", done: false },
  ];

  it("lists the goals in order and highlights the next one", () => {
    render(<GoalChecklist goals={goals} nextGoal="two" />);

    const items = screen.getAllByRole("listitem");
    expect(items).toHaveLength(3);
    expect(items[0]).toHaveTextContent("First");
    expect(items[1]).toHaveTextContent("Second");
    expect(items[2]).toHaveTextContent("Third");
    expect(items[1]).toHaveAttribute("aria-current", "step");
    expect(items[0]).not.toHaveAttribute("aria-current");
    expect(items[2]).not.toHaveAttribute("aria-current");
  });

  it("selects only goals that are not done", () => {
    const onSelect = vi.fn();
    render(<GoalChecklist goals={goals} nextGoal="two" onSelect={onSelect} />);

    expect(screen.queryByRole("button", { name: /First/ })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: /Third/ }));

    expect(onSelect).toHaveBeenCalledWith("three");
    expect(onSelect).toHaveBeenCalledTimes(1);
  });

  it("offers no selection without a handler", () => {
    render(<GoalChecklist goals={goals} />);
    expect(screen.queryByRole("button")).toBeNull();
  });
});
