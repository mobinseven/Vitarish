import { Card, CardBody, CardHeader, Link } from "@heroui/react";
import { PhoneOtpForm } from "@/components/auth/PhoneOtpForm";

export default function PhoneLoginPage() {
  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="flex flex-col items-center gap-2 pb-2">
          <h1 className="text-2xl font-bold">Phone Sign In</h1>
          <p className="text-sm text-default-500">
            Enter your phone number to receive a code
          </p>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          <PhoneOtpForm />
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
