import { LoadingSpinner } from "@/components/loading/LoadingSpinner";
import { StandaloneCenteredPage } from "@vitarish/web-ui/layout";

export default function Loading() {
  return (
    <StandaloneCenteredPage className="bg-background">
      <LoadingSpinner />
    </StandaloneCenteredPage>
  );
}
