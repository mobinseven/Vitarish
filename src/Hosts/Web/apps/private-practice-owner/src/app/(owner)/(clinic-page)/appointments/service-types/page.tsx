import { LegacyPaths, redirectLegacyRoute } from "@/app/legacyRoutes";

/** The retired address of a page that moved; see legacyRoutes. */
export default function LegacyServiceTypesRoute(): never {
  return redirectLegacyRoute(LegacyPaths.ServiceTypes);
}
