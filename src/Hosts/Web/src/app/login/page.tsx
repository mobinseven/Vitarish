import { Card, CardBody, CardHeader } from "@heroui/react";
import { AuthMethodSelector } from "@/components/auth/AuthMethodSelector";

export default function LoginPage() {
  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="flex flex-col items-center gap-2 pb-2">
          <h1 className="text-2xl font-bold">Vitarish</h1>
          <p className="text-sm text-default-500">
            Sign in to your account
          </p>
        </CardHeader>
        <CardBody>
          <AuthMethodSelector />
        </CardBody>
      </Card>
    </div>
  );
}
