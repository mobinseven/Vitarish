import { TenantSetupPage } from "@vitarish/journey";
import { Routes } from "@/app/routes";

export default function SetupRoute() {
  return <TenantSetupPage handoffPath={Routes.Handoff} signInPath={Routes.Login.Index} />;
}
