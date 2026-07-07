import { useQuery } from "@tanstack/react-query";
import { seaGetStatistics } from "@/api/generated/sea/sea";
import type { DatabaseStatisticsReadDto as SeaStatisticsDto } from "@/api/generated/sea/model";

async function getSeaStatistics(): Promise<SeaStatisticsDto> {
  return seaGetStatistics();
}

const seaStatisticsKeys = {
  all: ["sea", "statistics"] as const,
};

export function useSeaStatistics() {
  return useQuery({
    queryKey: seaStatisticsKeys.all,
    queryFn: getSeaStatistics,
  });
}
