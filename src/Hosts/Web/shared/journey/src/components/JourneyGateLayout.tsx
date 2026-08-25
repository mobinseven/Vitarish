import { CenteredRegion } from "@vitarish/ui/layout";

interface JourneyGateLayoutProps {
  children: React.ReactNode;
}

export function JourneyGateLayout({ children }: JourneyGateLayoutProps) {
  return (
    <CenteredRegion>
      <section className="w-full max-w-2xl" aria-live="polite">
        {children}
      </section>
    </CenteredRegion>
  );
}
