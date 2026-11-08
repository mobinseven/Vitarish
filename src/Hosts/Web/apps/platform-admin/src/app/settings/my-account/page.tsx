import { CentralAuthenticationLayout } from "@vitarish/auth/central-server";
import { AuthenticationWebRoutes } from "@vitarish/auth/routes";
import AccountDetails from "./AccountDetails";
export default function MyAccountPage() {
  return <CentralAuthenticationLayout securityPath={AuthenticationWebRoutes.Account}><AccountDetails /></CentralAuthenticationLayout>;
}
