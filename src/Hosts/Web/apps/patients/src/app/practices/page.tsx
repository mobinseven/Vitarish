import { Routes } from "@/app/routes";
import Link from "next/link";

export default function PracticesPage() {
  return (
    <main className="mx-auto max-w-2xl p-6">
      <h1 className="text-2xl font-semibold">Practices</h1>
      <p className="mt-3 text-muted">
        Choose a practice to start registration.
      </p>
      <Link
        className="mt-6 inline-block rounded-medium border p-4"
        href={Routes.Login.Index}
      >
        Sign in to continue
      </Link>
    </main>
  );
}
