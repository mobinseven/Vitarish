"use client";

import { useEffect, useId, useState, type FormEvent } from "react";
import {
  useAuthSession,
  usePlatformAuthorization,
  useTenantAuthorization,
  useUsersAdministrationCreateWithEmail,
  useUsersAdministrationCreateWithPhone,
  useUsersAdministrationGrantTenantRole,
  useUsersAdministrationGetUserSecurity,
  useUsersAdministrationListTenantRoles,
  useUsersAdministrationRevokeTenantRole,
  useUsersAdministrationUpdateUser,
  useUsersListTenantMembers,
  useUsersProvisionTenantMember,
  useUsersSearchUsers,
  useUsersRemoveTenantMember,
} from "@vitarish/auth";
import type { TenantRoleMembershipResponse, UserSecurityDetailsResponse, UserSecurityEventResponse, UserSecurityHistoryResponse } from "@vitarish/auth";
import { tenantRoleSuffixes } from "@vitarish/auth/users";
import type { TenantRoleSuffix } from "@vitarish/auth/users";
import { formatDateTime, formatNumber, useLocalizedText } from "@vitarish/localization";
import { localizePlatformAuditEvent, localizeSecurityAuditOutcome } from "@vitarish/security/localization";

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

function SurfaceHeader({ title, description }: { title: string; description?: string }) {
  return (
    <header>
      <h1 className="text-2xl font-semibold">{title}</h1>
      {description && <p className="mt-2 text-muted">{description}</p>}
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
  const phoneContactHintId = useId();
  const [contactType, setContactType] = useState<"email" | "phone">("email");
  const [contact, setContact] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [message, setMessage] = useState("");

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const data = contactType === "email"
      ? { email: contact.trim(), firstName: firstName.trim() || undefined, lastName: lastName.trim() || undefined }
      : { phoneNumber: contact.trim(), firstName: firstName.trim() || undefined, lastName: lastName.trim() || undefined };
    provision.mutate({ data }, {
      onSuccess: async () => {
        setContact("");
        setFirstName("");
        setLastName("");
        setMessage(t("users.management.created"));
        await onRefresh();
      },
      onError: () => setMessage(t("users.management.createError")),
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
      <section className="rounded border p-4">
        <h2 className="text-lg font-semibold">{t("users.management.addMember")}</h2>
        <form className="mt-4 grid gap-3 md:grid-cols-2" onSubmit={submit}>
          <label>{t("users.management.contactType")}
            <select className="mt-1 w-full rounded border p-2" value={contactType} onChange={(event) => setContactType(event.target.value as "email" | "phone")}>
              <option value="email">{t("users.management.emailContact")}</option>
              <option value="phone">{t("users.management.phoneContact")}</option>
            </select>
          </label>
          <div>
            <label>{contactType === "email" ? t("users.management.emailContact") : t("users.management.phoneContact")}
              <input className="mt-1 w-full rounded border p-2" required value={contact} onChange={(event) => setContact(event.target.value)} aria-describedby={contactType === "phone" ? phoneContactHintId : undefined} />
            </label>
            {contactType === "phone" && <p className="mt-1 text-sm text-muted" id={phoneContactHintId}>{t("users.management.phoneContactHint")}</p>}
          </div>
          <label>{t("users.management.firstName")} <span className="text-muted">({t("users.management.optional")})</span>
            <input className="mt-1 w-full rounded border p-2" value={firstName} onChange={(event) => setFirstName(event.target.value)} />
          </label>
          <label>{t("users.management.lastName")} <span className="text-muted">({t("users.management.optional")})</span>
            <input className="mt-1 w-full rounded border p-2" value={lastName} onChange={(event) => setLastName(event.target.value)} />
          </label>
          <button className="inline-flex w-fit items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={provision.isPending} title={t("users.management.addMember")}>
            <Icon name="user-plus" /> {provision.isPending ? t("users.management.adding") : t("users.management.addMember")}
          </button>
        </form>
      </section>
      {message && <p role="status" aria-live="polite">{message}</p>}
      {isLoading && <p aria-live="polite">{t("users.management.loading")}</p>}
      {isError && <div role="alert"><p className="text-danger">{t("users.management.error")}</p><button className="inline-flex items-center gap-2 underline" onClick={onRetry} title={t("users.management.retry")}><Icon name="arrow-rotate-right" />{t("users.management.retry")}</button></div>}
      {!isLoading && !isError && members.length === 0 && <p className="text-muted">{t("users.management.empty")}</p>}
      {!isLoading && !isError && members.length > 0 && <ul className="space-y-3" aria-label={t("users.management.memberships")}>
        {members.map((member) => {
          const isOwner = member.roleSuffix === tenantRoleSuffixes.owner;
          const displayName = member.user.fullName ?? member.user.userName ?? member.user.email ?? member.user.phoneNumber ?? member.user.id;
          return <li className="flex flex-wrap items-center justify-between gap-3 rounded border p-4" key={member.user.id}>
            <div><p className="font-medium">{displayName}</p><p className="text-sm text-muted">{isOwner ? t("users.management.owner") : t("users.management.staff")}{member.user.email ? ` · ${member.user.email}` : ""}{member.user.phoneNumber ? ` · ${member.user.phoneNumber}` : ""}</p></div>
            {!isOwner && <button className="inline-flex items-center gap-2 rounded border px-3 py-2 text-danger" disabled={remove.isPending} onClick={() => removeMember(member.user.id)} title={t("users.management.remove")}><Icon name="user-minus" />{t("users.management.remove")}</button>}
          </li>;
        })}
      </ul>}
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
    <form className="flex gap-3" onSubmit={(event) => { event.preventDefault(); onSearch(); }}>
      <label className="sr-only" htmlFor="user-management-search">{t("users.management.searchLabel")}</label>
      <input id="user-management-search" className="min-w-0 flex-1 rounded border px-3 py-2" value={input} onChange={(event) => onInputChange(event.target.value)} placeholder={t("users.management.searchPlaceholder")} />
      <button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" title={t("users.management.search")}><Icon name="magnifying-glass" />{t("users.management.search")}</button>
    </form>
    {isLoading && <p aria-live="polite">{t("users.management.loading")}</p>}
    {isError && <div role="alert"><p className="text-danger">{t("users.management.error")}</p><button className="inline-flex items-center gap-2 underline" onClick={onRetry} title={t("users.management.retry")}><Icon name="arrow-rotate-right" />{t("users.management.retry")}</button></div>}
    {!isLoading && !isError && users.length === 0 && <p className="text-muted">{t("users.management.empty")}</p>}
    {users.length > 0 && <div className="grid gap-6 lg:grid-cols-[1fr_1fr]"><div className="overflow-x-auto"><table className="w-full text-left"><thead><tr><th>{t("users.management.name")}</th><th>{t("users.management.email")}</th><th>{t("users.management.phone")}</th><th><span className="sr-only">{t("users.management.userDetails")}</span></th></tr></thead><tbody>{users.map((user) => <tr className="border-t" key={user.id} onClick={() => onSelect(user.id)}><td className="py-3">{user.fullName ?? user.userName ?? user.id}</td><td>{user.email ?? "—"}</td><td>{user.phoneNumber ?? "—"}</td><td><button className="inline-flex items-center gap-2 rounded border px-3 py-2" type="button" onClick={(event) => { event.stopPropagation(); onOpenUserDetails?.(user.id); }} title={t("users.management.userDetails")} aria-label={t("users.management.userDetails")}><Icon name="user-gear" /></button></td></tr>)}</tbody></table></div><MembershipList userId={selectedUserId} memberships={memberships} t={t} /></div>}
  </main>;
}

function MembershipList({ userId, memberships, t }: { userId: string; memberships: MembershipQuery; t: Translate }) {
  return <section><h2 className="text-lg font-semibold">{t("users.management.memberships")}</h2>{!userId && <p className="mt-2 text-muted">{t("users.management.selectUser")}</p>}{userId && memberships.isLoading && <p className="mt-2">{t("users.management.membershipLoading")}</p>}{userId && memberships.isError && <p className="mt-2 text-danger">{t("users.management.membershipError")}</p>}{userId && !memberships.isLoading && !memberships.isError && !memberships.data?.length && <p className="mt-2 text-muted">{t("users.management.noMemberships")}</p>}{!!memberships.data?.length && <ul className="mt-3 space-y-2">{memberships.data.map((membership) => <li className="rounded border p-3" key={`${membership.tenantId}:${membership.roleSuffix}`}><div>{t("users.management.tenant")}: {membership.tenantId}</div><div>{t("users.management.role")}: {membership.roleSuffix}</div></li>)}</ul>}</section>;
}

function PlatformCreateUser({ title, t }: { title: string; t: Translate }) {
  const phoneContactHintId = useId();
  const [contactType, setContactType] = useState<"email" | "phone">("email");
  const [contact, setContact] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [message, setMessage] = useState("");
  const email = useUsersAdministrationCreateWithEmail();
  const phone = useUsersAdministrationCreateWithPhone();
  const pending = email.isPending || phone.isPending;
  const submit = (event: FormEvent) => {
    event.preventDefault();
    const onSuccess = () => { setMessage(t("users.management.createdPlatform")); setContact(""); };
    if (contactType === "email") email.mutate({ data: { email: contact.trim(), firstName: firstName.trim(), lastName: lastName.trim() } }, { onSuccess, onError: () => setMessage(t("users.management.createError")) });
    else phone.mutate({ data: { phoneNumber: contact.trim(), firstName: firstName.trim(), lastName: lastName.trim() } }, { onSuccess, onError: () => setMessage(t("users.management.createError")) });
  };
  return <main className="max-w-2xl space-y-5"><SurfaceHeader title={title} /><form className="space-y-4" onSubmit={submit}><label className="block">{t("users.management.contactType")}<select className="mt-1 w-full rounded border p-2" value={contactType} onChange={(event) => setContactType(event.target.value as "email" | "phone")}><option value="email">{t("users.management.emailContact")}</option><option value="phone">{t("users.management.phoneContact")}</option></select></label><div><label className="block">{contactType === "email" ? t("users.management.emailContact") : t("users.management.phoneContact")}<input className="mt-1 w-full rounded border p-2" required value={contact} onChange={(event) => setContact(event.target.value)} aria-describedby={contactType === "phone" ? phoneContactHintId : undefined} /></label>{contactType === "phone" && <p className="mt-1 text-sm text-muted" id={phoneContactHintId}>{t("users.management.phoneContactHint")}</p>}</div><label className="block">{t("users.management.firstName")}<input className="mt-1 w-full rounded border p-2" value={firstName} onChange={(event) => setFirstName(event.target.value)} /></label><label className="block">{t("users.management.lastName")}<input className="mt-1 w-full rounded border p-2" value={lastName} onChange={(event) => setLastName(event.target.value)} /></label><button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={pending} title={t("users.management.createUser")}><Icon name="user-plus" />{pending ? t("users.management.adding") : t("users.management.createUser")}</button></form>{message && <p role="status">{message}</p>}</main>;
}

function PlatformUserDetail({ title, userId, memberships, t }: { title: string; userId: string; memberships: MembershipQuery; t: Translate }) {
  const phoneContactHintId = useId();
  const { locale } = useLocalizedText();
  const grant = useUsersAdministrationGrantTenantRole();
  const revoke = useUsersAdministrationRevokeTenantRole();
  const update = useUsersAdministrationUpdateUser();
  const [page, setPage] = useState(1);
  const [tenantId, setTenantId] = useState("");
  const [roleSuffix, setRoleSuffix] = useState<TenantRoleSuffix>(tenantRoleSuffixes.user);
  const [message, setMessage] = useState("");
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [revision, setRevision] = useState<string | null>(null);
  const pageSize = 25;
  const securityQuery = useUsersAdministrationGetUserSecurity(
    userId,
    { page, pageSize },
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
  const totalPages = Math.max(
    1,
    Math.ceil(Number(securityHistory?.totalCount ?? 0) / pageSize),
    Math.ceil(Number(authenticationHistory?.totalCount ?? 0) / pageSize),
  );

  useEffect(() => {
    setPage(1);
  }, [userId]);

  useEffect(() => {
    const user = securityDetails?.user;
    if (!user) return;
    setEmail(user.email ?? "");
    setPhoneNumber(user.phoneNumber ?? "");
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
        await refreshSecurityDetails();
      },
      onError: async (error) => {
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
  if (securityQuery.isLoading && !securityDetails) {
    return <main className="max-w-4xl space-y-6"><SurfaceHeader title={title} description={userId} /><p aria-live="polite">{t("users.management.loadingSecurity")}</p></main>;
  }

  if (!securityDetails) {
    return <main className="max-w-4xl space-y-6"><SurfaceHeader title={title} description={userId} /><div role="alert"><p className="text-danger">{t("users.management.securityDetailsError")}</p><button className="inline-flex items-center gap-2 underline" onClick={() => securityQuery.refetch()} title={t("users.management.retry")}><Icon name="arrow-rotate-right" />{t("users.management.retry")}</button></div></main>;
  }

  return <main className="max-w-5xl space-y-6">
    <SurfaceHeader title={title} description={securityDetails.user.fullName ?? securityDetails.user.userName ?? undefined} />
    {historyUnavailable && <p className="rounded border border-warning p-3" role="status">{t("users.management.historyUnavailable")}</p>}
    {securityQuery.isError && <button className="inline-flex items-center gap-2 underline" onClick={() => securityQuery.refetch()} title={t("users.management.retry")}><Icon name="arrow-rotate-right" />{t("users.management.retry")}</button>}

    <section className="space-y-3 rounded border p-4">
      <h2 className="text-lg font-semibold">{t("users.management.profile")}</h2>
      <dl className="grid gap-3 sm:grid-cols-2">
        <div><dt className="text-sm text-muted">{t("users.management.userIdentifier")}</dt><dd className="break-all font-mono">{securityDetails.user.id}</dd></div>
        <div><dt className="text-sm text-muted">{t("users.management.name")}</dt><dd>{securityDetails.user.fullName ?? securityDetails.user.userName ?? t("users.management.notAvailable")}</dd></div>
      </dl>
      <form className="grid gap-3 md:grid-cols-2" onSubmit={submitProfile}>
        <label>{t("users.management.emailContact")}
          <input className="mt-1 w-full rounded border p-2" type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} />
        </label>
        <div>
          <label>{t("users.management.phoneContact")}
            <input className="mt-1 w-full rounded border p-2" type="tel" autoComplete="tel" value={phoneNumber} onChange={(event) => setPhoneNumber(event.target.value)} aria-describedby={phoneContactHintId} />
          </label>
          <p className="mt-1 text-sm text-muted" id={phoneContactHintId}>{t("users.management.phoneContactHint")}</p>
        </div>
        <label>{t("users.management.firstName")}
          <input className="mt-1 w-full rounded border p-2" autoComplete="given-name" value={firstName} onChange={(event) => setFirstName(event.target.value)} />
        </label>
        <label>{t("users.management.lastName")}
          <input className="mt-1 w-full rounded border p-2" autoComplete="family-name" value={lastName} onChange={(event) => setLastName(event.target.value)} />
        </label>
        <button className="inline-flex w-fit items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={update.isPending} title={t("users.management.saveChanges")}>
          <Icon name="floppy-disk" />{update.isPending ? t("users.management.savingChanges") : t("users.management.saveChanges")}
        </button>
      </form>
    </section>

    <section className="space-y-3 rounded border p-4">
      <h2 className="text-lg font-semibold">{t("users.management.globalRoles")}</h2>
      {securityDetails.globalRoles.length === 0
        ? <p className="text-muted">{t("users.management.noRoles")}</p>
        : <ul className="flex flex-wrap gap-2">{securityDetails.globalRoles.map((role) => <li className="rounded border px-3 py-1" key={role}>{role}</li>)}</ul>}
      <h3 className="pt-2 font-medium">{t("users.management.memberships")}</h3>
      {memberships.isLoading && <p aria-live="polite">{t("users.management.membershipLoading")}</p>}
      {memberships.isError && <p className="text-danger">{t("users.management.membershipError")}</p>}
      {!memberships.isLoading && !memberships.isError && tenantRoles.length === 0 && <p className="text-muted">{t("users.management.noMemberships")}</p>}
      {tenantRoles.map((membership) => <div className="mt-2 flex flex-wrap items-center justify-between gap-3 rounded border p-3" key={`${membership.tenantId}:${membership.roleSuffix}`}>
        <span>{t("users.management.tenant")}: {membership.tenantId} · {t("users.management.role")}: {membership.roleSuffix}</span>
        <button className="inline-flex items-center gap-2 text-danger" disabled={revoke.isPending || memberships.isError} onClick={() => revokeRole(membership.tenantId, membership.roleSuffix)} title={t("users.management.revoke")}><Icon name="user-minus" />{t("users.management.revoke")}</button>
      </div>)}
      <form className="space-y-3 border-t pt-4" onSubmit={submit}>
        <h3 className="font-medium">{t("users.management.grant")}</h3>
        <label className="block">{t("users.management.tenantId")}<input className="mt-1 w-full rounded border p-2" required value={tenantId} onChange={(event) => setTenantId(event.target.value)} /></label>
        <label className="block">{t("users.management.roleToGrant")}<select className="mt-1 rounded border p-2" value={roleSuffix} onChange={(event) => setRoleSuffix(event.target.value === tenantRoleSuffixes.owner ? tenantRoleSuffixes.owner : tenantRoleSuffixes.user)}><option value={tenantRoleSuffixes.user}>{tenantRoleSuffixes.user}</option><option value={tenantRoleSuffixes.owner}>{tenantRoleSuffixes.owner}</option></select></label>
        <button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={grant.isPending || !tenantId.trim()} title={t("users.management.grant")}><Icon name="user-check" />{t("users.management.grant")}</button>
      </form>
    </section>

    <section className="space-y-4 rounded border p-4">
      <h2 className="text-lg font-semibold">{t("users.management.authenticationHistory")}</h2>
      <UserSecurityHistoryList history={authenticationHistory} available={authenticationHistoryAvailable} emptyMessage={t("users.management.authenticationHistoryEmpty")} t={t} locale={locale} />
    </section>
    <section className="space-y-4 rounded border p-4">
      <h2 className="text-lg font-semibold">{t("users.management.securityHistory")}</h2>
      <UserSecurityHistoryList history={securityHistory} available={securityHistoryAvailable} emptyMessage={t("users.management.securityHistoryEmpty")} t={t} locale={locale} />
    </section>

    {totalPages > 1 && <nav className="flex flex-wrap items-center justify-between gap-3" aria-label={t("users.management.securityHistory")}>
      <p className="text-sm text-muted">{t("users.management.pageSummary", { page: formatNumber(page, locale), totalPages: formatNumber(totalPages, locale) })}</p>
      <div className="flex gap-3">
        {page > 1 && <button className="inline-flex items-center gap-2 rounded border px-3 py-2" onClick={() => setPage(1)} title={t("users.management.newestEvents")}><Icon name="arrow-up" />{t("users.management.newestEvents")}</button>}
        {page < totalPages && <button className="inline-flex items-center gap-2 rounded border px-3 py-2" onClick={() => setPage((current) => current + 1)} title={t("users.management.loadOlderEvents")}><Icon name="arrow-down" />{t("users.management.loadOlderEvents")}</button>}
      </div>
    </nav>}
    {message && <p role="status" aria-live="polite">{message}</p>}
  </main>;
}

function UserSecurityHistoryList({ history, available, emptyMessage, t, locale }: {
  history: UserSecurityHistoryResponse | undefined;
  available: boolean;
  emptyMessage: string;
  t: Translate;
  locale: ReturnType<typeof useLocalizedText>["locale"];
}) {
  if (!history) {
    return available
      ? <p className="text-muted">{emptyMessage}</p>
      : null;
  }

  if (history.items.length === 0) {
    return <p className="text-muted">{history.totalCount === 0 ? emptyMessage : t("users.management.noEventsOnPage")}</p>;
  }

  return <ol className="space-y-3">{history.items.map((item, index) => <li className="rounded border p-3" key={`${item.occurredAt}:${item.operation}:${index}`}>
    <div className="flex flex-wrap items-start justify-between gap-2">
      <p className="font-medium">{localizePlatformAuditEvent(locale, item.eventName, item.eventName)}</p>
      <span className="rounded border px-2 py-1 text-sm">{localizeSecurityAuditOutcome(locale, item.outcome, item.outcome)}</span>
    </div>
    <dl className="mt-3 grid gap-2 text-sm sm:grid-cols-2">
      <div><dt className="text-muted">{t("users.management.eventTimeUtc")}</dt><dd>{formatDateTime(item.occurredAt, locale, { timeZone: "UTC" })}</dd></div>
      <div><dt className="text-muted">{t("users.management.clientInterfaceHost")}</dt><dd className="break-all">{item.clientInterfaceHost ?? t("users.management.notAvailable")}</dd></div>
      <div><dt className="text-muted">{t("users.management.eventActor")}</dt><dd className="break-all">{item.actorId ?? t("users.management.notAvailable")}</dd></div>
      {item.redactedReason && <div><dt className="text-muted">{t("users.management.eventReason")}</dt><dd>{item.redactedReason}</dd></div>}
    </dl>
  </li>)}</ol>;
}

function readHttpStatus(error: unknown): number | undefined {
  if (typeof error !== "object" || error === null) return undefined;
  const status = (error as Record<string, unknown>).status;
  return typeof status === "number" ? status : undefined;
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
    && Array.isArray(details.globalRoles) && details.globalRoles.every((role) => typeof role === "string")
    && Array.isArray(details.tenantRoles) && details.tenantRoles.every(isTenantRoleMembership)
    && isOptionalHistory(details.securityHistory)
    && isOptionalHistory(details.authenticationHistory)
    && typeof details.securityHistoryAvailable === "boolean"
    && typeof details.authenticationHistoryAvailable === "boolean";
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
