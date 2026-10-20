import { StandaloneCenteredPage } from "@vitarish/ui/layout";
import { BrandedPageFrame } from "@/components/branding/BrandedPageFrame";

/** The handoff runs before any session exists on this host, so it has no clinic shell. */
export default function HandoffLayout({ children }: { children: React.ReactNode }) {
  return (
    <BrandedPageFrame>
      <StandaloneCenteredPage width="reading">{children}</StandaloneCenteredPage>
    </BrandedPageFrame>
  );
}
