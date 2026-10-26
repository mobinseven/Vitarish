"use client";

import { useEffect, useId, useMemo, useState, type ComponentProps, type FormEvent } from "react";
import { Alert, Button, Chip, Modal, Pagination, Surface, Table, Tooltip } from "@heroui/react";
import {
  useAuthSession,
  useAuthenticationUserInactivityPolicyGet,
  useAuthenticationUserInactivityPolicyPut,
  useAuthenticationUserSessionsRevoke,
  useUsersAddRole,
  usePlatformAuthorization,
  useTenantAuthorization,
  useUsersAdministrationCreateWithEmail,
  useUsersAdministrationCreateWithPhone,
  useUsersAdministrationGrantTenantRole,
  useUsersAdministrationGetUserSecurity,
  useUsersAdministrationListTenantRoles,
  useUsersAdministrationRevokeTenantRole,
  useUsersAdministrationUpdateUser,
  useUsersDisableUser,
  useUsersEnableUser,
  useUsersListTenantMembers,
  useUsersProvisionTenantMember,
  useUsersSearchUsers,
  useUsersRemoveTenantMember,
} from "@vitarish/auth";
import type { TenantRoleMembershipResponse, UserAuthenticationMethodsResponse, UserPasskeyResponse, UserSecurityDetailsResponse, UserSecurityEventResponse, UserSecurityHistoryResponse } from "@vitarish/auth";
import { tenantRoleSuffixes, UsersCommandErrorKind } from "@vitarish/auth/users";
import type { TenantRoleSuffix } from "@vitarish/auth/users";
import { formatDateTime, formatNumber, useLocalizedText } from "@vitarish/localization";
import { auditOutcomeColor } from "@vitarish/security";
import { localizePlatformAuditEvent, localizeSecurityAuditOutcome } from "@vitarish/security/localization";
import { HeaderIcon, MobileNumberField } from "@vitarish/ui";

export type UserManagementMode = "tenant" | "platform";
export type UserManagementPlatformView = "directory" | "create" | "detail";

export interface UserManagementSurfaceProps {
  mode: UserManagementMode;
  title?: string;
  description?: string;
  platformView?: UserManagementPlatformView;
  userId?: string;
  onOpenUserDetails?: (userId: string) => void;
}

const maxResults = 50;
function Icon({ name }: { name: string }) {
  return <i aria-hidden="true" className={`fa-light fa-${name}`} />;
}

export function UserManagementSurface({
  mode,
  title,
  description,
  platformView = "directory",
  userId,
  onOpenUserDetails,
}: UserManagementSurfaceProps) {
  const { t } = useLocalizedText();
  const { isAuthenticated, isLoading: sessionLoading } = useAuthSession();
  const tenantAuthorization = useTenantAuthorization("owner");
  const platformAuthorization = usePlatformAuthorization();
  const canUseSurface = mode === "tenant"
    ? tenantAuthorization.hasAccess
    : platformAuthorization.hasAccess;
  const accessLoading = mode === "tenant"
    ? tenantAuthorization.isLoading
    : platformAuthorization.isLoading;

  const tenantMembers = useUsersListTenantMembers(
    { MaxResults: maxResults },
    { query: { enabled: mode === "tenant" && isAuthenticated && canUseSurface } },
  );
  const provisionTenantMember = useUsersProvisionTenantMember();
  const removeTenantMember = useUsersRemoveTenantMember();

  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const platformUsers = useUsersSearchUsers(
    { Query: search || undefined, MaxResults: maxResults, IncludeTenantUsers: false },
    { query: { enabled: mode === "platform" && isAuthenticated && canUseSurface } },
  );
  const [selectedUserId, setSelectedUserId] = useState(userId ?? "");
  const platformMemberships = useUsersAdministrationListTenantRoles(selectedUserId);

  if (sessionLoading || accessLoading) {
    return <p aria-live="polite">{t("users.management.loadingSession")}</p>;
  }

  if (!isAuthenticated || !canUseSurface) {
    return <p role="alert" className="text-danger">{t("users.management.forbidden")}</p>;
  }

  if (mode === "tenant") {
    return (
      <TenantUserManagement
        title={title ?? t("users.management.title")}
        description={description ?? t("users.management.description")}
        members={tenantMembers.data ?? []}
        isLoading={tenantMembers.isLoading}
        isError={tenantMembers.isError}
        onRetry={() => tenantMembers.refetch()}
        provision={provisionTenantMember}
        remove={removeTenantMember}
        onRefresh={() => tenantMembers.refetch()}
        t={t}
      />
    );
  }

  if (platformView === "create") {
    return <PlatformCreateUser title={title ?? t("users.management.createUser")} t={t} />;
  }

  if (platformView === "detail") {
    return (
      <PlatformUserDetail
        title={title ?? t("users.management.platformDetail")}
        userId={userId ?? ""}
        memberships={platformMemberships}
        t={t}
      />
    );
  }

  return (
    <PlatformUserDirectory
      title={title ?? t("users.management.title")}
      description={description ?? t("users.management.description")}
      input={searchInput}
      onInputChange={setSearchInput}
      onSearch={() => { setSearch(searchInput.trim()); setSelectedUserId(""); }}
      users={platformUsers.data ?? []}
      selectedUserId={selectedUserId}
      onSelect={setSelectedUserId}
      onOpenUserDetails={onOpenUserDetails}
      memberships={platformMemberships}
      isLoading={platformUsers.isLoading}
      isError={platformUsers.isError}
      onRetry={() => platformUsers.refetch()}
      t={t}
    />
  );
}

type Translate = ReturnType<typeof useLocalizedText>["t"];
type MembershipQuery = {
  data?: TenantRoleMembershipResponse[];
  isLoading: boolean;
  isError: boolean;
  refetch: () => Promise<unknown>;
};

function renderSection(props: ComponentProps<"div">) {
  return <section {...props} />;
}

/*
 * Native form controls stay native (form submission and option semantics), but
 * take HeroUI's field tokens and button classes so they match the design system.
 */
const nativeFieldClassName =
  "mt-1 w-full rounded-(--field-radius) bg-(--field-background) px-3 py-2 text-(--field-foreground) shadow-field outline-none focus-visible:ring-2 focus-visible:ring-(--focus)";
const nativePrimaryButtonClassName = "button button--md button--primary";
const nativeLinkButtonClassName = "button button--sm button--ghost text-accent";
const nativeSecondaryButtonClassName = "button button--md button--secondary";
const nativeDangerButtonClassName = "button button--md button--danger-soft";

function SurfaceHeader({ title, description }: { title: string; description?: string }) {
  return (
    <header className="flex items-center gap-4">
      <HeaderIcon icon="users" level="page" />
      <div className="min-w-0">
        <h1 className="text-2xl font-semibold">{title}</h1>
        {description && <p className="mt-2 text-muted">{description}</p>}
      </div>
    </header>
  );
}

