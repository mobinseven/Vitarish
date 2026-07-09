"use client";

import { use } from "react";
import { SeaTemplateProjectDetailPage } from "@/features/sea/templates/components";

export default function SeaTemplateDetailRoute({
  params,
}: {
  params: Promise<{ projectId: string }>;
}) {
  const { projectId } = use(params);
  return <SeaTemplateProjectDetailPage projectId={projectId} />;
}
