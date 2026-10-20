import { StandaloneCenteredPage } from "@vitarish/ui/layout";

/** The handoff runs before any session exists on this host, so it has no clinic shell. */
export default function HandoffLayout({ children }: { children: React.ReactNode }) {
  return <StandaloneCenteredPage width="reading">{children}</StandaloneCenteredPage>;
}
