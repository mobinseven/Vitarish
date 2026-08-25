import { useQuery } from "@tanstack/react-query";
import { seaGetStatistics } from "../../api/generated/sea/sea";
import { mapSeaStatistics } from "../../mappers/seaReadDtoMappers";
import type { SeaStatisticsDto } from "../../models/seaStatModels";

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
