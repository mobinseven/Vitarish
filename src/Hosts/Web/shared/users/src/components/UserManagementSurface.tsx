"use client";

import { useState, type FormEvent } from "react";
import {
  useAuthSession,
  usePlatformAuthorization,
  useTenantAuthorization,
  useUsersAdministrationCreateWithEmail,
  useUsersAdministrationCreateWithPhone,
  useUsersAdministrationGrantTenantRole,
  useUsersAdministrationListTenantRoles,
  useUsersAdministrationRevokeTenantRole,
  useUsersListTenantMembers,
  useUsersProvisionTenantMember,
  useUsersSearchUsers,
  useUsersRemoveTenantMember,
} from "@vitarish/auth";
import type { TenantRoleMembershipResponse } from "@vitarish/auth";
import { tenantRoleSuffixes } from "@vitarish/auth/users";
import type { TenantRoleSuffix } from "@vitarish/auth/users";
import { useLocalizedText } from "@vitarish/localization";

export type UserManagementMode = "tenant" | "platform";
export type UserManagementPlatformView = "directory" | "create" | "detail";

export interface UserManagementSurfaceProps {
  mode: UserManagementMode;
  title?: string;
  description?: string;
  platformView?: UserManagementPlatformView;
  userId?: string;
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
          <label>{contactType === "email" ? t("users.management.emailContact") : t("users.management.phoneContact")}
            <input className="mt-1 w-full rounded border p-2" required value={contact} onChange={(event) => setContact(event.target.value)} />
          </label>
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
  title, description, input, onInputChange, onSearch, users, selectedUserId, onSelect, memberships, isLoading, isError, onRetry, t,
}: {
  title: string; description?: string; input: string; onInputChange: (value: string) => void; onSearch: () => void;
  users: Array<{ id: string; fullName?: string | null; userName?: string | null; email?: string | null; phoneNumber?: string | null }>;
  selectedUserId: string; onSelect: (id: string) => void; memberships: MembershipQuery; isLoading: boolean; isError: boolean; onRetry: () => void; t: Translate;
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
    {users.length > 0 && <div className="grid gap-6 lg:grid-cols-[1fr_1fr]"><div className="overflow-x-auto"><table className="w-full text-left"><thead><tr><th>{t("users.management.name")}</th><th>{t("users.management.email")}</th><th>{t("users.management.phone")}</th></tr></thead><tbody>{users.map((user) => <tr className="cursor-pointer border-t" key={user.id} onClick={() => onSelect(user.id)}><td className="py-3">{user.fullName ?? user.userName ?? user.id}</td><td>{user.email ?? "—"}</td><td>{user.phoneNumber ?? "—"}</td></tr>)}</tbody></table></div><MembershipList userId={selectedUserId} memberships={memberships} t={t} /></div>}
  </main>;
}

function MembershipList({ userId, memberships, t }: { userId: string; memberships: MembershipQuery; t: Translate }) {
  return <section><h2 className="text-lg font-semibold">{t("users.management.memberships")}</h2>{!userId && <p className="mt-2 text-muted">{t("users.management.selectUser")}</p>}{userId && memberships.isLoading && <p className="mt-2">{t("users.management.membershipLoading")}</p>}{userId && memberships.isError && <p className="mt-2 text-danger">{t("users.management.membershipError")}</p>}{userId && !memberships.isLoading && !memberships.isError && !memberships.data?.length && <p className="mt-2 text-muted">{t("users.management.noMemberships")}</p>}{!!memberships.data?.length && <ul className="mt-3 space-y-2">{memberships.data.map((membership) => <li className="rounded border p-3" key={`${membership.tenantId}:${membership.roleSuffix}`}><div>{t("users.management.tenant")}: {membership.tenantId}</div><div>{t("users.management.role")}: {membership.roleSuffix}</div></li>)}</ul>}</section>;
}

