import type { NavigationIntent } from "../../navigation";
import { StatisticsDashboard } from "./StatisticsDashboard";

export function StatisticsPage({ onNavigate }: { onNavigate?: (intent: NavigationIntent) => void }) {
  return <StatisticsDashboard onNavigate={onNavigate} />;
}
