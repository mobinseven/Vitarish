"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
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
          <a href="/account" className="text-sm hover:text-primary">Settings</a>
          <a href="/account/name" className="text-sm hover:text-primary">Name</a>
          <a href="/account/email" className="text-sm hover:text-primary">Email</a>
          <a href="/account/phone" className="text-sm hover:text-primary">Phone</a>
          <a href="/account/password" className="text-sm hover:text-primary">Password</a>
          <a href="/account/passkeys" className="text-sm hover:text-primary">Passkeys</a>
          <a href="/account/theme" className="text-sm hover:text-primary">Theme</a>
          <a href="/account/culture" className="text-sm hover:text-primary">Language</a>
        </nav>
      </aside>
      <main className="flex-1 p-8">{children}</main>
    </div>
  );
}