function TenantUserManagement({
  title,
  description,
  members,
  isLoading,
  isError,
  onRetry,
  provision,
  remove,
  onRefresh,
  t,
}: {
  title: string;
  description?: string;
  members: Array<{ user: { id: string; fullName?: string | null; userName?: string | null; email?: string | null; phoneNumber?: string | null }; roleSuffix: string }>;
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  provision: ReturnType<typeof useUsersProvisionTenantMember>;
  remove: ReturnType<typeof useUsersRemoveTenantMember>;
  onRefresh: () => Promise<unknown>;
  t: Translate;
}) {
  const [contactType, setContactType] = useState<"email" | "phone">("email");
  const [contact, setContact] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [message, setMessage] = useState("");
  const [contactFieldError, setContactFieldError] = useState<string | undefined>();

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const data = contactType === "email"
      ? { email: contact.trim(), firstName: firstName.trim() || undefined, lastName: lastName.trim() || undefined }
      : { phoneNumber: contact.trim(), firstName: firstName.trim() || undefined, lastName: lastName.trim() || undefined };
    provision.mutate({ data }, {
      onSuccess: async () => {
        setContact("");
        setContactFieldError(undefined);
        setFirstName("");
        setLastName("");
        setMessage(t("users.management.created"));
        await onRefresh();
      },
      onError: (error) => {
        if (isContactIdentifierInUse(error)) {
          setContactFieldError(undefined);
          setMessage(t("users.management.contactInUse"));
          return;
        }
        if (isInvalidContact(error)) {
          if (contactType === "phone") {
            setContactFieldError(t("users.management.contactRequired"));
            setMessage("");
          } else {
            setMessage(t("users.management.contactRequired"));
          }
          return;
        }
        setMessage(t("users.management.createError"));
      },
    });
  };

  const removeMember = (id: string) => {
    if (!window.confirm(t("users.management.removeConfirm"))) return;
    remove.mutate({ userId: id }, {
      onSuccess: async () => { setMessage(t("users.management.removed")); await onRefresh(); },
      onError: () => setMessage(t("users.management.removeError")),
    });
  };

  return (
    <main className="space-y-6">
      <SurfaceHeader title={title} description={description} />
      <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
        <div className="flex flex-col items-start gap-1">
          <div className="flex items-center gap-3">
            <HeaderIcon icon="user-plus" />
            <div className="min-w-0">
              <h2 className="text-lg font-semibold text-foreground">{t("users.management.addMember")}</h2>
            </div>
          </div>
        </div>
        <div>
        <form className="grid gap-3 md:grid-cols-2" onSubmit={submit}>
          <label>{t("users.management.contactType")}
            <select className={nativeFieldClassName} value={contactType} onChange={(event) => { setContactType(event.target.value as "email" | "phone"); setContactFieldError(undefined); }}>
              <option value="email">{t("users.management.emailContact")}</option>
              <option value="phone">{t("users.management.phoneContact")}</option>
            </select>
          </label>
          <div>
            {contactType === "email" ? (
              <label>{t("users.management.emailContact")}
                <input className={nativeFieldClassName} required value={contact} onChange={(event) => setContact(event.target.value)} />
              </label>
            ) : (
              <MobileNumberField
                label={t("users.management.phoneContact")}
                hint={t("users.management.phoneContactHint")}
                value={contact}
                onChange={(value) => { setContact(value); setContactFieldError(undefined); }}
                isRequired
                error={contactFieldError}
              />
            )}
          </div>
          <label>{t("users.management.firstName")} <span className="text-muted">({t("users.management.optional")})</span>
            <input className={nativeFieldClassName} value={firstName} onChange={(event) => setFirstName(event.target.value)} />
          </label>
          <label>{t("users.management.lastName")} <span className="text-muted">({t("users.management.optional")})</span>
            <input className={nativeFieldClassName} value={lastName} onChange={(event) => setLastName(event.target.value)} />
          </label>
          <button className={`${nativePrimaryButtonClassName} w-fit`} disabled={provision.isPending} title={t("users.management.addMember")}>
            <Icon name="user-plus" /> {provision.isPending ? t("users.management.adding") : t("users.management.addMember")}
          </button>
        </form>
        </div>
      </Surface>
      {message && <p role="status" aria-live="polite">{message}</p>}
      <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
        <div className="flex items-center gap-3">
          <HeaderIcon icon="id-badge" />
          <h2 className="text-lg font-semibold text-foreground">{t("users.management.memberships")}</h2>
        </div>
        <div>
      {isLoading && <p aria-live="polite">{t("users.management.loading")}</p>}
      {isError && <div role="alert"><p className="text-danger">{t("users.management.error")}</p><button className={nativeLinkButtonClassName} onClick={onRetry} title={t("users.management.retry")}><Icon name="arrow-rotate-right" />{t("users.management.retry")}</button></div>}
      {!isLoading && !isError && members.length === 0 && <p className="text-muted">{t("users.management.empty")}</p>}
      {!isLoading && !isError && members.length > 0 && <ul className="space-y-3" aria-label={t("users.management.memberships")}>
        {members.map((member) => {
          const isOwner = member.roleSuffix === tenantRoleSuffixes.owner;
          const displayName = member.user.fullName ?? member.user.userName ?? member.user.email ?? member.user.phoneNumber ?? member.user.id;
          return <li className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-surface-secondary p-4" key={member.user.id}>
            <div><p className="font-medium">{displayName}</p><p className="text-sm text-muted">{isOwner ? t("users.management.owner") : t("users.management.staff")}{member.user.email ? ` · ${member.user.email}` : ""}{member.user.phoneNumber ? <> · <span dir="ltr">{member.user.phoneNumber}</span></> : ""}</p></div>
            {!isOwner && <button className={nativeDangerButtonClassName} disabled={remove.isPending} onClick={() => removeMember(member.user.id)} title={t("users.management.remove")}><Icon name="user-minus" />{t("users.management.remove")}</button>}
          </li>;
        })}
      </ul>}
        </div>
      </Surface>
    </main>
  );
}

