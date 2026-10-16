import { useMemo } from "react";
import { useUsersListTenantMembers } from "@vitarish/auth";

/**
 * Display names of the practice's members, keyed by user id, for labelling who wrote a procedure item.
 * SEA items carry only the author's id. Names are an enrichment: when the viewer may not list members,
 * or the lookup fails, authors fall back to the generic care-team label.
 */
export function useProcedureEventAuthorNames(): ReadonlyMap<string, string> {
  const members = useUsersListTenantMembers(undefined, {
    query: { retry: false, staleTime: 5 * 60_000 },
  });
  return useMemo(
    () =>
      new Map(
        (members.data ?? []).flatMap(({ user }) => {
          const name = user.fullName?.trim() || user.userName?.trim();
          return name ? [[user.id, name] as const] : [];
        }),
      ),
    [members.data],
  );
}