function PlatformCreateUser({ title, t }: { title: string; t: Translate }) {
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
  return <main className="max-w-2xl space-y-5"><SurfaceHeader title={title} /><form className="space-y-4" onSubmit={submit}><label className="block">{t("users.management.contactType")}<select className="mt-1 w-full rounded border p-2" value={contactType} onChange={(event) => setContactType(event.target.value as "email" | "phone")}><option value="email">{t("users.management.emailContact")}</option><option value="phone">{t("users.management.phoneContact")}</option></select></label><label className="block">{contactType === "email" ? t("users.management.emailContact") : t("users.management.phoneContact")}<input className="mt-1 w-full rounded border p-2" required value={contact} onChange={(event) => setContact(event.target.value)} /></label><label className="block">{t("users.management.firstName")}<input className="mt-1 w-full rounded border p-2" value={firstName} onChange={(event) => setFirstName(event.target.value)} /></label><label className="block">{t("users.management.lastName")}<input className="mt-1 w-full rounded border p-2" value={lastName} onChange={(event) => setLastName(event.target.value)} /></label><button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={pending} title={t("users.management.createUser")}><Icon name="user-plus" />{pending ? t("users.management.adding") : t("users.management.createUser")}</button></form>{message && <p role="status">{message}</p>}</main>;
}

function PlatformUserDetail({ title, userId, memberships, t }: { title: string; userId: string; memberships: MembershipQuery; t: Translate }) {
  const grant = useUsersAdministrationGrantTenantRole();
  const revoke = useUsersAdministrationRevokeTenantRole();
  const [tenantId, setTenantId] = useState("");
  const [roleSuffix, setRoleSuffix] = useState<TenantRoleSuffix>(tenantRoleSuffixes.user);
  const [message, setMessage] = useState("");
  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (!tenantId.trim()) return;
    grant.mutate({ userId, tenantId: tenantId.trim(), roleSuffix, data: { roleSuffix } }, { onSuccess: () => { setMessage(t("users.management.roleGranted")); memberships.refetch(); }, onError: () => setMessage(t("users.management.roleError")) });
  };
  const revokeRole = (tenant: string, role: string) => {
    if (!window.confirm(t("users.management.revokeConfirm"))) return;
    revoke.mutate({ userId, tenantId: tenant, roleSuffix: role, data: { roleSuffix: role } }, { onSuccess: () => { setMessage(t("users.management.roleRevoked")); memberships.refetch(); }, onError: () => setMessage(t("users.management.roleError")) });
  };
  return <main className="max-w-3xl space-y-6"><SurfaceHeader title={title} description={userId} /><section><h2 className="text-lg font-semibold">{t("users.management.memberships")}</h2>{memberships.isLoading && <p>{t("users.management.membershipLoading")}</p>}{memberships.isError && <p className="text-danger">{t("users.management.membershipError")}</p>}{memberships.data?.map((membership) => <div className="mt-2 flex justify-between gap-3 rounded border p-3" key={`${membership.tenantId}:${membership.roleSuffix}`}><span>{membership.tenantId} · {membership.roleSuffix}</span><button className="inline-flex items-center gap-2 text-danger" disabled={revoke.isPending} onClick={() => revokeRole(membership.tenantId, membership.roleSuffix)} title={t("users.management.revoke")}><Icon name="user-minus" />{t("users.management.revoke")}</button></div>)}</section><form className="space-y-3 rounded border p-4" onSubmit={submit}><h2 className="text-lg font-semibold">{t("users.management.grant")}</h2><label className="block">{t("users.management.tenantId")}<input className="mt-1 w-full rounded border p-2" required value={tenantId} onChange={(event) => setTenantId(event.target.value)} /></label><label className="block">{t("users.management.roleToGrant")}<select className="mt-1 rounded border p-2" value={roleSuffix} onChange={(event) => setRoleSuffix(event.target.value === tenantRoleSuffixes.owner ? tenantRoleSuffixes.owner : tenantRoleSuffixes.user)}><option value={tenantRoleSuffixes.user}>{tenantRoleSuffixes.user}</option><option value={tenantRoleSuffixes.owner}>{tenantRoleSuffixes.owner}</option></select></label><button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={grant.isPending || !tenantId.trim()} title={t("users.management.grant")}><Icon name="user-check" />{t("users.management.grant")}</button></form>{message && <p role="status">{message}</p>}</main>;
}
