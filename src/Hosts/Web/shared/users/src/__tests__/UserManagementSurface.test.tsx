import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/localization";
import { UserManagementSurface, type UserManagementSurfaceProps } from "../components/UserManagementSurface";
import { usersMessageCatalogs } from "../localization/catalogs";

const refetch = vi.fn(async () => ({}));
const tenantProvision = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
const tenantRemove = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
const managedUserUpdate = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
const platformPhoneCreate = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
const globalRoleGrant = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
const inactivityPolicySave = vi.fn((_input: unknown, options?: { onSuccess?: () => void; onError?: (error: unknown) => void }) => options?.onSuccess?.());
let securityDetailsQueryResponse: { data?: typeof userSecurityDetails; error: unknown; isLoading: boolean; isError: boolean; refetch: typeof refetch };
const userSecurityDetails = {
  user: {
    id: "platform-user",
    userName: "user@example.com",
    firstName: "Platform",
    lastName: "User",
    email: "user@example.com",
    phoneNumber: null,
    fullName: "Platform User",
  },
  globalRoles: ["PlatformAdmin"],
  permittedGlobalRoles: ["PlatformAdmin"],
  tenantRoles: [{ tenantId: "tenant-a", roleSuffix: "TenantUser" }],
  securityHistory: {
    items: [{
      eventName: "users.security-attributes.changed",
      eventCategory: "security-attributes",
      outcome: "succeeded",
      actorId: "platform-admin-1",
      operation: "users.security-attributes.change",
      occurredAt: "2026-09-28T08:00:00Z",
      clientInterfaceHost: "admin.example.test",
      isAuthenticationEvent: false,
      redactedReason: "security_attribute_changed",
    }],
    page: 1,
    pageSize: 25,
    totalCount: 1,
  },
  authenticationHistory: {
    items: [{
      eventName: "authentication.passkey.login",
      eventCategory: "authentication",
      outcome: "denied",
      actorId: "platform-user",
      operation: "passkey.login",
      occurredAt: "2026-09-28T07:00:00Z",
      clientInterfaceHost: "admin.example.test",
      isAuthenticationEvent: true,
      redactedReason: "authentication_failed",
    }],
    page: 1,
    pageSize: 25,
    totalCount: 1,
  },
  securityHistoryAvailable: true,
  authenticationHistoryAvailable: true,
};

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
  useUsersAdministrationGetUserSecurity: () => securityDetailsQueryResponse,
  useUsersAdministrationUpdateUser: () => ({ isPending: false, mutate: managedUserUpdate }),
  useUsersAddRole: () => ({ isPending: false, mutate: globalRoleGrant }),
  useAuthenticationUserInactivityPolicyGet: () => ({ data: { version: 1, inactivityTimeoutMinutes: null }, isLoading: false, isError: false, refetch }),
  useAuthenticationUserInactivityPolicyPut: () => ({ isPending: false, mutate: inactivityPolicySave }),
  useUsersAdministrationCreateWithEmail: () => ({ isPending: false, mutate: vi.fn() }),
  useUsersAdministrationCreateWithPhone: () => ({ isPending: false, mutate: platformPhoneCreate }),
  useUsersAdministrationGrantTenantRole: () => ({ isPending: false, mutate: vi.fn() }),
  useUsersAdministrationRevokeTenantRole: () => ({ isPending: false, mutate: vi.fn() }),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  securityDetailsQueryResponse = { data: userSecurityDetails, error: null, isLoading: false, isError: false, refetch };
});

securityDetailsQueryResponse = { data: userSecurityDetails, error: null, isLoading: false, isError: false, refetch };