function PlatformUserDirectory({
  title, description, input, onInputChange, onSearch, users, selectedUserId, onSelect, onOpenUserDetails, memberships, isLoading, isError, onRetry, t,
}: {
  title: string; description?: string; input: string; onInputChange: (value: string) => void; onSearch: () => void;
  users: Array<{ id: string; fullName?: string | null; userName?: string | null; email?: string | null; phoneNumber?: string | null }>;
  selectedUserId: string; onSelect: (id: string) => void; onOpenUserDetails?: (id: string) => void; memberships: MembershipQuery; isLoading: boolean; isError: boolean; onRetry: () => void; t: Translate;
}) {
  return <main className="space-y-6"><SurfaceHeader title={title} description={description} />
    <Surface className="flex flex-col gap-6 rounded-3xl p-6"><div className="space-y-4">
    <form className="flex gap-3" onSubmit={(event) => { event.preventDefault(); onSearch(); }}>
      <label className="sr-only" htmlFor="user-management-search">{t("users.management.searchLabel")}</label>
      <input id="user-management-search" className={`${nativeFieldClassName} mt-0 min-w-0 flex-1`} value={input} onChange={(event) => onInputChange(event.target.value)} placeholder={t("users.management.searchPlaceholder")} />
      <button className={nativePrimaryButtonClassName} title={t("users.management.search")}><Icon name="magnifying-glass" />{t("users.management.search")}</button>
    </form>
    {isLoading && <p aria-live="polite">{t("users.management.loading")}</p>}
    {isError && <div role="alert"><p className="text-danger">{t("users.management.error")}</p><button className={nativeLinkButtonClassName} onClick={onRetry} title={t("users.management.retry")}><Icon name="arrow-rotate-right" />{t("users.management.retry")}</button></div>}
    {!isLoading && !isError && users.length === 0 && <p className="text-muted">{t("users.management.empty")}</p>}
    </div></Surface>
    {users.length > 0 && <div className="grid gap-6 lg:grid-cols-[1fr_1fr]"><Surface className="flex flex-col gap-6 rounded-3xl p-6"><div className="overflow-x-auto"><table className="w-full text-left"><thead><tr><th>{t("users.management.name")}</th><th>{t("users.management.email")}</th><th>{t("users.management.phone")}</th><th><span className="sr-only">{t("users.management.userDetails")}</span></th></tr></thead><tbody>{users.map((user) => <tr className="border-t" key={user.id} onClick={() => onSelect(user.id)}><td className="py-3">{user.fullName ?? user.userName ?? user.id}</td><td>{user.email ?? "—"}</td><td><span dir="ltr">{user.phoneNumber ?? "—"}</span></td><td><button className={nativeSecondaryButtonClassName} type="button" onClick={(event) => { event.stopPropagation(); onOpenUserDetails?.(user.id); }} title={t("users.management.userDetails")} aria-label={t("users.management.userDetails")}><Icon name="user-gear" /></button></td></tr>)}</tbody></table></div></Surface><MembershipList userId={selectedUserId} memberships={memberships} t={t} /></div>}
  </main>;
}

function MembershipList({ userId, memberships, t }: { userId: string; memberships: MembershipQuery; t: Translate }) {
  return <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}><div className="flex items-center gap-3"><HeaderIcon icon="id-badge" /><h2 className="text-lg font-semibold text-foreground">{t("users.management.memberships")}</h2></div><div>{!userId && <p className="mt-2 text-muted">{t("users.management.selectUser")}</p>}{userId && memberships.isLoading && <p className="mt-2">{t("users.management.membershipLoading")}</p>}{userId && memberships.isError && <p className="mt-2 text-danger">{t("users.management.membershipError")}</p>}{userId && !memberships.isLoading && !memberships.isError && !memberships.data?.length && <p className="mt-2 text-muted">{t("users.management.noMemberships")}</p>}{!!memberships.data?.length && <ul className="mt-3 space-y-2">{memberships.data.map((membership) => <li className="rounded-2xl bg-surface-secondary p-3" key={`${membership.tenantId}:${membership.roleSuffix}`}><div>{t("users.management.tenant")}: {membership.tenantId}</div><div>{t("users.management.role")}: {membership.roleSuffix}</div></li>)}</ul>}</div></Surface>;
}

function PlatformCreateUser({ title, t }: { title: string; t: Translate }) {
  const [contactType, setContactType] = useState<"email" | "phone">("email");
  const [contact, setContact] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [message, setMessage] = useState("");
  const [contactFieldError, setContactFieldError] = useState<string | undefined>();
  const email = useUsersAdministrationCreateWithEmail();
  const phone = useUsersAdministrationCreateWithPhone();
  const pending = email.isPending || phone.isPending;
  const submit = (event: FormEvent) => {
    event.preventDefault();
    const onSuccess = () => { setMessage(t("users.management.createdPlatform")); setContact(""); setContactFieldError(undefined); };
    const onError = (error: unknown) => {
      if (isContactIdentifierInUse(error)) {
        setContactFieldError(undefined);
        setMessage(t("users.management.contactInUse"));
        return;
      }
      if (isInvalidContact(error)) {
        if (contactType === "phone") {
          setContactFieldError(t("users.management.contactRequired"));
          setMessage("");
        } else {
          setMessage(t("users.management.contactRequired"));
        }
        return;
      }
      setMessage(t("users.management.createError"));
    };
    if (contactType === "email") email.mutate({ data: { email: contact.trim(), firstName: firstName.trim(), lastName: lastName.trim() } }, { onSuccess, onError });
    else phone.mutate({ data: { phoneNumber: contact.trim(), firstName: firstName.trim(), lastName: lastName.trim() } }, { onSuccess, onError });
  };
  return <main className="max-w-2xl space-y-5"><SurfaceHeader title={title} /><Surface className="flex flex-col gap-6 rounded-3xl p-6"><div><form className="space-y-4" onSubmit={submit}><label className="block">{t("users.management.contactType")}<select className={nativeFieldClassName} value={contactType} onChange={(event) => { setContactType(event.target.value as "email" | "phone"); setContactFieldError(undefined); }}><option value="email">{t("users.management.emailContact")}</option><option value="phone">{t("users.management.phoneContact")}</option></select></label><div>{contactType === "email" ? <label className="block">{t("users.management.emailContact")}<input className={nativeFieldClassName} required value={contact} onChange={(event) => setContact(event.target.value)} /></label> : <MobileNumberField label={t("users.management.phoneContact")} hint={t("users.management.phoneContactHint")} value={contact} onChange={(value) => { setContact(value); setContactFieldError(undefined); }} isRequired error={contactFieldError} />}</div><label className="block">{t("users.management.firstName")}<input className={nativeFieldClassName} value={firstName} onChange={(event) => setFirstName(event.target.value)} /></label><label className="block">{t("users.management.lastName")}<input className={nativeFieldClassName} value={lastName} onChange={(event) => setLastName(event.target.value)} /></label><button className={nativePrimaryButtonClassName} disabled={pending} title={t("users.management.createUser")}><Icon name="user-plus" />{pending ? t("users.management.adding") : t("users.management.createUser")}</button></form></div></Surface>{message && <p role="status">{message}</p>}</main>;
}

