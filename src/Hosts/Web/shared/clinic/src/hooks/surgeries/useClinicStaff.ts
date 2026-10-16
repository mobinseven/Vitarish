import type { UseQueryResult } from "@tanstack/react-query";
import { useUsersListTenantMembers } from "@vitarish/auth";
import type { ClinicStaffMember } from "../../models/surgeries/surgeryModels";
import { surgeryKeys } from "./surgeryKeys";

const maxStaffResults = 200;
const staffStaleTimeMs = 5 * 60 * 1000;

/** The clinic's staff members, for picking a surgery team. */
export function useClinicStaff(): UseQueryResult<ClinicStaffMember[], unknown> {
  return useUsersListTenantMembers<ClinicStaffMember[]>(
    { MaxResults: maxStaffResults },
    {
      query: {
        queryKey: surgeryKeys.staff(),
        retry: false,
        staleTime: staffStaleTimeMs,
        select: (members) =>
          members.map((member) => ({
            userId: member.user.id,
            displayName:
              member.user.fullName?.trim() ||
              member.user.userName?.trim() ||
              member.user.id,
          })),
      },
    },
  );
}
