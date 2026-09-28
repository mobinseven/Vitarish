import { PasswordRecoveryPage } from "@vitarish/auth";
import { Routes } from "@/app/routes";

export default function ForgotPasswordPage() {
  return <PasswordRecoveryPage loginHref={Routes.LoginPassword} />;
}
