import { TenantHandoffPage } from "@vitarish/journey";
import { Routes } from "@/app/routes";

export default function HandoffRoute() {
  return <TenantHandoffPage signInPath={Routes.Login.Index} />;
}
