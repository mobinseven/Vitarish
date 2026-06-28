import { LoginPageCard } from "@/components/auth/LoginPageCard";
import { LoginForm } from "@/components/auth/LoginForm";

export default function PasswordLoginPage() {
  return (
    <LoginPageCard
      title="Sign In"
      subtitle="Enter your credentials"
      skeletonHeight="184px"
    >
      <LoginForm />
    </LoginPageCard>
  );
}
