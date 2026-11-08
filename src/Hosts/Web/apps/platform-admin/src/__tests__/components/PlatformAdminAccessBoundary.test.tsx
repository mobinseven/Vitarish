import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthenticationSearchParams, AuthenticationWebRoutes } from "@vitarish/auth/routes";
import { Routes } from "@/app/routes";
import { PlatformAdminAccessBoundary } from "@/components/PlatformAdminAccessBoundary";

const state = vi.hoisted(() => ({
  pathname: "/", replace: vi.fn(), isAuthenticated: false, hasAccess: false,
}));
vi.mock("next/navigation", () => ({ usePathname: () => state.pathname, useRouter: () => ({ replace: state.replace }) }));
vi.mock("@vitarish/auth", () => ({
  useAuthSession: () => ({ isAuthenticated: state.isAuthenticated, isLoading: false, isError: false }),
  usePlatformAuthorization: () => ({ hasAccess: state.hasAccess }),
}));
vi.mock("@vitarish/localization", () => ({ useLocalizedText: () => ({ t: (key: string) => key }) }));
vi.mock("@vitarish/ui", () => ({
  CenteredRegion: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  LoadingSpinner: () => <span>Loading</span>, FaIcon: () => null,
}));

describe("PlatformAdminAccessBoundary", () => {
  beforeEach(() => {
    state.pathname = Routes.Root;
    state.isAuthenticated = false;
    state.hasAccess = false;
    state.replace.mockReset();
  });
  afterEach(cleanup);

  it("allows the anonymous handoff to redeem before requiring an app session", () => {
    state.pathname = AuthenticationWebRoutes.Handoff;
    render(<PlatformAdminAccessBoundary><p>Ticket redemption</p></PlatformAdminAccessBoundary>);
    expect(screen.getByText("Ticket redemption")).toBeVisible();
    expect(state.replace).not.toHaveBeenCalled();
  });

  it("still redirects an anonymous protected page without exposing its content", async () => {
    state.pathname = Routes.Users.Index;
    render(<PlatformAdminAccessBoundary><p>Protected users</p></PlatformAdminAccessBoundary>);
    const query = new URLSearchParams({ [AuthenticationSearchParams.ReturnUrl]: Routes.Users.Index });
    await waitFor(() => expect(state.replace).toHaveBeenCalledWith(`${Routes.Login}?${query}`));
    expect(screen.queryByText("Protected users")).not.toBeInTheDocument();
  });

  it("still refuses protected content to a signed-in person without administrator access", () => {
    state.pathname = Routes.Users.Index;
    state.isAuthenticated = true;
    render(<PlatformAdminAccessBoundary><p>Protected users</p></PlatformAdminAccessBoundary>);
    expect(screen.queryByText("Protected users")).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "admin.forbiddenTitle" })).toBeVisible();
    expect(state.replace).not.toHaveBeenCalled();
  });
});
