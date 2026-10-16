import { UsersCommandErrorKind } from "@vitarish/auth/users";
import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/localization";
import { UserManagementSurface, type UserManagementSurfaceProps } from "../components/UserManagementSurface";
import { usersMessageCatalogs } from "../localization/catalogs";

const refetch = vi.fn(async () => ({}));
const tenantProvision = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
const tenantRemove = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
const managedUserUpdate = vi.fn((_input: unknown, options?: { onSuccess?: () => void; onError?: (error: unknown) => void }) => options?.onSuccess?.());
const platformPhoneCreate = vi.fn((_input: unknown, options?: { onSuccess?: () => void; onError?: (error: unknown) => void }) => options?.onSuccess?.());
const globalRoleGrant = vi.fn((_input: unknown, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
const inactivityPolicySave = vi.fn((_input: unknown, options?: { onSuccess?: () => void; onError?: (error: unknown) => void }) => options?.onSuccess?.());
const revokeSessions = vi.fn((_input: unknown, options?: { onSuccess?: (result: { endedSessions: number }) => void; onError?: (error: unknown) => void }) => options?.onSuccess?.({ endedSessions: 2 }));
const disableUser = vi.fn((_input: unknown, options?: { onSuccess?: () => void; onError?: (error: unknown) => void }) => options?.onSuccess?.());
const enableUser = vi.fn((_input: unknown, options?: { onSuccess?: () => void; onError?: (error: unknown) => void }) => options?.onSuccess?.());
const securityQueryParams = vi.fn();
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
    revision: "server-revision-1",
    isEnabled: true,
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
  useUsersAdministrationGetUserSecurity: (_userId: string, params: unknown) => {
    securityQueryParams(params);
    return securityDetailsQueryResponse;
  },
  useUsersAdministrationUpdateUser: () => ({ isPending: false, mutate: managedUserUpdate }),
  useUsersAddRole: () => ({ isPending: false, mutate: globalRoleGrant }),
  useAuthenticationUserInactivityPolicyGet: () => ({ data: { version: 1, inactivityTimeoutMinutes: null }, isLoading: false, isError: false, refetch }),
  useAuthenticationUserInactivityPolicyPut: () => ({ isPending: false, mutate: inactivityPolicySave }),
  useAuthenticationUserSessionsRevoke: () => ({ isPending: false, mutate: revokeSessions }),
  useUsersDisableUser: () => ({ isPending: false, mutate: disableUser }),
  useUsersEnableUser: () => ({ isPending: false, mutate: enableUser }),
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

function historySection(name: string): HTMLElement {
  const section = screen.getByRole("heading", { name }).closest("section");
  if (!section) throw new Error(`The ${name} section is not rendered.`);
  return section;
}

function withHistoryTotals(securityTotal: number, authenticationTotal: number): typeof userSecurityDetails {
  return {
    ...userSecurityDetails,
    securityHistory: { ...userSecurityDetails.securityHistory, totalCount: securityTotal },
    authenticationHistory: { ...userSecurityDetails.authenticationHistory, totalCount: authenticationTotal },
  };
}

function accountStatusSection(): HTMLElement {
  const heading = screen.getByRole("heading", { name: "Account status" });
  const section = heading.closest("section");
  if (!section) throw new Error("The account status section is not rendered.");
  return section;
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

  it("explains and submits phone identifiers for tenant user creation", async () => {
    const user = userEvent.setup();
    renderSurface("tenant");

    await user.selectOptions(screen.getByRole("combobox", { name: "Contact type" }), "phone");
    const phone = screen.getByRole("textbox", {
      name: "Phone number",
      description: "Mobile number, for example 0912 345 6789.",
    });
    await user.type(phone, "+14155552671");
    await user.click(screen.getByRole("button", { name: "Add staff member" }));

    expect(tenantProvision).toHaveBeenCalledWith({ data: { phoneNumber: "+14155552671", firstName: undefined, lastName: undefined } }, expect.any(Object));
  });

  it("explains the mobile number format before platform user creation", async () => {
    const user = userEvent.setup();
    renderSurface("platform", { platformView: "create" });

    await user.selectOptions(screen.getByRole("combobox", { name: "Contact type" }), "phone");
    const phone = screen.getByRole("textbox", {
      name: "Phone number",
      description: "Mobile number, for example 0912 345 6789.",
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

  it("pages the sign-in history on its own without moving the security history", async () => {
    const user = userEvent.setup();
    securityDetailsQueryResponse = { ...securityDetailsQueryResponse, data: withHistoryTotals(60, 60) };
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.click(within(historySection("Authentication history")).getByRole("button", { name: "Older events" }));

    expect(securityQueryParams).toHaveBeenLastCalledWith({ page: 1, authenticationPage: 2, pageSize: 25 });
  });

  it("pages the security history on its own without moving the sign-in history", async () => {
    const user = userEvent.setup();
    securityDetailsQueryResponse = { ...securityDetailsQueryResponse, data: withHistoryTotals(60, 60) };
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.click(within(historySection("Security history")).getByRole("button", { name: "Older events" }));

    expect(securityQueryParams).toHaveBeenLastCalledWith({ page: 2, authenticationPage: 1, pageSize: 25 });
    expect(within(historySection("Security history")).getByText("Page 2 of 3")).toBeInTheDocument();
  });

  it("offers a pager only for a history that spans more than one page", () => {
    securityDetailsQueryResponse = { ...securityDetailsQueryResponse, data: withHistoryTotals(60, 1) };
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    expect(within(historySection("Security history")).getByRole("button", { name: "Older events" })).toBeInTheDocument();
    expect(within(historySection("Authentication history")).queryByRole("button", { name: "Older events" })).not.toBeInTheDocument();
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

  it("ends every active session after confirmation and reports the ended count", async () => {
    const user = userEvent.setup();
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.click(screen.getByRole("button", { name: "End all sessions" }));
    expect(screen.getByRole("dialog")).toHaveAccessibleName("End all sessions");
    await user.click(screen.getByRole("button", { name: "End sessions" }));

    expect(revokeSessions).toHaveBeenCalledWith({ userId: "platform-user" }, expect.any(Object));
    expect(refetch).toHaveBeenCalled();
    expect(screen.getByRole("status")).toHaveTextContent("Active sessions ended: 2");
  });

  it("reports a forbidden session-termination attempt without claiming success", async () => {
    const user = userEvent.setup();
    revokeSessions.mockImplementationOnce(
      (_input: unknown, options?: { onError?: (error: unknown) => void }) => options?.onError?.({ status: 403 }),
    );
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.click(screen.getByRole("button", { name: "End all sessions" }));
    await user.click(screen.getByRole("button", { name: "End sessions" }));

    expect(screen.getByRole("status")).toHaveTextContent("You do not have permission to end this user's sessions.");
    expect(refetch).not.toHaveBeenCalled();
  });

  it("disables an enabled account only after confirmation and reports the new status", async () => {
    const user = userEvent.setup();
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    expect(accountStatusSection()).toHaveTextContent("Enabled");
    await user.click(screen.getByRole("button", { name: "Disable account" }));
    const dialog = screen.getByRole("dialog");
    expect(dialog).toHaveAccessibleName("Disable account");
    await user.click(within(dialog).getByRole("button", { name: "Disable account" }));

    expect(disableUser).toHaveBeenCalledWith({ userId: "platform-user" }, expect.any(Object));
    expect(enableUser).not.toHaveBeenCalled();
    expect(refetch).toHaveBeenCalled();
    expect(screen.getByRole("status")).toHaveTextContent("The account is disabled.");
  });

  it("points to ending sessions when a signed-in user cannot be disabled", async () => {
    const user = userEvent.setup();
    disableUser.mockImplementationOnce(
      (_input: unknown, options?: { onError?: (error: unknown) => void }) => options?.onError?.({
        status: 409,
        problem: { status: 409, errors: [{ code: "ActiveSessionConflict", failureCode: UsersCommandErrorKind.ActiveSessionConflict }] },
      }),
    );
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.click(screen.getByRole("button", { name: "Disable account" }));
    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Disable account" }));

    expect(screen.getByRole("status")).toHaveTextContent("This user is signed in. End all sessions before disabling the account.");
    expect(refetch).toHaveBeenCalled();
  });

  it("reports a concurrent edit, not a signed-in user, when disabling hits a concurrency conflict", async () => {
    const user = userEvent.setup();
    disableUser.mockImplementationOnce(
      (_input: unknown, options?: { onError?: (error: unknown) => void }) => options?.onError?.({
        status: 409,
        problem: { status: 409, errors: [{ code: "ConcurrencyFailure", failureCode: UsersCommandErrorKind.ConcurrencyFailure }] },
      }),
    );
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.click(screen.getByRole("button", { name: "Disable account" }));
    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Disable account" }));

    expect(screen.getByRole("status")).toHaveTextContent(
      "This account changed elsewhere. The details were reloaded; review them and save again.",
    );
    expect(refetch).toHaveBeenCalled();
  });

  it("enables a disabled account after confirmation without a session precondition", async () => {
    const user = userEvent.setup();
    securityDetailsQueryResponse.data = {
      ...userSecurityDetails,
      user: { ...userSecurityDetails.user, isEnabled: false },
    };
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    expect(accountStatusSection()).toHaveTextContent("Disabled");
    await user.click(screen.getByRole("button", { name: "Enable account" }));
    const dialog = screen.getByRole("dialog");
    expect(dialog).toHaveAccessibleName("Enable account");
    await user.click(within(dialog).getByRole("button", { name: "Enable account" }));

    expect(enableUser).toHaveBeenCalledWith({ userId: "platform-user" }, expect.any(Object));
    expect(disableUser).not.toHaveBeenCalled();
    expect(refetch).toHaveBeenCalled();
    expect(screen.getByRole("status")).toHaveTextContent("The account is enabled.");
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

  it("requires an identifier and saves the typed phone as the user's contact identifier", async () => {
    const user = userEvent.setup();
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    const email = screen.getByRole("textbox", { name: "Email address" });
    expect(screen.getByRole("textbox", {
      name: "Phone number",
      description: "Mobile number, for example 0912 345 6789.",
    })).toBeInTheDocument();
    await user.clear(email);
    await user.click(screen.getByRole("button", { name: "Save user changes" }));

    expect(screen.getByRole("status")).toHaveTextContent("Enter a valid email address or mobile number.");
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
        revision: "server-revision-1",
      },
    }, expect.any(Object));
    expect(screen.getByRole("status")).toHaveTextContent("User details were updated.");
  });

  it("rejects a stale save with a conflict message and reloads the account", async () => {
    const user = userEvent.setup();
    managedUserUpdate.mockImplementationOnce(
      (_input: unknown, options?: { onError?: (error: unknown) => void }) => options?.onError?.({ status: 409 }),
    );
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.click(screen.getByRole("button", { name: "Save user changes" }));

    expect(screen.getByRole("status")).toHaveTextContent(
      "This account changed elsewhere. The details were reloaded; review them and save again.",
    );
    expect(refetch).toHaveBeenCalled();
  });

  it("explains a duplicate contact on save, keeps the form, and does not reload", async () => {
    const user = userEvent.setup();
    managedUserUpdate.mockImplementationOnce(
      (_input: unknown, options?: { onError?: (error: unknown) => void }) => options?.onError?.({
        status: 400,
        problem: {
          status: 400,
          errors: [
            { code: "ContactIdentifierInUse", description: "Another account already uses this email address or phone number.", failureCode: "ContactIdentifierInUse" },
          ],
        },
      }),
    );
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.clear(screen.getByRole("textbox", { name: "Email address" }));
    await user.type(screen.getByRole("textbox", { name: "Email address" }), "taken@example.test");
    await user.click(screen.getByRole("button", { name: "Save user changes" }));

    expect(screen.getByRole("status")).toHaveTextContent(
      "Another account already uses this email address or phone number.",
    );
    expect(refetch).not.toHaveBeenCalled();
    expect(screen.getByRole("textbox", { name: "Email address" })).toHaveValue("taken@example.test");
  });

  it("shows a localized phone-field error for an invalid managed contact", async () => {
    const user = userEvent.setup();
    managedUserUpdate.mockImplementationOnce(
      (_input: unknown, options?: { onError?: (error: unknown) => void }) => options?.onError?.({
        status: 400,
        problem: {
          errors: [{ code: "InvalidContact", description: "At least one valid contact is required.", failureCode: "InvalidContact" }],
        },
      }),
    );
    renderSurface("platform", { platformView: "detail", userId: "platform-user" });

    await user.clear(screen.getByRole("textbox", { name: "Email address" }));
    const phone = screen.getByRole("textbox", { name: "Phone number" });
    await user.type(phone, "+982112345678");
    await user.click(screen.getByRole("button", { name: "Save user changes" }));

    expect(screen.getByRole("alert")).toHaveTextContent("Enter a valid email address or mobile number.");
    expect(screen.getByText("+98")).toBeInTheDocument();
    expect(phone).toHaveValue("2112345678");
  });

  it("explains a duplicate contact when the created account already exists", async () => {
    const user = userEvent.setup();
    platformPhoneCreate.mockImplementationOnce(
      (_input: unknown, options?: { onError?: (error: unknown) => void }) => options?.onError?.({
        status: 409,
        problem: {
          status: 409,
          errors: [{ code: "ContactIdentifierInUse", description: "Another account already uses this email address or phone number.", failureCode: "ContactIdentifierInUse" }],
        },
      }),
    );
    renderSurface("platform", { platformView: "create" });

    await user.selectOptions(screen.getByRole("combobox", { name: "Contact type" }), "phone");
    await user.type(screen.getByRole("textbox", { name: "Phone number" }), "09123456789");
    await user.click(screen.getByRole("button", { name: "Create user" }));

    expect(screen.getByRole("status")).toHaveTextContent(
      "Another account already uses this email address or phone number.",
    );
    expect(screen.getByRole("textbox", { name: "Phone number" })).toHaveValue("09123456789");
  });
});
