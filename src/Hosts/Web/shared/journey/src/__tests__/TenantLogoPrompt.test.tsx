import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  profile: vi.fn(),
  dismiss: vi.fn(),
  setLogo: vi.fn(),
  upload: vi.fn(),
}));

vi.mock("../api/generated/parties/parties", () => ({
  getPartiesGetTenantProfileQueryKey: () => ["parties", "tenant-profile"],
  usePartiesGetTenantProfile: () => ({ data: mocks.profile() }),
  usePartiesDismissTenantLogoPrompt: () => ({ mutate: mocks.dismiss, isPending: false, isError: false }),
  partiesSetTenantLogo: mocks.setLogo,
}));

vi.mock("../features/tenantLogo/uploadTenantImage", async () => {
  const actual = await vi.importActual<typeof import("../features/tenantLogo/uploadTenantImage")>(
    "../features/tenantLogo/uploadTenantImage",
  );
  return { ...actual, uploadTenantImage: mocks.upload };
});

import { TenantLogoPrompt } from "../features/tenantLogo/TenantLogoPrompt";

// Without a message catalog, t() returns each key, so the keys name the controls.
const title = "journey.logoPrompt.title";
const uploadButton = "journey.logoPrompt.upload";
const dismissButton = "journey.logoPrompt.dismiss";

function renderPrompt() {
  const client = new QueryClient();
  return render(
    <QueryClientProvider client={client}>
      <TenantLogoPrompt />
    </QueryClientProvider>,
  );
}

function profile(overrides: Partial<{ logoUrl: string | null; logoPromptDismissed: boolean }> = {}) {
  return { name: "Clinic", description: null, category: null, businessDetails: null, logoUrl: null, logoPromptDismissed: false, ...overrides };
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("TenantLogoPrompt", () => {
  it.each([
    ["a user who cannot read the profile", undefined],
    ["a clinic that already has a logo", profile({ logoUrl: "/api/modules/files/logo/content" })],
    ["an owner who dismissed it", profile({ logoPromptDismissed: true })],
  ])("stays hidden for %s", (_, data) => {
    mocks.profile.mockReturnValue(data);
    renderPrompt();
    expect(screen.queryByText(title)).toBeNull();
  });

  it("asks an owner without a logo and records a dismissal on the server", async () => {
    mocks.profile.mockReturnValue(profile());
    renderPrompt();

    expect(screen.getByRole("heading", { name: title })).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: dismissButton }));

    expect(mocks.dismiss).toHaveBeenCalledTimes(1);
  });

  it("refuses a file that is not a displayable image without uploading it", async () => {
    mocks.profile.mockReturnValue(profile());
    const { container } = renderPrompt();
    const input = container.querySelector<HTMLInputElement>("input[type=file]")!;

    await userEvent.upload(input, new File(["%PDF"], "logo.pdf", { type: "application/pdf" }), { applyAccept: false });

    expect(await screen.findByText("journey.logoPrompt.failure.type")).toBeInTheDocument();
    expect(mocks.upload).not.toHaveBeenCalled();
    expect(mocks.setLogo).not.toHaveBeenCalled();
  });

  it("uploads an image and makes it the clinic's logo", async () => {
    mocks.profile.mockReturnValue(profile());
    mocks.upload.mockResolvedValue("file-1");
    mocks.setLogo.mockResolvedValue(undefined);
    const { container } = renderPrompt();
    const input = container.querySelector<HTMLInputElement>("input[type=file]")!;

    expect(screen.getByRole("button", { name: uploadButton })).toBeInTheDocument();
    await userEvent.upload(input, new File(["png"], "logo.png", { type: "image/png" }));

    await waitFor(() => expect(mocks.setLogo).toHaveBeenCalledWith({ fileId: "file-1" }));
  });
});
