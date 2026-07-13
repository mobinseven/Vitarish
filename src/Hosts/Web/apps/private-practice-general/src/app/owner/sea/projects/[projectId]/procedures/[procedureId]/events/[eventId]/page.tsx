"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { useSeaEventDetail } from "@vitarish/sea-web";
import { SeaLoadingState, SeaErrorState } from "@vitarish/sea-web";
import { DetailGrid, type DetailGridItem } from "@vitarish/web-ui";
import { useLocalizedText, formatDateTime } from "@/localization";

export default function SeaEventDetailRoute({
  params,
}: {
  params: Promise<{ projectId: string; procedureId: string; eventId: string }>;
}) {
  const { projectId, procedureId, eventId } = use(params);
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const { data: event, isLoading, error } = useSeaEventDetail(eventId);

  if (isLoading) {
    return <SeaLoadingState label={t("sea.events.loading")} />;
  }

  if (error || !event) {
    return <SeaErrorState title={t("sea.events.notFound")} />;
  }

  const details: DetailGridItem[] = [
    { label: t("sea.events.title_label"), value: event.title },
    ...(event.description ? [{ label: t("sea.events.description"), value: event.description }] : []),
    { label: t("sea.events.status"), value: event.isCommitted ? t("sea.events.committed") : t("sea.events.draft") },
    { label: t("sea.projects.createdAt"), value: formatDateTime(event.createdAt, locale) },
    ...(event.committedAt ? [{ label: t("sea.events.committedAt"), value: formatDateTime(event.committedAt, locale) }] : []),
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button
          variant="ghost"
          className="mb-2"
          onPress={() => router.push(Routes.Sea.Procedures.Detail(projectId, procedureId))}
        >
          {t("sea.events.backToProcedure")}
        </Button>
        <h1 className="text-2xl font-bold">{event.title}</h1>
      </div>

      <Widget>
        <Widget.Content>
          <DetailGrid items={details} />
        </Widget.Content>
      </Widget>
    </div>
  );
}
