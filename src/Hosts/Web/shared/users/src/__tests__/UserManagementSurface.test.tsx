import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/localization";
import { UserManagementSurface } from "../components/UserManagementSurface";
import { usersMessageCatalogs } from "../localization/catalogs";

const refetch = vi.fn(async () => ({}));
const tenantProvision = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
const tenantRemove = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());

vi.mock("@vitarish/auth", () => ({
  useAuthSession: () => ({ isAuthenticated: true, isLoading: false, session: {} }),
  useTenantAuthorization: () => ({ hasAccess: true, isLoading: false }),
  usePlatformAuthorization: () => ({ hasAccess: true, isLoading: false }),
  useUsersListTenantMembers: () => ({ isLoading: false, isError: false, refetch, data: [
    { user: { id: "owner", fullName: "Clinic Owner", email: "owner@example.com", phoneNumber: null }, roleSuffix: "TenantOwner" },
    { user: { id: "staff", fullName: "Clinic Staff", email: "staff@example.com", phoneNumber: null }, roleSuffix: "TenantUser" },
  ] }),
  useUsersProvisionTenantMember: () => ({ isPending: false, mutate: tenantProvision }),
  useUsersRemoveTenantMember: () => ({ isPending: false, mutate: tenantRemove }),
  useUsersSearchUsers: () => ({ isLoading: false, isError: false, refetch, data: [
    { id: "platform-user", fullName: "Platform User", email: "user@example.com", phoneNumber: null },
  ] }),
  useUsersAdministrationListTenantRoles: () => ({ isLoading: false, isError: false, refetch, data: [
    { tenantId: "tenant-a", roleSuffix: "TenantUser", assigned: true },
  ] }),
  useUsersAdministrationCreateWithEmail: () => ({ isPending: false, mutate: vi.fn() }),
  useUsersAdministrationCreateWithPhone: () => ({ isPending: false, mutate: vi.fn() }),
  useUsersAdministrationGrantTenantRole: () => ({ isPending: false, mutate: vi.fn() }),
  useUsersAdministrationRevokeTenantRole: () => ({ isPending: false, mutate: vi.fn() }),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function renderSurface(mode: "tenant" | "platform") {
  return render(
    <LocalizationProvider locale="en" messages={usersMessageCatalogs.en}>
      <UserManagementSurface mode={mode} />
    </LocalizationProvider>,
  );
}

describe("UserManagementSurface", () => {
  it("shows the owner and staff while exposing removal and refresh only for staff", async () => {
    const user = userEvent.setup();
    vi.spyOn(window, "confirm").mockReturnValue(true);
    renderSurface("tenant");

    expect(screen.getByText("Clinic Owner")).toBeInTheDocument();
    expect(screen.getByText(/Owner · owner@example.com/)).toBeInTheDocument();
    expect(screen.getByText("Clinic Staff")).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "Remove access" })).toHaveLength(1);
    expect(screen.queryByText("Role to grant")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Remove access" }));
    expect(tenantRemove).toHaveBeenCalledWith({ userId: "staff" }, expect.any(Object));
    expect(refetch).toHaveBeenCalled();
  });

  it("uses the tenant provisioning contract and refreshes after a successful add", async () => {
    const user = userEvent.setup();
    renderSurface("tenant");

    await user.type(screen.getByRole("textbox", { name: "Email address" }), "new@example.com");
    await user.click(screen.getByRole("button", { name: "Add staff member" }));

    expect(tenantProvision).toHaveBeenCalledWith({ data: { email: "new@example.com", firstName: undefined, lastName: undefined } }, expect.any(Object));
    expect(refetch).toHaveBeenCalled();
    expect(screen.getByRole("status")).toHaveTextContent("The account is now a tenant member.");
  });

  it("keeps platform search and membership inspection on the shared surface", async () => {
    renderSurface("platform");
    expect(screen.getByText("Platform User")).toBeInTheDocument();

    await userEvent.setup().click(screen.getByText("Platform User"));
    expect(screen.getByText(/Tenant: tenant-a/)).toBeInTheDocument();
    expect(screen.getByText(/Role: TenantUser/)).toBeInTheDocument();
  });
});
