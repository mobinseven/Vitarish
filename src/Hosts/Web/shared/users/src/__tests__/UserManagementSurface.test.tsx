import { cleanup, render, screen } from "@testing-library/react";
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
  },
  globalRoles: ["PlatformAdmin"],
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
