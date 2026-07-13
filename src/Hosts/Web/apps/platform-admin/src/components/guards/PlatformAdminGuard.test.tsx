import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@/localization";
import { PlatformAdminGuard } from "./PlatformAdminGuard";

const replace = vi.fn();
const useAuthSessionMock = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    replace,
    push: vi.fn(),
    prefetch: vi.fn(),
    back: vi.fn(),
    forward: vi.fn(),
    refresh: vi.fn(),
  }),
  usePathname: () => "/",
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock("@vitarish/auth-web", () => ({
  useAuthSession: () => useAuthSessionMock(),
  useLogout: () => ({ logout: vi.fn() }),
}));

function renderGuard() {
  return render(
    <LocalizationProvider locale="en">
      <PlatformAdminGuard>
        <section>Protected shell content</section>
      </PlatformAdminGuard>
    </LocalizationProvider>,
  );
}

function sessionState(overrides: Record<string, unknown>) {
  return {
    session: undefined,
    isAuthenticated: false,
    isLoading: false,
    isError: false,
    user: null,
    displayProfile: { displayLabel: "Test Admin", initials: "TA", secondaryLabel: null },
    ...overrides,
  };
}

describe("PlatformAdminGuard", () => {
  beforeEach(() => {
    replace.mockReset();
    useAuthSessionMock.mockReset();
  });

  it("shows a loading state while session access is being checked", () => {
    useAuthSessionMock.mockReturnValue(sessionState({ isLoading: true }));

    renderGuard();

    expect(screen.getByText("Loading...")).toBeInTheDocument();
    expect(screen.queryByText("Protected shell content")).not.toBeInTheDocument();
    expect(replace).not.toHaveBeenCalled();
  });

  it("redirects unauthenticated users to login without rendering guarded content", async () => {
    useAuthSessionMock.mockReturnValue(sessionState({ isAuthenticated: false }));

    renderGuard();

    expect(screen.queryByText("Protected shell content")).not.toBeInTheDocument();
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
  });

  it("shows a session error when session verification fails", () => {
    useAuthSessionMock.mockReturnValue(sessionState({ isError: true }));

    renderGuard();

    expect(screen.getByRole("heading", { name: "Session Error" })).toBeInTheDocument();
    expect(screen.getByText("Could not verify your session.")).toBeInTheDocument();
    expect(screen.queryByText("Protected shell content")).not.toBeInTheDocument();
  });

  it("shows forbidden state for authenticated users without platform-admin access", () => {
    useAuthSessionMock.mockReturnValue(
      sessionState({
        isAuthenticated: true,
        session: {
          isAuthenticated: true,
          user: null,
          tenantAccess: { canAccessTenantOwner: false, canAccessTenantUser: false },
          platformAccess: { canAccessPlatformAdmin: false },
        },
      }),
    );

    renderGuard();

    expect(screen.getByRole("heading", { name: "Access Denied" })).toBeInTheDocument();
    expect(screen.getByText("You do not have platform administrator access.")).toBeInTheDocument();
    expect(screen.queryByText("Protected shell content")).not.toBeInTheDocument();
  });

  it("redirects to login when a forbidden user chooses the sign-out action", async () => {
    useAuthSessionMock.mockReturnValue(
      sessionState({
        isAuthenticated: true,
        session: {
          isAuthenticated: true,
          user: null,
          tenantAccess: { canAccessTenantOwner: false, canAccessTenantUser: false },
          platformAccess: { canAccessPlatformAdmin: false },
        },
      }),
    );

    renderGuard();
    await userEvent.click(screen.getByRole("button", { name: "Sign out" }));

    expect(replace).toHaveBeenCalledWith("/login");
  });

  it("renders the platform admin shell for authenticated platform admins", () => {
    useAuthSessionMock.mockReturnValue(
      sessionState({
        isAuthenticated: true,
        user: { userId: "user-1", displayName: "Test Admin", givenName: null, familyName: null, email: null, phoneNumber: null },
        displayProfile: { displayLabel: "Test Admin", initials: "TA", secondaryLabel: null },
        session: {
          isAuthenticated: true,
          user: { userId: "user-1", displayName: "Test Admin", givenName: null, familyName: null, email: null, phoneNumber: null },
          tenantAccess: { canAccessTenantOwner: false, canAccessTenantUser: false },
          platformAccess: { canAccessPlatformAdmin: true },
        },
      }),
    );

    renderGuard();

    expect(screen.getAllByText("Platform Admin")).toHaveLength(2);
    expect(screen.getByRole("navigation", { name: "Overview" })).toBeInTheDocument();
    expect(screen.getByText("Protected shell content")).toBeInTheDocument();
  });
});
