import { SettingsUsersPage } from "@vitarish/clinic";
import { SettingsChildPage } from "@/components/settings/SettingsChildPage";

export default function SettingsUsersRoute() {
  return (
    <SettingsChildPage>
      <SettingsUsersPage />
    </SettingsChildPage>
  );
}
