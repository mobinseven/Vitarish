"use client";
import { DetailGrid, type DetailGridItem } from "@vitarish/web-ui";

import { Button } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import type { SeaNavigationIntent } from "../../navigation";
import { SeaLoadingState, SeaErrorState } from "../state/SeaStateSurface";
import { useSeaTemplateProjectDetail } from "../../hooks/templates/useSeaTemplates";
import { useLocalizedText, formatDateTime } from "../../localization";

interface SeaTemplateProjectDetailPageProps {
  projectId: string;
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

export function SeaTemplateProjectDetailPage({
  projectId,
  onNavigate,
}: SeaTemplateProjectDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const {
    data: project,
    isLoading,
    error,
  } = useSeaTemplateProjectDetail(projectId);

  if (isLoading) {
    return <SeaLoadingState label={t("sea.templates.loading")} />;
  }

  if (error || !project) {
    return <SeaErrorState title={t("sea.templates.notFound")} />;
  }

  const details: DetailGridItem[] = [
    { label: t("sea.projects.name"), value: project.name },
    ...(project.description
      ? [{ label: t("sea.projects.description"), value: project.description }]
      : []),
    {
      label: t("sea.projects.createdAt"),
      value: formatDateTime(project.createdAt, locale),
    },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button
          variant="ghost"
          className="mb-2"
          onPress={() => onNavigate?.({ kind: "templateList" })}
        >
          {t("sea.templates.backToTemplates")}
        </Button>
        <h1 className="text-2xl font-bold">{project.name}</h1>
      </div>

      <Widget>
        <Widget.Content>
          <DetailGrid items={details} />
        </Widget.Content>
      </Widget>
    </div>
  );
}
