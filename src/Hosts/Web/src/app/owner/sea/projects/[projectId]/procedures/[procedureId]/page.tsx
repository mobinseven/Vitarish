"use client";

import { use } from "react";
import { SeaProcedureDetailPage } from "@/features/sea/procedures/components/SeaProcedureDetailPage";

export default function SeaProcedureDetailRoute({
  params,
}: {
  params: Promise<{ projectId: string; procedureId: string }>;
}) {
  const { projectId, procedureId } = use(params);
  return <SeaProcedureDetailPage projectId={projectId} procedureId={procedureId} />;
}
