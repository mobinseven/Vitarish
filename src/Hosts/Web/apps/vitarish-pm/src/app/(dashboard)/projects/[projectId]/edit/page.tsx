import { SeaRoutePage } from "@/components/SeaRoutePage";
export default async function Page({
  params,
}: {
  params: Promise<{ projectId: string }>;
}) {
  return (
    <SeaRoutePage page="projectEdit" projectId={(await params).projectId} />
  );
}
