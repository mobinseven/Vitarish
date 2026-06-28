"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuthSession } from "@/hooks/auth/useAuthSession";

export default function AccountLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { isAuthenticated, isLoading } = useAuthSession();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push("/login");
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <p className="text-default-500">Loading...</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <div className="flex min-h-screen">
      <aside className="w-64 border-r border-divider p-4">
        <h2 className="text-lg font-bold mb-4">Account</h2>
        <nav className="flex flex-col gap-2">
          <Link href="/account" className="text-sm hover:text-primary">Settings</Link>
          <Link href="/account/name" className="text-sm hover:text-primary">Name</Link>
          <Link href="/account/email" className="text-sm hover:text-primary">Email</Link>
          <Link href="/account/phone" className="text-sm hover:text-primary">Phone</Link>
          <Link href="/account/password" className="text-sm hover:text-primary">Password</Link>
          <Link href="/account/passkeys" className="text-sm hover:text-primary">Passkeys</Link>
          <Link href="/account/theme" className="text-sm hover:text-primary">Theme</Link>
          <Link href="/account/culture" className="text-sm hover:text-primary">Language</Link>
        </nav>
      </aside>
      <main className="flex-1 p-8">{children}</main>
    </div>
  );
}
