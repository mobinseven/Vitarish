"use client";

import { use } from "react";
import { SeaProcedureDetailPage } from "@vitarish/sea-web";

export default function SeaProcedureDetailRoute({
  params,
}: {
  params: Promise<{ projectId: string; procedureId: string }>;
}) {
  const { projectId, procedureId } = use(params);
  return <SeaProcedureDetailPage projectId={projectId} procedureId={procedureId} />;
}
