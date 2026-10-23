import { clinicSpecialtyMessagePrefix } from "@vitarish/clinic/localization";
import { TenantSetupPage } from "@vitarish/journey";
import { Routes } from "@/app/routes";

export default function SetupRoute() {
  return <TenantSetupPage categoryMessagePrefix={clinicSpecialtyMessagePrefix} handoffPath={Routes.Handoff} signInPath={Routes.Login.Index} />;
}
