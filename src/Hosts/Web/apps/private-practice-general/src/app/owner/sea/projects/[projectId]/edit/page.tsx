"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Routes } from "@/app/routes";
import { SeaProjectForm } from "@vitarish/sea-web";
import { useSeaProjectDetail } from "@vitarish/sea-web";
import { SeaLoadingState, SeaErrorState } from "@vitarish/sea-web";
import { useLocalizedText } from "@/localization";

export default function SeaProjectEditRoute({
  params,
}: {
  params: Promise<{ projectId: string }>;
}) {
  const { projectId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: project, isLoading, error } = useSeaProjectDetail(projectId);

  if (isLoading) {
    return <SeaLoadingState label={t("sea.projects.loading")} />;
  }

  if (error || !project) {
    return <SeaErrorState title={t("sea.projects.notFound")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button
          variant="ghost"
          className="mb-2"
          onPress={() => router.push(Routes.Sea.Projects.Detail(projectId))}
        >
          {t("sea.projects.backToProjects")}
        </Button>
        <h1 className="text-2xl font-bold">{t("sea.projects.editTitle")}</h1>
      </div>
      <SeaProjectForm
        isOpen={true}
        onClose={() => router.push(Routes.Sea.Projects.Detail(projectId))}
        project={project}
      />
    </div>
  );
}
