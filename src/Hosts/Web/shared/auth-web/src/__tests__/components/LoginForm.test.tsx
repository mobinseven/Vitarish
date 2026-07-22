import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { LoginForm } from "../../components/LoginForm";
import { LocalizationProvider } from "@vitarish/web-localization";

vi.mock("@vitarish/web-ui", () => ({
  StatusAlert: ({ message }: { message: string }) => <div>{message}</div>,
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <LocalizationProvider
        locale="en"
        messages={{
          auth: {
            login: {
              usernameOrEmailLabel: "Username or email",
              usernameOrEmailPlaceholder: "Enter",
              passwordLabel: "Password",
              passwordPlaceholder: "Enter",
              signInAction: "Sign in",
            },
          },
        }}
      >
        {children}
      </LocalizationProvider>
    </QueryClientProvider>
  );
}

describe("LoginForm", () => {
  afterEach(() => cleanup());
  beforeEach(() => {
    cleanup();
    queryClient.clear();
  });

  it("should render username and password fields", () => {
    render(<LoginForm />, { wrapper });

    expect(screen.getByLabelText(/username or email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /sign in/i }),
    ).toBeInTheDocument();
  });

  it("should render with correct input types", () => {
    render(<LoginForm />, { wrapper });

    const passwordInput = screen.getByLabelText(/password/i);
    expect(passwordInput).toHaveAttribute("type", "password");
  });
});
