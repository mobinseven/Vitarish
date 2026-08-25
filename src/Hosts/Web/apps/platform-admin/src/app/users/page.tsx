"use client";

import { useState } from "react";
import Link from "next/link";
import { useAuthSession, usePlatformAuthorization, useUsersAdministrationListTenantRoles, useUsersSearchUsers } from "@vitarish/auth";
import { CenteredRegion } from "@vitarish/ui/layout";
import { useLocalizedText } from "@vitarish/localization";
import { Routes } from "../routes";

export default function UsersPage() {
  const { isAuthenticated, isLoading: sessionLoading } = useAuthSession();
  const { hasAccess } = usePlatformAuthorization();
  const { t } = useLocalizedText();
  const [input, setInput] = useState("");
  const [query, setQuery] = useState("");
  const [selectedUserId, setSelectedUserId] = useState("");
  const users = useUsersSearchUsers({ Query: query || undefined, MaxResults: 50, IncludeTenantUsers: false }, { query: { enabled: isAuthenticated && hasAccess } });
  const memberships = useUsersAdministrationListTenantRoles(selectedUserId);

  if (sessionLoading) return <CenteredRegion><p>{t("common.states.loading")}</p></CenteredRegion>;
  if (!isAuthenticated || !hasAccess) return <CenteredRegion><h1 className="text-2xl font-semibold text-danger">{t("admin.forbiddenTitle")}</h1><p className="mt-2 text-muted">{t("admin.forbiddenDescription")}</p></CenteredRegion>;

  return <main className="mx-auto max-w-6xl space-y-6 p-6">
    <header className="flex items-start justify-between gap-4"><div><h1 className="text-2xl font-semibold">{t("admin.users.title")}</h1><p className="mt-2 text-muted">{t("admin.users.description")}</p></div><Link className="rounded bg-primary px-4 py-2 text-primary-foreground" href={Routes.Users.New}>{t("admin.users.createTitle")}</Link></header>
    <form className="flex gap-3" onSubmit={(event) => { event.preventDefault(); setQuery(input.trim()); setSelectedUserId(""); }}>
      <label className="sr-only" htmlFor="user-search">{t("admin.users.searchLabel")}</label>
      <input id="user-search" className="min-w-0 flex-1 rounded border px-3 py-2" value={input} onChange={(event) => setInput(event.target.value)} placeholder={t("admin.users.searchPlaceholder")} />
      <button className="rounded bg-primary px-4 py-2 text-primary-foreground" type="submit">{t("admin.users.searchAction")}</button>
    </form>
    {users.isLoading && <p>{t("admin.users.loading")}</p>}
    {users.isError && <div><p className="text-danger">{t("admin.users.error")}</p><button className="underline" onClick={() => users.refetch()}>{t("admin.users.retry")}</button></div>}
    {!users.isLoading && !users.isError && !users.data?.length && <p className="text-muted">{t("admin.users.empty")}</p>}
    {!!users.data?.length && <div className="grid gap-6 lg:grid-cols-[1fr_1fr]"><div className="overflow-x-auto"><table className="w-full text-left"><thead><tr><th>{t("admin.users.user")}</th><th>{t("admin.users.email")}</th><th>{t("admin.users.phone")}</th></tr></thead><tbody>{users.data.map((user) => <tr key={user.id} className="cursor-pointer border-t" onClick={() => setSelectedUserId(user.id)}><td className="py-3">{user.fullName ?? user.userName ?? user.id}</td><td>{user.email ?? "—"}</td><td>{user.phoneNumber ?? "—"}</td></tr>)}</tbody></table></div><section><h2 className="text-lg font-semibold">{t("admin.users.memberships")}</h2>{!selectedUserId && <p className="mt-2 text-muted">{t("admin.users.selectPrompt")}</p>}{selectedUserId && memberships.isLoading && <p className="mt-2">{t("admin.users.membershipsLoading")}</p>}{selectedUserId && memberships.isError && <div className="mt-2"><p className="text-danger">{t("admin.users.membershipsError")}</p><button className="underline" onClick={() => memberships.refetch()}>{t("admin.users.retry")}</button></div>}{selectedUserId && !memberships.isLoading && !memberships.isError && !memberships.data?.length && <p className="mt-2 text-muted">{t("admin.users.noMemberships")}</p>}{!!memberships.data?.length && <ul className="mt-3 space-y-2">{memberships.data.map((membership) => <li className="rounded border p-3" key={`${membership.tenantId}:${membership.roleSuffix}`}><div>{t("admin.users.tenant")}: {membership.tenantId}</div><div>{t("admin.users.role")}: {membership.roleSuffix}</div></li>)}</ul>}</section></div>}
  </main>;
}
