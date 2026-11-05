// TypeScript mirror of ContentsApiRoutes and ContentsPublicDelivery.
import { TENANT_HOST_HEADER } from "@vitarish/api-client/tenant-host-header";

export const contentsApiBaseRoute = "/api/contents";

export const contentsPublicDeliveryRoutes = {
  base: `${contentsApiBaseRoute}/public`,
  site: "/sites/{siteKey}",
  entry: "/sites/{siteKey}/entries",
  entries: "/sites/{siteKey}/types/{typeKey}/entries",
} as const;

export const contentsTenantHostHeader = TENANT_HOST_HEADER;

export const publicEntryPathQueryKey = "path";
export const publishedEntriesQueryKeys = { skip: "skip", take: "take" } as const;
