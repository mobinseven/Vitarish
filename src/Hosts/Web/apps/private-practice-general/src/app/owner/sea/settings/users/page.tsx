import type { Metadata } from "next";
import { SeaRolesPage } from "@vitarish/sea-web";

export const metadata: Metadata = {
  title: "SEA Roles | Vitarish",
  description: "Manage SEA role definitions.",
};

export default function SeaSettingsUsersRoute() {
  return <SeaRolesPage />;
}
