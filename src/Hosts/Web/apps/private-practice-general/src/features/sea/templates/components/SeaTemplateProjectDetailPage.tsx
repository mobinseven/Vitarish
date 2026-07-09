"use client";

import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Routes } from "@/app/routes";
import { SeaLoadingState, SeaErrorState } from "../../shared/components/SeaStateSurface";
import { useSeaTemplateProjectDetail } from "../hooks/useSeaTemplates";
import { useLocalizedText, formatDateTime } from "@/localization";
import { ClinicDetailGrid, type ClinicDetailItem } from "@/features/clinic/components/ClinicDetailGrid";
import { Widget } from "@heroui-pro/react";

interface SeaTemplateProjectDetailPageProps {
  projectId: string;
}

export function SeaTemplateProjectDetailPage({ projectId }: SeaTemplateProjectDetailPageProps) {
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const { data: project, isLoading, error } = useSeaTemplateProjectDetail(projectId);

  if (isLoading) {
    return <SeaLoadingState label={t("sea.templates.loading")} />;
  }

  if (error || !project) {
    return <SeaErrorState title={t("sea.templates.notFound")} />;
  }

  const details: ClinicDetailItem[] = [
    { label: t("sea.projects.name"), value: project.name },
    ...(project.description ? [{ label: t("sea.projects.description"), value: project.description }] : []),
    { label: t("sea.projects.createdAt"), value: formatDateTime(project.createdAt, locale) },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button
          variant="ghost"
          className="mb-2"
          onPress={() => router.push(Routes.Sea.Templates.Index)}
        >
          {t("sea.templates.backToTemplates")}
        </Button>
        <h1 className="text-2xl font-bold">{project.name}</h1>
      </div>

      <Widget>
        <Widget.Content>
          <ClinicDetailGrid items={details} />
        </Widget.Content>
      </Widget>
    </div>
  );
}
