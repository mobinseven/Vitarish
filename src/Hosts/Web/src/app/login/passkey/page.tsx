import { Suspense } from "react";
import { Card, CardBody, CardHeader, Link } from "@heroui/react";
import { PasskeyButton } from "@/components/auth/PasskeyButton";

export default function PasskeyLoginPage() {
  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="flex flex-col items-center gap-2 pb-2">
          <h1 className="text-2xl font-bold">Passkey Sign In</h1>
          <p className="text-sm text-default-500">
            Use your passkey to sign in securely
          </p>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          <Suspense fallback={<div className="h-[128px] w-full animate-pulse rounded-xl bg-default-100" />}>
            <PasskeyButton />
          </Suspense>
          <div className="flex flex-col items-center gap-2 text-sm">
            <Link href="/login" className="text-sm">
              Other sign-in methods
            </Link>
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
