import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/localization";
import { AuthorizationGuard } from "../../components/AuthorizationGuard";

const mocks = vi.hoisted(() => {
  const signedInWithoutAccess = { isAuthenticated: true, isLoading: false, hasAccess: false };
  return { logout: vi.fn(), signedInWithoutAccess, authorization: signedInWithoutAccess };
});
vi.mock("../../hooks/useTenantAuthorization", () => ({
  useTenantAuthorization: () => mocks.authorization,
}));
vi.mock("../../api/generated/authentication/authentication", () => ({
  useAuthenticationLogoutPost: () => ({ mutateAsync: mocks.logout, isPending: false }),
  useAuthenticationLogoutContext: () => ({ data: undefined, isLoading: false, isError: false, error: null }),
  getAuthenticationSessionGetQueryKey: () => ["session"],
}));
vi.mock("@tanstack/react-query", () => ({ useQueryClient: () => ({ removeQueries: vi.fn() }) }));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  mocks.authorization = mocks.signedInWithoutAccess;
  window.history.pushState({}, "", "/");
});

const guard = {
  accessDenied: "Access Denied",
  noPermission: "You do not have permission to access this area.",
  home: "Go to home",
  signOut: "Sign out",
};
const messages = { auth: { guard } };

function renderDenied(onNavigate: (href: string) => void, homeHref?: string) {
  render(
    <LocalizationProvider locale="en" messages={messages}>
      <AuthorizationGuard requiredRole="owner" loginHref="/sign-in" homeHref={homeHref} onNavigate={onNavigate}>
        <p>protected</p>
      </AuthorizationGuard>
    </LocalizationProvider>,
  );
}

it("explains the denial and lets a signed-in person leave to home or sign out", async () => {
  const navigate = vi.fn();
  mocks.logout.mockResolvedValue({ redirectUrl: "/signed-out" });
  renderDenied(navigate, "/home");

  expect(screen.getByRole("heading", { name: guard.accessDenied })).toBeVisible();
  expect(screen.getByText(guard.noPermission)).toBeVisible();
  expect(screen.queryByText("protected")).toBeNull();

  fireEvent.click(screen.getByRole("button", { name: guard.home }));
  expect(navigate).toHaveBeenCalledWith("/home");

  fireEvent.click(screen.getByRole("button", { name: guard.signOut }));
  await waitFor(() => expect(mocks.logout).toHaveBeenCalledWith({ data: { logoutId: null, returnUrl: "/home" } }));
  await waitFor(() => expect(navigate).toHaveBeenCalledWith("/signed-out"));
});

it("offers only sign-out when the app names no home", () => {
  renderDenied(vi.fn());
  expect(screen.queryByRole("button", { name: guard.home })).toBeNull();
  expect(screen.getByRole("button", { name: guard.signOut })).toBeVisible();
});

it("sends an unauthenticated person to sign-in carrying the page they opened", () => {
  mocks.authorization = { isAuthenticated: false, isLoading: false, hasAccess: false };
  window.history.pushState({}, "", "/patients/1?tab=x");
  const navigate = vi.fn();

  render(
    <LocalizationProvider locale="en" messages={messages}>
      <AuthorizationGuard requiredRole="owner" loginHref="/sign-in" onNavigate={navigate}>
        <p>protected</p>
      </AuthorizationGuard>
    </LocalizationProvider>,
  );

  expect(navigate).toHaveBeenCalledWith("/sign-in?returnUrl=%2Fpatients%2F1%3Ftab%3Dx");
  expect(screen.queryByText("protected")).toBeNull();
});