function PlatformUserDetail({ title, userId, memberships, t }: { title: string; userId: string; memberships: MembershipQuery; t: Translate }) {
  const { locale } = useLocalizedText();
  const grant = useUsersAdministrationGrantTenantRole();
  const grantGlobalRole = useUsersAddRole();
  const revoke = useUsersAdministrationRevokeTenantRole();
  const update = useUsersAdministrationUpdateUser();
  const inactivityPolicy = useAuthenticationUserInactivityPolicyGet(userId, { query: { enabled: userId.length > 0 } });
  const saveInactivityPolicy = useAuthenticationUserInactivityPolicyPut();
  const revokeUserSessions = useAuthenticationUserSessionsRevoke();
  const disableUser = useUsersDisableUser();
  const enableUser = useUsersEnableUser();
  const [confirmEndSessions, setConfirmEndSessions] = useState(false);
  const [confirmAccountStatus, setConfirmAccountStatus] = useState<"disable" | "enable" | null>(null);
  const [securityPage, setSecurityPage] = useState(1);
  const [authenticationPage, setAuthenticationPage] = useState(1);
  const [tenantId, setTenantId] = useState("");
  const [roleSuffix, setRoleSuffix] = useState<TenantRoleSuffix>(tenantRoleSuffixes.user);
  const [message, setMessage] = useState("");
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [phoneFieldError, setPhoneFieldError] = useState<string | undefined>();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [globalRole, setGlobalRole] = useState("");
  const [inactivityTimeout, setInactivityTimeout] = useState("");
  const [inactivityConflict, setInactivityConflict] = useState(false);
  const [revision, setRevision] = useState<string | null>(null);
  const pageSize = 25;
  const securityQuery = useUsersAdministrationGetUserSecurity(
    userId,
    { page: securityPage, authenticationPage, pageSize },
    { query: { enabled: userId.length > 0 } },
  );
  const partialSecurityDetails = readUnavailableSecurityDetails(securityQuery.error, userId);
  const securityDetails = securityQuery.data ?? partialSecurityDetails;
  const tenantRoles = memberships.data ?? securityDetails?.tenantRoles ?? [];
  const securityHistoryAvailable = securityDetails?.securityHistoryAvailable ?? false;
  const authenticationHistoryAvailable = securityDetails?.authenticationHistoryAvailable ?? false;
  const historyUnavailable = !securityHistoryAvailable || !authenticationHistoryAvailable || securityQuery.isError;
  const securityHistory = securityDetails?.securityHistory ?? undefined;
  const authenticationHistory = securityDetails?.authenticationHistory ?? undefined;
  // History page changes do not reload the methods, so keep the ones from the first details load.
  const [authenticationMethods, setAuthenticationMethods] = useState<UserAuthenticationMethodsResponse>();
  useEffect(() => setAuthenticationMethods(undefined), [userId]);
  useEffect(() => {
    if (securityDetails?.authenticationMethods) setAuthenticationMethods(securityDetails.authenticationMethods);
  }, [securityDetails?.authenticationMethods]);
  const authenticationMethodsUnavailable = securityDetails?.authenticationMethodsUnavailable ?? false;

  useEffect(() => {
    setSecurityPage(1);
    setAuthenticationPage(1);
    setGlobalRole("");
    setInactivityConflict(false);
  }, [userId]);

  useEffect(() => {
    if (inactivityPolicy.data) setInactivityTimeout(inactivityPolicy.data.inactivityTimeoutMinutes?.toString() ?? "");
  }, [inactivityPolicy.data?.version, userId]);

  useEffect(() => {
    const user = securityDetails?.user;
    if (!user) return;
    setEmail(user.email ?? "");
    setPhoneNumber(user.phoneNumber ?? "");
    setPhoneFieldError(undefined);
    setFirstName(user.firstName ?? "");
    setLastName(user.lastName ?? "");
    setRevision(user.revision ?? null);
  }, [securityDetails?.user.id, securityDetails?.user.email, securityDetails?.user.phoneNumber, securityDetails?.user.firstName, securityDetails?.user.lastName, securityDetails?.user.revision]);

  const refreshSecurityDetails = async () => {
    await Promise.all([securityQuery.refetch(), memberships.refetch()]);
  };

  const submitProfile = (event: FormEvent) => {
    event.preventDefault();
    const normalizedEmail = email.trim();
    const normalizedPhoneNumber = phoneNumber.trim();
    if (!normalizedEmail && !normalizedPhoneNumber) {
      setMessage(t("users.management.contactRequired"));
      return;
    }

    update.mutate({
      userId,
      data: {
        email: normalizedEmail || null,
        phoneNumber: normalizedPhoneNumber || null,
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        revision,
      },
    }, {
      onSuccess: async () => {
        setMessage(t("users.management.changesSaved"));
        setPhoneFieldError(undefined);
        await refreshSecurityDetails();
      },
      onError: async (error) => {
        if (isContactIdentifierInUse(error)) {
          setMessage(t("users.management.contactInUse"));
          return;
        }
        if (isInvalidContact(error)) {
          if (normalizedPhoneNumber) {
            setPhoneFieldError(t("users.management.contactRequired"));
            setMessage("");
          } else {
            setMessage(t("users.management.contactRequired"));
          }
          return;
        }
        if (readHttpStatus(error) === 409) {
          setMessage(t("users.management.editConflict"));
          await refreshSecurityDetails();
          return;
        }
        setMessage(t("users.management.editError"));
      },
    });
  };

  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (!tenantId.trim()) return;
    grant.mutate({ userId, tenantId: tenantId.trim(), roleSuffix, data: { roleSuffix } }, { onSuccess: () => { setMessage(t("users.management.roleGranted")); refreshSecurityDetails(); }, onError: () => setMessage(t("users.management.roleError")) });
  };
  const revokeRole = (tenant: string, role: string) => {
    if (!window.confirm(t("users.management.revokeConfirm"))) return;
    revoke.mutate({ userId, tenantId: tenant, roleSuffix: role, data: { roleSuffix: role } }, { onSuccess: () => { setMessage(t("users.management.roleRevoked")); refreshSecurityDetails(); }, onError: () => setMessage(t("users.management.roleError")) });
  };
  const submitGlobalRole = (event: FormEvent) => {
    event.preventDefault();
    if (!globalRole || !securityDetails?.permittedGlobalRoles.includes(globalRole)) return;
    grantGlobalRole.mutate({ userId, data: { roleName: globalRole } }, {
      onSuccess: async () => { setMessage(t("users.management.globalRoleGranted")); setGlobalRole(""); await refreshSecurityDetails(); },
      onError: () => setMessage(t("users.management.roleError")),
    });
  };
  const submitInactivityPolicy = (event: FormEvent) => {
    event.preventDefault();
    if (!inactivityPolicy.data) return;
    const parsed = inactivityTimeout.trim() === "" ? null : Number(inactivityTimeout);
    if (parsed !== null && !Number.isInteger(parsed)) { setMessage(t("users.management.inactivityValidationError")); return; }
    saveInactivityPolicy.mutate({ userId, data: { inactivityTimeoutMinutes: parsed, expectedVersion: inactivityPolicy.data.version } }, {
      onSuccess: async () => { setMessage(t("users.management.inactivitySaved")); setInactivityConflict(false); await inactivityPolicy.refetch(); },
      onError: (error) => { const conflict = readHttpStatus(error) === 409; setInactivityConflict(conflict); setMessage(conflict ? t("users.management.inactivityConflict") : t("users.management.inactivityError")); },
    });
  };
  const endUserSessions = () => {
    revokeUserSessions.mutate({ userId }, {
      onSuccess: async (result) => {
        setConfirmEndSessions(false);
        setMessage(t("users.management.sessionsEnded", { count: formatNumber(Number(result.endedSessions), locale) }));
        await refreshSecurityDetails();
      },
      onError: (error) => {
        setConfirmEndSessions(false);
        setMessage(readHttpStatus(error) === 403
          ? t("users.management.sessionsForbidden")
          : t("users.management.sessionsError"));
      },
    });
  };
  const changeAccountStatus = (enabled: boolean) => {
    const mutation = enabled ? enableUser : disableUser;
    mutation.mutate({ userId }, {
      onSuccess: async () => {
        setConfirmAccountStatus(null);
        setMessage(enabled ? t("users.management.accountEnabled") : t("users.management.accountDisabled"));
        await refreshSecurityDetails();
      },
      onError: async (error) => {
        setConfirmAccountStatus(null);
        const status = readHttpStatus(error);
        if (!enabled && hasUsersCommandFailure(error, UsersCommandErrorKind.ActiveSessionConflict)) {
          setMessage(t("users.management.accountDisableConflict"));
          await refreshSecurityDetails();
          return;
        }
        if (status === 409) {
          setMessage(t("users.management.editConflict"));
          await refreshSecurityDetails();
          return;
        }
        setMessage(status === 403
          ? t("users.management.accountStatusForbidden")
          : t("users.management.accountStatusError"));
      },
    });
  };
  if (securityQuery.isLoading && !securityDetails) {
    return <main className="max-w-4xl space-y-6"><SurfaceHeader title={title} description={userId} /><p aria-live="polite">{t("users.management.loadingSecurity")}</p></main>;
  }

  if (!securityDetails) {
    return <main className="max-w-4xl space-y-6"><SurfaceHeader title={title} description={userId} /><div role="alert"><p className="text-danger">{t("users.management.securityDetailsError")}</p><button className={nativeLinkButtonClassName} onClick={() => securityQuery.refetch()} title={t("users.management.retry")}><Icon name="arrow-rotate-right" />{t("users.management.retry")}</button></div></main>;
  }

  return <main className="max-w-5xl space-y-6">
    <SurfaceHeader title={title} description={securityDetails.user.fullName ?? securityDetails.user.userName ?? undefined} />
    {historyUnavailable && <Alert status="warning" role="status"><Alert.Indicator /><Alert.Content><Alert.Title>{t("users.management.historyUnavailable")}</Alert.Title></Alert.Content></Alert>}
    {securityQuery.isError && <button className={nativeLinkButtonClassName} onClick={() => securityQuery.refetch()} title={t("users.management.retry")}><Icon name="arrow-rotate-right" />{t("users.management.retry")}</button>}

    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="user" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("users.management.profile")}</h2>
          </div>
        </div>
      </div>
      <div className="space-y-3">
      <dl className="grid gap-3 sm:grid-cols-2">
        <div><dt className="text-sm text-muted">{t("users.management.userIdentifier")}</dt><dd className="break-all font-mono">{securityDetails.user.id}</dd></div>
        <div><dt className="text-sm text-muted">{t("users.management.name")}</dt><dd>{securityDetails.user.fullName ?? securityDetails.user.userName ?? t("users.management.notAvailable")}</dd></div>
        <div><dt className="text-sm text-muted">{t("users.management.accountStatusTitle")}</dt><dd>{securityDetails.user.isEnabled ? t("users.management.accountStatusEnabled") : t("users.management.accountStatusDisabled")}</dd></div>
      </dl>
      <form className="grid gap-3 md:grid-cols-2" onSubmit={submitProfile}>
        <label>{t("users.management.emailContact")}
          <input className={nativeFieldClassName} type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} />
        </label>
        <MobileNumberField
          label={t("users.management.phoneContact")}
          hint={t("users.management.phoneContactHint")}
          value={phoneNumber}
          onChange={(value) => {
            setPhoneNumber(value);
            setPhoneFieldError(undefined);
          }}
          error={phoneFieldError}
        />
        <label>{t("users.management.firstName")}
          <input className={nativeFieldClassName} autoComplete="given-name" value={firstName} onChange={(event) => setFirstName(event.target.value)} />
        </label>
        <label>{t("users.management.lastName")}
          <input className={nativeFieldClassName} autoComplete="family-name" value={lastName} onChange={(event) => setLastName(event.target.value)} />
        </label>
        <button className={`${nativePrimaryButtonClassName} w-fit`} disabled={update.isPending} title={t("users.management.saveChanges")}>
          <Icon name="floppy-disk" />{update.isPending ? t("users.management.savingChanges") : t("users.management.saveChanges")}
        </button>
      </form>
      </div>
    </Surface>

    {(authenticationMethods || authenticationMethodsUnavailable) && <AuthenticationMethodsSection methods={authenticationMethodsUnavailable ? undefined : authenticationMethods} locale={locale} t={t} />}

    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="user-shield" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("users.management.globalRoles")}</h2>
          </div>
        </div>
      </div>
      <div className="space-y-3">
      {securityDetails.globalRoles.length === 0
        ? <p className="text-muted">{t("users.management.noRoles")}</p>
        : <ul className="flex flex-wrap gap-2">{securityDetails.globalRoles.map((role) => <li className="rounded-xl bg-surface-secondary px-3 py-1" key={role}>{role}</li>)}</ul>}
      <form className="flex flex-wrap items-end gap-3 border-t pt-4" onSubmit={submitGlobalRole}>
        <label className="min-w-56 flex-1">{t("users.management.globalRoleToGrant")}<select className={nativeFieldClassName} value={globalRole} onChange={(event) => setGlobalRole(event.target.value)}><option value="">{t("users.management.selectGlobalRole")}</option>{securityDetails.permittedGlobalRoles.filter((role) => !securityDetails.globalRoles.includes(role)).map((role) => <option key={role} value={role}>{role}</option>)}</select></label>
        <button className={nativeSecondaryButtonClassName} disabled={!globalRole || grantGlobalRole.isPending} title={t("users.management.grantGlobalRole")}><Icon name="user-check" />{t("users.management.grantGlobalRole")}</button>
      </form>
      <p className="text-sm text-muted">{t("users.management.globalRoleRevokeGap")}</p>
      <h3 className="pt-2 font-medium">{t("users.management.memberships")}</h3>
      {memberships.isLoading && <p aria-live="polite">{t("users.management.membershipLoading")}</p>}
      {memberships.isError && <p className="text-danger">{t("users.management.membershipError")}</p>}
      {!memberships.isLoading && !memberships.isError && tenantRoles.length === 0 && <p className="text-muted">{t("users.management.noMemberships")}</p>}
      {tenantRoles.map((membership) => <div className="mt-2 flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-surface-secondary p-3" key={`${membership.tenantId}:${membership.roleSuffix}`}>
        <span>{t("users.management.tenant")}: {membership.tenantId} · {t("users.management.role")}: {membership.roleSuffix}</span>
        <button className="inline-flex items-center gap-2 text-danger" disabled={revoke.isPending || memberships.isError} onClick={() => revokeRole(membership.tenantId, membership.roleSuffix)} title={t("users.management.revoke")}><Icon name="user-minus" />{t("users.management.revoke")}</button>
      </div>)}
      <form className="space-y-3 border-t pt-4" onSubmit={submit}>
        <h3 className="font-medium">{t("users.management.grant")}</h3>
        <label className="block">{t("users.management.tenantId")}<input className={nativeFieldClassName} required value={tenantId} onChange={(event) => setTenantId(event.target.value)} /></label>
        <label className="block">{t("users.management.roleToGrant")}<select className={`${nativeFieldClassName} w-auto`} value={roleSuffix} onChange={(event) => setRoleSuffix(event.target.value === tenantRoleSuffixes.owner ? tenantRoleSuffixes.owner : tenantRoleSuffixes.user)}><option value={tenantRoleSuffixes.user}>{tenantRoleSuffixes.user}</option><option value={tenantRoleSuffixes.owner}>{tenantRoleSuffixes.owner}</option></select></label>
        <button className={nativePrimaryButtonClassName} disabled={grant.isPending || !tenantId.trim()} title={t("users.management.grant")}><Icon name="user-check" />{t("users.management.grant")}</button>
      </form>
      </div>
    </Surface>

    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="hourglass-half" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("users.management.inactivityPolicyTitle")}</h2>
          </div>
        </div>
      </div>
      <div className="space-y-3">
      {inactivityPolicy.isLoading && <p aria-live="polite">{t("users.management.inactivityLoading")}</p>}
      {inactivityPolicy.isError && <div role="alert"><p className="text-danger">{t("users.management.inactivityError")}</p><button className={nativeLinkButtonClassName} onClick={() => inactivityPolicy.refetch()} title={t("users.management.retry")}><Icon name="arrow-rotate-right" />{t("users.management.retry")}</button></div>}
      {inactivityPolicy.data && <form className="space-y-3" onSubmit={submitInactivityPolicy}>
        <p className="text-sm text-muted">{t("users.management.inactivityDescription")}</p>
        <label className="block">{t("users.management.inactivityTimeout")}<input className={nativeFieldClassName} type="number" min={1} value={inactivityTimeout} onChange={(event) => setInactivityTimeout(event.target.value)} /></label>
        <p className="text-sm text-muted">{t("users.management.inactivityInheritHint")}</p>
        <button className={nativeSecondaryButtonClassName} disabled={saveInactivityPolicy.isPending} title={t("users.management.saveInactivity")}><Icon name="floppy-disk" />{t("users.management.saveInactivity")}</button>
        {inactivityConflict && <button className={nativeLinkButtonClassName} onClick={() => inactivityPolicy.refetch()} title={t("users.management.reloadInactivity")}><Icon name="arrow-rotate-right" />{t("users.management.reloadInactivity")}</button>}
      </form>}
      </div>
    </Surface>

    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="toggle-on" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("users.management.accountStatusTitle")}</h2>
          </div>
        </div>
      </div>
      <div className="space-y-3">
      <p className="text-sm text-muted">{t("users.management.accountStatusDescription")}</p>
      <p className="font-medium">{securityDetails.user.isEnabled ? t("users.management.accountStatusEnabled") : t("users.management.accountStatusDisabled")}</p>
      <Tooltip>
        <Button
          variant={securityDetails.user.isEnabled ? "danger" : "secondary"}
          isPending={disableUser.isPending || enableUser.isPending}
          onPress={() => setConfirmAccountStatus(securityDetails.user.isEnabled ? "disable" : "enable")}
        >
          <Icon name={securityDetails.user.isEnabled ? "user-slash" : "user-check"} /> {securityDetails.user.isEnabled ? t("users.management.disableAccount") : t("users.management.enableAccount")}
        </Button>
        <Tooltip.Content>{securityDetails.user.isEnabled ? t("users.management.disableAccount") : t("users.management.enableAccount")}</Tooltip.Content>
      </Tooltip>
      </div>
    </Surface>

    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="laptop" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("users.management.sessionsTitle")}</h2>
          </div>
        </div>
      </div>
      <div className="space-y-3">
      <p className="text-sm text-muted">{t("users.management.sessionsDescription")}</p>
      <Tooltip>
        <Button variant="danger" isPending={revokeUserSessions.isPending} onPress={() => setConfirmEndSessions(true)}>
          <Icon name="right-from-bracket" /> {t("users.management.endSessions")}
        </Button>
        <Tooltip.Content>{t("users.management.endSessions")}</Tooltip.Content>
      </Tooltip>
      </div>
    </Surface>
    <EndUserSessionsDialog isOpen={confirmEndSessions} isPending={revokeUserSessions.isPending} onCancel={() => setConfirmEndSessions(false)} onConfirm={endUserSessions} t={t} />
    <AccountStatusDialog action={confirmAccountStatus} isPending={disableUser.isPending || enableUser.isPending} onCancel={() => setConfirmAccountStatus(null)} onConfirm={() => changeAccountStatus(confirmAccountStatus === "enable")} t={t} />

    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="right-to-bracket" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("users.management.authenticationHistory")}</h2>
          </div>
        </div>
      </div>
      <div className="space-y-4">
      <UserSecurityHistoryTable history={authenticationHistory} available={authenticationHistoryAvailable} emptyMessage={t("users.management.authenticationHistoryEmpty")} label={t("users.management.authenticationHistory")} page={authenticationPage} onPageChange={setAuthenticationPage} isFetching={securityQuery.isFetching} t={t} />
      </div>
    </Surface>
    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="shield-halved" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("users.management.securityHistory")}</h2>
          </div>
        </div>
      </div>
      <div className="space-y-4">
      <UserSecurityHistoryTable history={securityHistory} available={securityHistoryAvailable} emptyMessage={t("users.management.securityHistoryEmpty")} label={t("users.management.securityHistory")} page={securityPage} onPageChange={setSecurityPage} isFetching={securityQuery.isFetching} t={t} />
      </div>
    </Surface>

    {message && <p role="status" aria-live="polite">{message}</p>}
  </main>;
}

