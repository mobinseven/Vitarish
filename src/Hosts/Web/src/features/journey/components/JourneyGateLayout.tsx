interface JourneyGateLayoutProps {
  children: React.ReactNode;
}

export function JourneyGateLayout({ children }: JourneyGateLayoutProps) {
  return (
    <main className="flex min-h-screen items-center justify-center px-4 py-10 sm:px-6 lg:px-8">
      <section className="w-full max-w-2xl" aria-live="polite">
        {children}
      </section>
    </main>
  );
}
