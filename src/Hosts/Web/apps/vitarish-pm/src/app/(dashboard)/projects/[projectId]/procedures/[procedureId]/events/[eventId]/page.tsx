import { SeaRoutePage } from "@/components/SeaRoutePage";
export default async function Page({
  params,
}: {
  params: Promise<{ projectId: string; procedureId: string; eventId: string }>;
}) {
  const value = await params;
  return (
    <SeaRoutePage
      page="event"
      projectId={value.projectId}
      procedureId={value.procedureId}
      eventId={value.eventId}
    />
  );
}
