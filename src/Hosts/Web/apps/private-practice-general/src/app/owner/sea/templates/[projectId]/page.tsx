"use client";

import { use } from "react";
import { SeaTemplateProjectDetailPage } from "@vitarish/sea-web";

export default function SeaTemplateDetailRoute({
  params,
}: {
  params: Promise<{ projectId: string }>;
}) {
  const { projectId } = use(params);
  return <SeaTemplateProjectDetailPage projectId={projectId} />;
}
