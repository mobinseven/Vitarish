import { useQuery } from "@tanstack/react-query";
import { seaGetStatistics } from "../../../../api/generated/sea/sea";
import { mapSeaStatistics } from "../../shared/models/seaReadDtoMappers";
import type { SeaStatisticsDto } from "../../shared/models/seaStatModels";

async function getSeaStatistics(): Promise<SeaStatisticsDto> {
  return mapSeaStatistics(await seaGetStatistics());
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
