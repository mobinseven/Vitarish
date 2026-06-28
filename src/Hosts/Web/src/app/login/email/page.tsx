import { LoginPageCard } from "@/components/auth/LoginPageCard";
import { EmailOtpForm } from "@/components/auth/EmailOtpForm";

export default function EmailLoginPage() {
  return (
    <LoginPageCard
      title="Email Sign In"
      subtitle="Enter your email to receive a code"
      skeletonHeight="112px"
    >
      <EmailOtpForm />
    </LoginPageCard>
  );
}
