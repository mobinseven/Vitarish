import { LandingFooter } from "@/features/landing/LandingFooter";
import { LandingHero } from "@/features/landing/LandingHero";
import { LandingNavbar } from "@/features/landing/LandingNavbar";
import { PracticePrinciplesShowcase } from "@/features/landing/PracticePrinciplesShowcase";

export default function RootPage() {
  return (
    <div className="min-h-dvh bg-background">
      <LandingNavbar />
      <main aria-label="Main content">
        <LandingHero />
        <PracticePrinciplesShowcase />
        <LandingFooter />
      </main>
    </div>
  );
}