function AuthenticationMethodsSection({ methods, locale, t }: { methods: UserAuthenticationMethodsResponse | undefined; locale: ReturnType<typeof useLocalizedText>["locale"]; t: Translate }) {
  const passkeysHeadingId = useId();
  return <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection}>
    <div className="flex flex-col items-start gap-1">
      <div className="flex items-center gap-3">
        <HeaderIcon icon="key" />
        <div className="min-w-0">
          <h2 className="text-lg font-semibold text-foreground">{t("users.management.authenticationMethodsTitle")}</h2>
          <p className="text-sm text-muted">{t("users.management.authenticationMethodsDescription")}</p>
        </div>
      </div>
    </div>
    {!methods
      ? <Alert status="warning" role="status"><Alert.Indicator /><Alert.Content><Alert.Title>{t("users.management.authenticationMethodsUnavailable")}</Alert.Title></Alert.Content></Alert>
      : <>
    <dl className="grid gap-3 sm:grid-cols-3">
      <div><dt className="text-sm text-muted">{t("users.management.password")}</dt><dd>{methods.passwordSet ? t("users.management.passwordSet") : t("users.management.passwordNotSet")}</dd></div>
      <div><dt className="text-sm text-muted">{t("users.management.emailVerification")}</dt><dd>{methods.emailVerified ? t("users.management.verified") : t("users.management.notVerified")}</dd></div>
      <div><dt className="text-sm text-muted">{t("users.management.phoneVerification")}</dt><dd>{methods.phoneNumberVerified ? t("users.management.verified") : t("users.management.notVerified")}</dd></div>
    </dl>
    <div className="space-y-2">
      <h3 className="font-medium" id={methods.passkeys.length === 0 ? undefined : passkeysHeadingId}>{t("users.management.passkeys")}</h3>
      {methods.passkeys.length === 0
        ? <p className="text-muted">{t("users.management.noPasskeys")}</p>
        : <ul aria-labelledby={passkeysHeadingId} className="space-y-2">{methods.passkeys.map((passkey) => <li className="rounded-2xl bg-surface-secondary p-3" key={passkey.id}>
            <div className="font-medium">{passkey.friendlyName || t("users.management.unnamedPasskey")}</div>
            <div className="text-sm text-muted">{t("users.management.passkeyRegistered", { date: formatDateTime(passkey.registeredAt, locale, { timeZone: "UTC" }) })}</div>
            <div className="text-sm text-muted">{passkey.lastUsedAt ? t("users.management.passkeyLastUsed", { date: formatDateTime(passkey.lastUsedAt, locale, { timeZone: "UTC" }) }) : t("users.management.passkeyNeverUsed")}</div>
          </li>)}</ul>}
    </div>
    </>}
  </Surface>;
}

