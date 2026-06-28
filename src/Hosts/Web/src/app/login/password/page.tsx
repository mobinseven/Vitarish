import { Card, CardBody, CardHeader, Link } from "@heroui/react";
import { LoginForm } from "@/components/auth/LoginForm";

export default function PasswordLoginPage() {
  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="flex flex-col items-center gap-2 pb-2">
          <h1 className="text-2xl font-bold">Sign In</h1>
          <p className="text-sm text-default-500">
            Enter your credentials
          </p>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          <LoginForm />
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
