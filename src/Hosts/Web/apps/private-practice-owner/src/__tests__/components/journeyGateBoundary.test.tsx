import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { signInReturnUrlQueryKey } from "@vitarish/journey/sign-in-host";
import { Routes } from "@/app/routes";

const mocks = vi.hoisted(() => ({
  pathname: "",
  search: "",
  push: vi.fn(),
  useCurrentJourney: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  usePathname: () => mocks.pathname,
  useRouter: () => ({ push: mocks.push }),
  useSearchParams: () => new URLSearchParams(mocks.search),
}));

vi.mock("@/hooks/journey/useCurrentJourney", () => ({
  useCurrentJourney: mocks.useCurrentJourney,
}));

vi.mock("@vitarish/auth", async () => ({
  ...(await vi.importActual<typeof import("@vitarish/auth")>("@vitarish/auth")),
  // These cases are about a signed-in person's return intent.
  useAuthSession: () => ({ isAuthenticated: true, isLoading: false }),
}));

vi.mock("@vitarish/journey", () => ({
  JourneyErrorState: () => <div>Journey error</div>,
  JourneyLoadingState: () => <div>Journey loading</div>,
}));

import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";

const profilePath = Routes.Account.ProfileComplete;

function profileGate(destinationUrl: string) {
  return {
    data: {
      step: {
        destinationUrl,
        blockingGate: {
          isBlocking: true,
          destinationUrl,
        },
      },
    },
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  };
}

afterEach(() => {
  vi.clearAllMocks();
  mocks.pathname = profilePath;
  mocks.search = "";
});

beforeEach(() => {
  mocks.pathname = profilePath;
});

describe("JourneyGateBoundary return intent", () => {
  it("requests and stays on the profile gate carrying the safe return path", () => {
    mocks.search = `${signInReturnUrlQueryKey}=%2Fstudio&tab=profile`;
    mocks.useCurrentJourney.mockReturnValue(
      profileGate(`${profilePath}?${signInReturnUrlQueryKey}=%2Fstudio`),
    );

    render(
      <JourneyGateBoundary>
        <div>Profile completion</div>
      </JourneyGateBoundary>,
    );

    expect(mocks.useCurrentJourney).toHaveBeenCalledWith("/studio");
    expect(screen.getByText("Profile completion")).toBeInTheDocument();
    expect(mocks.push).not.toHaveBeenCalled();
  });

  it("does not send an external return target to the journey API", () => {
    mocks.search = `${signInReturnUrlQueryKey}=${encodeURIComponent("//evil.example")}`;
    mocks.useCurrentJourney.mockReturnValue(profileGate(profilePath));

    render(
      <JourneyGateBoundary>
        <div>Profile completion</div>
      </JourneyGateBoundary>,
    );

    expect(mocks.useCurrentJourney).toHaveBeenCalledWith(undefined);
    expect(screen.getByText("Profile completion")).toBeInTheDocument();
    expect(mocks.push).not.toHaveBeenCalled();
  });
});