function EndUserSessionsDialog({ isOpen, isPending, onCancel, onConfirm, t }: {
  isOpen: boolean;
  isPending: boolean;
  onCancel: () => void;
  onConfirm: () => void;
  t: Translate;
}) {
  const state = useMemo(
    () => ({
      isOpen,
      setOpen: (open: boolean) => {
        if (!open && !isPending) onCancel();
      },
      open: () => undefined,
      close: () => {
        if (!isPending) onCancel();
      },
      toggle: () => {
        if (isOpen && !isPending) onCancel();
      },
    }),
    [isOpen, isPending, onCancel],
  );
  if (!isOpen) return null;
  return (
    <Modal state={state}>
      <Modal.Backdrop isDismissable={!isPending}>
        <Modal.Container placement="center">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading>{t("users.management.endSessionsDialogTitle")}</Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p>{t("users.management.endSessionsConfirm")}</p>
            </Modal.Body>
            <Modal.Footer>
              <Tooltip>
                <Button variant="secondary" onPress={onCancel} isDisabled={isPending}>
                  <Icon name="xmark" /> {t("users.management.cancel")}
                </Button>
                <Tooltip.Content>{t("users.management.cancel")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button variant="danger" onPress={onConfirm} isPending={isPending}>
                  <Icon name="right-from-bracket" /> {t("users.management.endSessionsConfirmAction")}
                </Button>
                <Tooltip.Content>{t("users.management.endSessionsConfirmAction")}</Tooltip.Content>
              </Tooltip>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

function AccountStatusDialog({ action, isPending, onCancel, onConfirm, t }: {
  action: "disable" | "enable" | null;
  isPending: boolean;
  onCancel: () => void;
  onConfirm: () => void;
  t: Translate;
}) {
  const isOpen = action !== null;
  const state = useMemo(
    () => ({
      isOpen,
      setOpen: (open: boolean) => {
        if (!open && !isPending) onCancel();
      },
      open: () => undefined,
      close: () => {
        if (!isPending) onCancel();
      },
      toggle: () => {
        if (isOpen && !isPending) onCancel();
      },
    }),
    [isOpen, isPending, onCancel],
  );
  if (!isOpen) return null;
  const isEnable = action === "enable";
  const title = isEnable ? t("users.management.enableAccountDialogTitle") : t("users.management.disableAccountDialogTitle");
  const confirmAction = isEnable ? t("users.management.enableAccountConfirmAction") : t("users.management.disableAccountConfirmAction");
  return (
    <Modal state={state}>
      <Modal.Backdrop isDismissable={!isPending}>
        <Modal.Container placement="center">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading>{title}</Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p>{isEnable ? t("users.management.enableAccountConfirm") : t("users.management.disableAccountConfirm")}</p>
            </Modal.Body>
            <Modal.Footer>
              <Tooltip>
                <Button variant="secondary" onPress={onCancel} isDisabled={isPending}>
                  <Icon name="xmark" /> {t("users.management.cancel")}
                </Button>
                <Tooltip.Content>{t("users.management.cancel")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button variant={isEnable ? "primary" : "danger"} onPress={onConfirm} isPending={isPending}>
                  <Icon name={isEnable ? "user-check" : "user-slash"} /> {confirmAction}
                </Button>
                <Tooltip.Content>{confirmAction}</Tooltip.Content>
              </Tooltip>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

/** A machine identifier shown left-to-right in monospace; the localized fallback keeps the page's own script. */
function HistoryIdentifier({ value, fallback }: { value: string | null | undefined; fallback: string }) {
  return value
    ? <span className="whitespace-nowrap font-mono text-sm" dir="ltr">{value}</span>
    : <span className="whitespace-nowrap text-muted">{fallback}</span>;
}

/** One server-paged history as a table, with its own pager so each history pages independently. */
function UserSecurityHistoryTable({ history, available, emptyMessage, label, page, onPageChange, isFetching, t }: {
  history: UserSecurityHistoryResponse | undefined;
  available: boolean;
  emptyMessage: string;
  label: string;
  page: number;
  onPageChange: (page: number) => void;
  isFetching: boolean;
  t: Translate;
}) {
  const { locale, direction } = useLocalizedText();
  if (!history) {
    return available
      ? <p className="text-muted">{emptyMessage}</p>
      : null;
  }

  const totalPages = Math.max(1, Math.ceil(Number(history.totalCount) / Math.max(1, Number(history.pageSize))));
  const pager = totalPages > 1 && (
    <Pagination className="w-full" aria-label={label}>
      <Pagination.Summary>{t("users.management.pageSummary", { page: formatNumber(page, locale), totalPages: formatNumber(totalPages, locale) })}</Pagination.Summary>
      <Pagination.Content>
        <Pagination.Item>
          <Tooltip>
            <Pagination.Previous isDisabled={page <= 1 || isFetching} onPress={() => onPageChange(Math.max(1, page - 1))}>
              <Pagination.PreviousIcon><Icon name={direction === "rtl" ? "arrow-right" : "arrow-left"} /></Pagination.PreviousIcon>
              <span>{t("users.management.newerEvents")}</span>
            </Pagination.Previous>
            <Tooltip.Content>{t("users.management.newerEvents")}</Tooltip.Content>
          </Tooltip>
        </Pagination.Item>
        <Pagination.Item>
          <Tooltip>
            <Pagination.Next isDisabled={page >= totalPages || isFetching} onPress={() => onPageChange(Math.min(totalPages, page + 1))}>
              <span>{t("users.management.olderEvents")}</span>
              <Pagination.NextIcon><Icon name={direction === "rtl" ? "arrow-left" : "arrow-right"} /></Pagination.NextIcon>
            </Pagination.Next>
            <Tooltip.Content>{t("users.management.olderEvents")}</Tooltip.Content>
          </Tooltip>
        </Pagination.Item>
      </Pagination.Content>
    </Pagination>
  );

  if (history.items.length === 0) {
    return <div className="space-y-4"><p className="text-muted">{history.totalCount === 0 ? emptyMessage : t("users.management.noEventsOnPage")}</p>{pager}</div>;
  }

  return <Table>
    <Table.ScrollContainer>
      <Table.Content aria-label={label} className="min-w-[760px]">
        <Table.Header>
          <Table.Column isRowHeader>{t("users.management.eventTimeUtc")}</Table.Column>
          <Table.Column>{t("users.management.eventName")}</Table.Column>
          <Table.Column>{t("users.management.eventOutcome")}</Table.Column>
          <Table.Column>{t("users.management.eventActor")}</Table.Column>
          <Table.Column>{t("users.management.clientInterfaceHost")}</Table.Column>
          <Table.Column>{t("users.management.eventReason")}</Table.Column>
        </Table.Header>
        <Table.Body>
          {history.items.map((item, index) => <Table.Row key={`${item.occurredAt}:${item.operation}:${index}`}>
            <Table.Cell className="whitespace-nowrap text-muted">{formatDateTime(item.occurredAt, locale, { timeZone: "UTC" })}</Table.Cell>
            <Table.Cell className="whitespace-nowrap">{localizePlatformAuditEvent(locale, item.eventName, item.eventName)}</Table.Cell>
            <Table.Cell>
              <Chip className="whitespace-nowrap" color={auditOutcomeColor(item.outcome)} size="sm" variant="soft">
                <Chip.Label>{localizeSecurityAuditOutcome(locale, item.outcome, item.outcome)}</Chip.Label>
              </Chip>
            </Table.Cell>
            <Table.Cell><HistoryIdentifier value={item.actorId} fallback={t("users.management.notAvailable")} /></Table.Cell>
            <Table.Cell><HistoryIdentifier value={item.clientInterfaceHost} fallback={t("users.management.notAvailable")} /></Table.Cell>
            <Table.Cell>{item.redactedReason ?? "—"}</Table.Cell>
          </Table.Row>)}
        </Table.Body>
      </Table.Content>
    </Table.ScrollContainer>
    {pager && <Table.Footer>{pager}</Table.Footer>}
  </Table>;
}

function isContactIdentifierInUse(error: unknown): boolean {
  return hasUsersCommandFailure(error, UsersCommandErrorKind.ContactIdentifierInUse);
}

function isInvalidContact(error: unknown): boolean {
  return hasUsersCommandFailure(error, UsersCommandErrorKind.InvalidContact);
}

function hasUsersCommandFailure(error: unknown, failureCode: UsersCommandErrorKind): boolean {
  if (typeof error !== "object" || error === null) return false;
  const errorRecord = error as Record<string, unknown>;
  const problem = typeof errorRecord.problem === "object" && errorRecord.problem !== null
    ? errorRecord.problem as Record<string, unknown>
    : errorRecord;
  const errors = problem.errors;
  if (!Array.isArray(errors)) return false;
  return errors.some((entry) =>
    typeof entry === "object" && entry !== null &&
    (entry as Record<string, unknown>).failureCode === failureCode);
}

function readUnavailableSecurityDetails(error: unknown, userId: string): UserSecurityDetailsResponse | undefined {
  if (typeof error !== "object" || error === null) return undefined;
  const errorRecord = error as Record<string, unknown>;
  const problem = typeof errorRecord.problem === "object" && errorRecord.problem !== null
    ? errorRecord.problem as Record<string, unknown>
    : errorRecord;
  const status = typeof errorRecord.status === "number" ? errorRecord.status : problem.status;
  if (status !== 503 || !isUserSecurityDetailsResponse(problem, userId)) return undefined;
  return problem;
}

function isUserSecurityDetailsResponse(value: unknown, userId: string): value is UserSecurityDetailsResponse {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const details = value as Record<string, unknown>;
  if (typeof details.user !== "object" || details.user === null || Array.isArray(details.user)) return false;
  const user = details.user as Record<string, unknown>;
  return user.id === userId
    && isOptionalString(user.userName)
    && isOptionalString(user.firstName)
    && isOptionalString(user.lastName)
    && isOptionalString(user.email)
    && isOptionalString(user.phoneNumber)
    && isOptionalString(user.fullName)
    && isOptionalString(user.revision)
    && typeof user.isEnabled === "boolean"
    && Array.isArray(details.globalRoles) && details.globalRoles.every((role) => typeof role === "string")
    && Array.isArray(details.permittedGlobalRoles) && details.permittedGlobalRoles.every((role) => typeof role === "string")
    && Array.isArray(details.tenantRoles) && details.tenantRoles.every(isTenantRoleMembership)
    && isOptionalHistory(details.securityHistory)
    && isOptionalHistory(details.authenticationHistory)
    && typeof details.securityHistoryAvailable === "boolean"
    && typeof details.authenticationHistoryAvailable === "boolean"
    && (details.authenticationMethods === null || isAuthenticationMethods(details.authenticationMethods))
    && typeof details.authenticationMethodsUnavailable === "boolean";
}

function isAuthenticationMethods(value: unknown): value is UserAuthenticationMethodsResponse {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const methods = value as Record<string, unknown>;
  return typeof methods.passwordSet === "boolean"
    && typeof methods.emailVerified === "boolean"
    && typeof methods.phoneNumberVerified === "boolean"
    && Array.isArray(methods.passkeys) && methods.passkeys.every(isPasskey);
}

function isPasskey(value: unknown): value is UserPasskeyResponse {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const passkey = value as Record<string, unknown>;
  return typeof passkey.id === "string"
    && isOptionalString(passkey.friendlyName)
    && typeof passkey.registeredAt === "string"
    && (passkey.lastUsedAt === null || typeof passkey.lastUsedAt === "string");
}

function readHttpStatus(error: unknown): number | undefined {
  if (typeof error !== "object" || error === null) return undefined;
  const value = error as Record<string, unknown>;
  if (typeof value.status === "number") return value.status;
  if (typeof value.problem === "object" && value.problem !== null && typeof (value.problem as Record<string, unknown>).status === "number") {
    return (value.problem as Record<string, number>).status;
  }
  return undefined;
}

function isOptionalString(value: unknown): boolean {
  return value === undefined || value === null || typeof value === "string";
}

function isTenantRoleMembership(value: unknown): value is TenantRoleMembershipResponse {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const membership = value as Record<string, unknown>;
  return typeof membership.tenantId === "string" && typeof membership.roleSuffix === "string";
}

function isOptionalHistory(value: unknown): value is UserSecurityHistoryResponse | null {
  if (value === null) return true;
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const history = value as Record<string, unknown>;
  return Array.isArray(history.items) && history.items.every(isUserSecurityEvent)
    && isPageValue(history.page) && isPageValue(history.pageSize) && isPageValue(history.totalCount);
}

function isPageValue(value: unknown): boolean {
  return typeof value === "number" ? Number.isFinite(value) : typeof value === "string" && /^\d+$/.test(value);
}

function isUserSecurityEvent(value: unknown): value is UserSecurityEventResponse {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const event = value as Record<string, unknown>;
  return typeof event.eventName === "string"
    && (event.eventCategory === null || typeof event.eventCategory === "string")
    && typeof event.outcome === "string"
    && (event.actorId === null || typeof event.actorId === "string")
    && typeof event.operation === "string"
    && typeof event.occurredAt === "string"
    && (event.clientInterfaceHost === null || typeof event.clientInterfaceHost === "string")
    && typeof event.isAuthenticationEvent === "boolean"
    && (event.redactedReason === null || typeof event.redactedReason === "string");
}
