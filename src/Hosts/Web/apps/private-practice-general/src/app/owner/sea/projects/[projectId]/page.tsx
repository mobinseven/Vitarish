"use client";

import { use } from "react";
import { SeaProjectDetailPage } from "@vitarish/sea-web";

export default function SeaProjectDetailRoute({
  params,
}: {
  params: Promise<{ projectId: string }>;
}) {
  const { projectId } = use(params);
  return <SeaProjectDetailPage projectId={projectId} />;
}
