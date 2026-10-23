import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";

const session = vi.hoisted(() => ({ current: { isLoading: true, isAuthenticated: false } }));

vi.mock("../../hooks/useAuthSession", () => ({ useAuthSession: () => session.current }));
vi.mock("@vitarish/ui/layout", () => ({ StandaloneCenteredPage: ({ children }: { children: React.ReactNode }) => <>{children}</> }));

import { AuthenticatedRedirectBoundary } from "../../components/SessionRedirect";

function renderBoundary(onNavigate: (href: string) => void) {
  return render(
    <AuthenticatedRedirectBoundary authenticatedHref="/home" onNavigate={onNavigate} loadingFallback={<p>loading</p>}>
      <p>sign-in form</p>
    </AuthenticatedRedirectBoundary>,
  );
}

afterEach(() => {
  cleanup();
  session.current = { isLoading: true, isAuthenticated: false };
});

describe("AuthenticatedRedirectBoundary", () => {
  it("sends a visitor who arrives signed in away from the sign-in page", () => {
    const onNavigate = vi.fn();
    session.current = { isLoading: false, isAuthenticated: true };

    renderBoundary(onNavigate);

    expect(onNavigate).toHaveBeenCalledWith("/home");
    expect(screen.queryByText("sign-in form")).toBeNull();
  });

  it("leaves the navigation to the sign-in form when the visitor signs in on the page", () => {
    const onNavigate = vi.fn();
    session.current = { isLoading: false, isAuthenticated: false };
    const view = renderBoundary(onNavigate);
    expect(screen.getByText("sign-in form")).toBeTruthy();

    session.current = { isLoading: false, isAuthenticated: true };
    view.rerender(
      <AuthenticatedRedirectBoundary authenticatedHref="/home" onNavigate={onNavigate} loadingFallback={<p>loading</p>}>
        <p>sign-in form</p>
      </AuthenticatedRedirectBoundary>,
    );

    expect(onNavigate).not.toHaveBeenCalled();
    expect(screen.getByText("sign-in form")).toBeTruthy();
  });
});
