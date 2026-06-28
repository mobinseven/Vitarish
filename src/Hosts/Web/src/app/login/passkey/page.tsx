import { LoginPageCard } from "@/components/auth/LoginPageCard";
import { PasskeyButton } from "@/components/auth/PasskeyButton";

export default function PasskeyLoginPage() {
  return (
    <LoginPageCard
      title="Passkey Sign In"
      subtitle="Use your passkey to sign in securely"
      skeletonHeight="128px"
    >
      <PasskeyButton />
    </LoginPageCard>
  );
}
