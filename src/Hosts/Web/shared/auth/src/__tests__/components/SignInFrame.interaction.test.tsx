import { cleanup, render, screen, fireEvent } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { ReactNode } from "react";
import { SignInFrame } from "../../components/SignInFrame";
import { AuthenticationWebRoutes } from "../../routes";
import { LocalizationProvider } from "@vitarish/localization";
import { ApiError } from "@vitarish/api-client";
import { authMessageCatalogs } from "../../localization/catalogs";
import { authenticationErrorCodes } from "../../auth/errors";

const session = vi.hoisted(() => ({ isLoading: true, isAuthenticated: false, isError: false, error: undefined as unknown }));
const departing = vi.hoisted(() => vi.fn());
vi.mock("../../hooks/useAuthSession", () => ({ useAuthSession: () => session }));
vi.mock("@vitarish/ui/navigation-runtime", async importOriginal => ({
  ...await importOriginal<typeof import("@vitarish/ui/navigation-runtime")>(),
  navigationTransitionRuntime: () => ({ departing }),
}));

afterEach(() => { cleanup(); vi.clearAllMocks(); session.isLoading = true; session.isAuthenticated = false; session.isError = false; session.error = undefined; });

function Frame({ children }: { children: ReactNode }) { return <div data-testid="interactive-chrome">{children}</div>; }
function fixture(navigate = vi.fn(), continueOnSignInHost = true) {
  return <LocalizationProvider locale="en" messages={authMessageCatalogs.en}><SignInFrame continueOnSignInHost={continueOnSignInHost} searchParams={new URLSearchParams()} onNavigate={navigate} homeHref="/home" continueHref={AuthenticationWebRoutes.Continue} frame={Frame} loadingFallback={<p role="status">Discovering session</p>}>
    <input aria-label="Verification code" defaultValue="" />
  </SignInFrame></LocalizationProvider>;
}

describe("sign-in discovery and interaction", () => {
  it("withholds interactive chrome during discovery, then preserves an entered challenge across refetch", () => {
    const result = render(fixture());
    expect(screen.queryByTestId("interactive-chrome")).toBeNull();
    session.isLoading = false;
    result.rerender(fixture());
    const input = screen.getByRole("textbox", { name: "Verification code" });
    fireEvent.change(input, { target: { value: "123456" } });
    session.isLoading = true;
    result.rerender(fixture());
    expect(screen.getByRole("textbox", { name: "Verification code" })).toBe(input);
    expect(input).toHaveValue("123456");
    expect(screen.queryByRole("status")).toBeNull();
  });

  it("continues an initially signed-in visitor without mounting the interactive frame", () => {
    session.isLoading = false;
    session.isAuthenticated = true;
    const navigate = vi.fn();
    render(fixture(navigate));
    expect(navigate).toHaveBeenCalledExactlyOnceWith(AuthenticationWebRoutes.Continue);
    expect(departing).toHaveBeenCalledOnce();
    expect(departing.mock.invocationCallOrder[0]).toBeLessThan(navigate.mock.invocationCallOrder[0]);
    expect(screen.queryByTestId("interactive-chrome")).toBeNull();
    expect(screen.queryByRole("textbox")).toBeNull();
  });

  it("does not retain a departing cover when standalone sign-in navigates to its final app", () => {
    session.isLoading = false;
    session.isAuthenticated = true;
    const navigate = vi.fn();
    render(fixture(navigate, false));
    expect(navigate).toHaveBeenCalledExactlyOnceWith("/home");
    expect(departing).not.toHaveBeenCalled();
  });

  it("shows an initial dependency failure without presenting credentials or redirecting", () => {
    session.isLoading = false;
    session.isError = true;
    session.error = new ApiError({ status: 503, code: authenticationErrorCodes.providerFailure });
    const navigate = vi.fn();
    render(fixture(navigate));
    expect(screen.getByRole("alert")).toHaveTextContent(authMessageCatalogs.en.auth.failures.providerFailure);
    expect(screen.queryByRole("textbox")).toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  it("preserves the active challenge on a background dependency failure", () => {
    session.isLoading = false;
    const view = render(fixture());
    const input = screen.getByRole("textbox", { name: "Verification code" });
    fireEvent.change(input, { target: { value: "123456" } });
    session.isError = true;
    session.error = new ApiError({ status: 503, code: authenticationErrorCodes.providerFailure });
    view.rerender(fixture());
    expect(screen.getByRole("textbox", { name: "Verification code" })).toBe(input);
    expect(input).toHaveValue("123456");
  });
});
