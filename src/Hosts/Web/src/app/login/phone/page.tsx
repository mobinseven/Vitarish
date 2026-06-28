import { LoginPageCard } from "@/components/auth/LoginPageCard";
import { PhoneOtpForm } from "@/components/auth/PhoneOtpForm";

export default function PhoneLoginPage() {
  return (
    <LoginPageCard
      title="Phone Sign In"
      subtitle="Enter your phone number to receive a code"
      skeletonHeight="112px"
    >
      <PhoneOtpForm />
    </LoginPageCard>
  );
}
