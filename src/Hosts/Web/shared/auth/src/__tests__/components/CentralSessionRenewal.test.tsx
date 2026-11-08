import { act, cleanup, render, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, expect, it, vi } from "vitest";
import { CentralSessionRenewal } from "../../components/CentralSessionRenewal";
import { getAuthenticationSessionGetQueryKey } from "../../api/generated/authentication/authentication";

vi.mock("@vitarish/localization", () => ({ useLocalizedText: () => ({ t: (key: string) => key }) }));
vi.mock("@vitarish/ui", () => ({ StatusAlert: ({ message }: { message: string }) => <p>{message}</p> }));
afterEach(() => { cleanup(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });

it("renews Auth during authenticated app activity without navigation, referrer or tenant headers", async () => {
  const fetch = vi.fn().mockResolvedValue({ type: "opaque" });
  vi.stubGlobal("fetch", fetch);
  const now = vi.spyOn(performance, "now").mockReturnValue(1000);
  const client = new QueryClient();
  render(<QueryClientProvider client={client}><CentralSessionRenewal signInUrl="https://auth.example.test" /></QueryClientProvider>);
  act(() => { client.setQueryData(["public-page"], {}); });
  expect(fetch).not.toHaveBeenCalled();
  act(() => { client.setQueryData(getAuthenticationSessionGetQueryKey(), { isAuthenticated: true }); });
  await waitFor(() => expect(fetch).toHaveBeenCalledTimes(1));
  expect(fetch).toHaveBeenLastCalledWith(new URL(getAuthenticationSessionGetQueryKey()[0], "https://auth.example.test"), expect.objectContaining({
    mode: "no-cors", credentials: "include", referrerPolicy: "no-referrer", cache: "no-store",
  }));
  expect(fetch.mock.calls[0][1].headers).toBeUndefined();
  act(() => { client.setQueryData(["protected-work"], 1); });
  expect(fetch).toHaveBeenCalledTimes(1);
  now.mockReturnValue(31_001);
  act(() => { client.setQueryData(["protected-work"], 2); });
  await waitFor(() => expect(fetch).toHaveBeenCalledTimes(2));
});