function renderSurface(mode: "tenant" | "platform", props: Partial<UserManagementSurfaceProps> = {}) {
  return render(
    <LocalizationProvider locale="en" messages={usersMessageCatalogs.en}>
      <UserManagementSurface mode={mode} {...props} />
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

  it("explains and submits E.164 phone identifiers for tenant user creation", async () => {
    const user = userEvent.setup();
    renderSurface("tenant");

    await user.selectOptions(screen.getByRole("combobox", { name: "Contact type" }), "phone");
    const phone = screen.getByRole("textbox", {
      name: "Phone number",
      description: "Use international format, starting with + and a country code.",
    });
    await user.type(phone, "+14155552671");
    await user.click(screen.getByRole("button", { name: "Add staff member" }));

    expect(tenantProvision).toHaveBeenCalledWith({ data: { phoneNumber: "+14155552671", firstName: undefined, lastName: undefined } }, expect.any(Object));
  });

  it("explains the E.164 phone format before platform user creation", async () => {
    const user = userEvent.setup();
    renderSurface("platform", { platformView: "create" });

    await user.selectOptions(screen.getByRole("combobox", { name: "Contact type" }), "phone");
    const phone = screen.getByRole("textbox", {
      name: "Phone number",
      description: "Use international format, starting with + and a country code.",
    });
    await user.type(phone, "+14155552671");
    await user.click(screen.getByRole("button", { name: "Create user" }));

    expect(platformPhoneCreate).toHaveBeenCalledWith({ data: { phoneNumber: "+14155552671", firstName: "", lastName: "" } }, expect.any(Object));
  });

  it("opens a user's security detail route from the directory action", async () => {
    const openDetails = vi.fn();
    renderSurface("platform", { onOpenUserDetails: openDetails });

    await userEvent.setup().click(screen.getByRole("button", { name: "View user security details" }));

    expect(openDetails).toHaveBeenCalledWith("platform-user");
  });

  it("shows identity, roles, security history, and authentication history in the detail view", () => {
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    expect(screen.getByText("User ID")).toBeInTheDocument();
    expect(screen.getAllByText("platform-user")).toHaveLength(2);
    expect(screen.getByText("PlatformAdmin")).toBeInTheDocument();
    expect(screen.getByText(/Tenant: tenant-a/)).toBeInTheDocument();
    expect(screen.getByText(/Role: TenantUser/)).toBeInTheDocument();
    expect(screen.getByText("Authentication history")).toBeInTheDocument();
    expect(screen.getByText("Passkey login")).toBeInTheDocument();
    expect(screen.getByText("User security attributes changed")).toBeInTheDocument();
    expect(screen.getAllByText("admin.example.test")).toHaveLength(2);
  });

  it("grants only a global role offered by the server and refreshes security details", async () => {
    const user = userEvent.setup();
    securityDetailsQueryResponse.data = { ...userSecurityDetails, globalRoles: [] };
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.selectOptions(screen.getByRole("combobox", { name: "Global role to grant" }), "PlatformAdmin");
    await user.click(screen.getByRole("button", { name: "Grant global role" }));

    expect(globalRoleGrant).toHaveBeenCalledWith({ userId: "platform-user", data: { roleName: "PlatformAdmin" } }, expect.any(Object));
    expect(refetch).toHaveBeenCalled();
    expect(screen.getByRole("status")).toHaveTextContent("Global role granted.");
  });

  it("saves a per-user inactivity override with the server version", async () => {
    const user = userEvent.setup();
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.type(screen.getByRole("spinbutton", { name: "Inactivity timeout (minutes)" }), "15");
    await user.click(screen.getByRole("button", { name: "Save inactivity timeout" }));

    expect(inactivityPolicySave).toHaveBeenCalledWith({
      userId: "platform-user",
      data: { inactivityTimeoutMinutes: 15, expectedVersion: 1 },
    }, expect.any(Object));
    expect(screen.getByRole("status")).toHaveTextContent("User inactivity timeout updated.");
  });

  it("clears a per-user inactivity override to inherit the server default", async () => {
    const user = userEvent.setup();
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.click(screen.getByRole("button", { name: "Save inactivity timeout" }));

    expect(inactivityPolicySave).toHaveBeenCalledWith({
      userId: "platform-user",
      data: { inactivityTimeoutMinutes: null, expectedVersion: 1 },
    }, expect.any(Object));
  });

  it("requires reload after an inactivity-policy conflict", async () => {
    const user = userEvent.setup();
    inactivityPolicySave.mockImplementationOnce((_input, options) => options?.onError?.({ status: 409 }));
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.type(screen.getByRole("spinbutton", { name: "Inactivity timeout (minutes)" }), "12");
    await user.click(screen.getByRole("button", { name: "Save inactivity timeout" }));

    expect(screen.getByRole("status")).toHaveTextContent("This setting changed elsewhere. Reload it before saving again.");
    await user.click(screen.getByRole("button", { name: "Reload current inactivity setting" }));
    expect(refetch).toHaveBeenCalled();
  });

  it("keeps user identity and roles visible when audit history returns 503", () => {
    securityDetailsQueryResponse = {
      data: undefined,
      error: {
        status: 503,
        problem: {
          ...userSecurityDetails,
          securityHistory: null,
          authenticationHistory: null,
          securityHistoryAvailable: false,
          authenticationHistoryAvailable: false,
        },
      },
      isLoading: false,
      isError: true,
      refetch,
    };
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    expect(screen.getByText("platform-user")).toBeInTheDocument();
    expect(screen.getByText("PlatformAdmin")).toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent("Audit history is temporarily unavailable. User details and roles are still shown.");
  });

  it("requires an identifier and saves an E.164 phone as the user's contact identifier", async () => {
    const user = userEvent.setup();
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    const email = screen.getByRole("textbox", { name: "Email address" });
    expect(screen.getByRole("textbox", {
      name: "Phone number",
      description: "Use international format, starting with + and a country code.",
    })).toBeInTheDocument();
    await user.clear(email);
    await user.click(screen.getByRole("button", { name: "Save user changes" }));

    expect(screen.getByRole("status")).toHaveTextContent("Enter a valid email address or E.164 phone number.");
    expect(managedUserUpdate).not.toHaveBeenCalled();

    await user.type(screen.getByRole("textbox", { name: "Phone number" }), "+14155552671");
    await user.click(screen.getByRole("button", { name: "Save user changes" }));

    expect(managedUserUpdate).toHaveBeenCalledWith({
      userId: "platform-user",
      data: {
        email: null,
        phoneNumber: "+14155552671",
        firstName: "Platform",
        lastName: "User",
      },
    }, expect.any(Object));
    expect(screen.getByRole("status")).toHaveTextContent("User details were updated.");
  });
});
