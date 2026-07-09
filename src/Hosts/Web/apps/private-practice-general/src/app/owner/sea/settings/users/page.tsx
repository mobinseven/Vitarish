import type { Metadata } from "next";
import { SeaRolesPage } from "@/features/sea/roles/components";

export const metadata: Metadata = {
  title: "SEA Roles | Vitarish",
  description: "Manage SEA role definitions.",
};

export default function SeaSettingsUsersRoute() {
  return <SeaRolesPage />;
}
