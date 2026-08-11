import { SeaRoutePage } from "@/components/SeaRoutePage";
export default async function Page({
  params,
  searchParams,
}: {
  params: Promise<{ projectId: string; procedureId: string }>;
  searchParams: Promise<{ event?: string }>;
}) {
  const value = await params;
  const query = await searchParams;
  return (
    <SeaRoutePage
      page="procedure"
      projectId={value.projectId}
      procedureId={value.procedureId}
      focusedEventId={query.event}
    />
  );
}
