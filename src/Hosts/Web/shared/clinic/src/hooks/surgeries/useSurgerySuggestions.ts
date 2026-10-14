import { useQuery } from "@tanstack/react-query";
import { clinicGetSurgerySuggestions } from "../../api/generated/clinic/clinic";
import { mapSurgerySuggestions } from "../../models/surgeries/surgeryMappers";
import { surgeryKeys } from "./surgeryKeys";

export function useSurgerySuggestions() {
  return useQuery({
    queryKey: surgeryKeys.suggestions(),
    queryFn: async () =>
      mapSurgerySuggestions(await clinicGetSurgerySuggestions()),
  });
}
