import { cleanup, render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { signInReturnUrlQueryKey } from "../signInHost";

const mocks = vi.hoisted(() => ({ issueHandoff: vi.fn() }));

vi.mock("../api/generated/parties/parties", () => ({
  partiesIssueSignInHomeHandoff: mocks.issueHandoff,
}));
vi.mock("../components/JourneyGateLayout", () => ({
  JourneyGateLayout: ({ children }: { children: ReactNode }) => children,
}));

import { SignInContinuePage } from "../pages/SignInContinuePage";

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.history.replaceState({}, "", window.location.pathname);
});

describe("SignInContinuePage", () => {
  it("offers sign-in recovery with a validated return intent when handoff issuance fails", async () => {
    mocks.issueHandoff.mockRejectedValue(new Error("handoff unavailable"));
    window.history.replaceState({}, "", `?${signInReturnUrlQueryKey}=%2Fstudio`);

    render(<SignInContinuePage signInPath="/login" />);

    const signInAgain = await screen.findByRole("link", { name: "journey.continue.signInAgain" });
    expect(signInAgain).toHaveAttribute("href", `/login?${signInReturnUrlQueryKey}=%2Fstudio`);
  });

  it("does not carry an unsafe return intent to sign-in recovery", async () => {
    mocks.issueHandoff.mockRejectedValue(new Error("handoff unavailable"));
    window.history.replaceState({}, "", `?${signInReturnUrlQueryKey}=%2F%09%2Fevil.example`);

    render(<SignInContinuePage signInPath="/login" />);

    const signInAgain = await screen.findByRole("link", { name: "journey.continue.signInAgain" });
    expect(signInAgain).toHaveAttribute("href", "/login");
  });
});
