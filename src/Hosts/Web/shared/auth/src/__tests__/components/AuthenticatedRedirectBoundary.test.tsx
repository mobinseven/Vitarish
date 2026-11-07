import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";

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
  it("preserves entered verification state through anonymous background session checks", () => {
    function VerificationForm() {
      const [code, setCode] = useState("");
      return <input aria-label="Verification code" value={code} onChange={(event) => setCode(event.target.value)} />;
    }
    const onNavigate = vi.fn();
    const content = () => (
      <AuthenticatedRedirectBoundary authenticatedHref="/home" onNavigate={onNavigate} loadingFallback={<p>loading</p>}>
        <VerificationForm />
      </AuthenticatedRedirectBoundary>
    );
    const view = render(content());
    expect(screen.queryByRole("textbox")).toBeNull();

    session.current = { isLoading: false, isAuthenticated: false };
    view.rerender(content());
    fireEvent.change(screen.getByRole("textbox"), { target: { value: "123" } });
    const input = screen.getByRole("textbox");

    session.current = { isLoading: true, isAuthenticated: false };
    view.rerender(content());
    expect(screen.getByRole("textbox")).toBe(input);
    expect(input).toHaveValue("123");

    session.current = { isLoading: false, isAuthenticated: false };
    view.rerender(content());
    expect(screen.getByRole("textbox")).toBe(input);
    expect(input).toHaveValue("123");
    expect(onNavigate).not.toHaveBeenCalled();
  });

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
