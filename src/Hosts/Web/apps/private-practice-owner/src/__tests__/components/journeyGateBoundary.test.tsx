import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@vitarish/api-client";
import { signInReturnUrlQueryKey } from "@vitarish/journey/sign-in-host";
import { Routes } from "@/app/routes";

const signedInSession = { isAuthenticated: true, isLoading: false };

const mocks = vi.hoisted(() => ({
  pathname: "",
  search: "",
  push: vi.fn(),
  replace: vi.fn(),
  useCurrentJourney: vi.fn(),
  session: { isAuthenticated: true, isLoading: false } as { isAuthenticated: boolean; isLoading: boolean },
}));

vi.mock("next/navigation", () => ({
  usePathname: () => mocks.pathname,
  useRouter: () => ({ push: mocks.push, replace: mocks.replace }),
  useSearchParams: () => new URLSearchParams(mocks.search),
}));

vi.mock("@/hooks/journey/useCurrentJourney", () => ({
  useCurrentJourney: mocks.useCurrentJourney,
}));

vi.mock("@vitarish/auth", async () => ({
  ...(await vi.importActual<typeof import("@vitarish/auth")>("@vitarish/auth")),
  useAuthSession: () => mocks.session,
}));

vi.mock("@vitarish/journey", () => ({
  JourneyErrorState: ({ onRetry }: { onRetry: () => void }) => (
    <div>
      <p>Journey error</p>
      <button type="button" onClick={onRetry}>Retry journey</button>
    </div>
  ),
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
  mocks.session = signedInSession;
  window.history.pushState({}, "", "/");
});

beforeEach(() => {
  mocks.pathname = profilePath;
});

function journeyError(error: Error) {
  return { data: undefined, isLoading: false, error, refetch: vi.fn() };
}

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

describe("JourneyGateBoundary sign-in for signed-out visitors", () => {
  it("sends a signed-out visitor to sign-in with the page they opened, without the error card", () => {
    window.history.pushState({}, "", "/patients/1?tab=x");
    mocks.session = { isAuthenticated: false, isLoading: false };
    mocks.useCurrentJourney.mockReturnValue(journeyError(new Error("Journey unavailable")));

    render(
      <JourneyGateBoundary>
        <div>Profile completion</div>
      </JourneyGateBoundary>,
    );

    expect(mocks.replace).toHaveBeenCalledWith(`/login?returnUrl=${encodeURIComponent("/patients/1?tab=x")}`);
    expect(screen.getByText("Journey loading")).toBeInTheDocument();
    expect(screen.queryByText("Journey error")).not.toBeInTheDocument();
    expect(screen.queryByText("Profile completion")).not.toBeInTheDocument();
  });

  it("sends a visitor whose journey request is unauthorized to sign-in, without the error card", () => {
    window.history.pushState({}, "", "/patients/1?tab=x");
    mocks.useCurrentJourney.mockReturnValue(journeyError(new ApiError({ status: 401, title: "Unauthorized" })));

    render(
      <JourneyGateBoundary>
        <div>Profile completion</div>
      </JourneyGateBoundary>,
    );

    expect(mocks.replace).toHaveBeenCalledWith(`/login?returnUrl=${encodeURIComponent("/patients/1?tab=x")}`);
    expect(screen.getByText("Journey loading")).toBeInTheDocument();
    expect(screen.queryByText("Journey error")).not.toBeInTheDocument();
  });

  it("keeps the error card with retry for a server failure and does not redirect", () => {
    const query = journeyError(new ApiError({ status: 500, title: "Server error" }));
    mocks.useCurrentJourney.mockReturnValue(query);

    render(
      <JourneyGateBoundary>
        <div>Profile completion</div>
      </JourneyGateBoundary>,
    );

    expect(screen.getByText("Journey error")).toBeInTheDocument();
    expect(screen.queryByText("Profile completion")).not.toBeInTheDocument();
    expect(mocks.replace).not.toHaveBeenCalled();
    expect(mocks.push).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: "Retry journey" }));
    expect(query.refetch).toHaveBeenCalledTimes(1);
  });
});
