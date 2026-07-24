import { LoadingSpinner } from "@/components/loading/LoadingSpinner";

export default function Loading() {
  return (
    <main className="flex min-h-dvh items-center justify-center bg-background">
      <LoadingSpinner />
    </main>
  );
}
