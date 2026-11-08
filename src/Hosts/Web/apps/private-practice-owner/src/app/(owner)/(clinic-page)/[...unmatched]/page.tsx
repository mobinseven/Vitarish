import { redirectLegacyRoute } from "@/app/legacyRoutes";

/** Redirects retired clinic URLs to their new place; any other unknown URL gets the in-shell not-found page. */
export default async function UnmatchedClinicRoute({
  params,
}: {
  params: Promise<{ unmatched: string[] }>;
}): Promise<never> {
  const { unmatched } = await params;
  return redirectLegacyRoute(`/${unmatched.join("/")}`);
}
