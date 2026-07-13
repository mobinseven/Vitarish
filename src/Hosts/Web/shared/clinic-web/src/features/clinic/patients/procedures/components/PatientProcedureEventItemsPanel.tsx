"use client";

import { ListView } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import { usePatientProcedureEventItems } from "../hooks/usePatientProcedureEventItems";
import { useLocalizedText, formatDateTime } from "../../../../../localization";
import type { EventItemSummary } from "../models/PatientProcedureEventItemModels";

interface PatientProcedureEventItemsPanelProps {
  eventId: string;
}

const itemTypeLabels: Record<string, string> = {
  note: "clinic.procedures.items.note",
  file: "clinic.procedures.items.file",
  media: "clinic.procedures.items.media",
  reminder: "clinic.procedures.items.reminder",
  audit: "clinic.procedures.items.audit",
};

export function PatientProcedureEventItemsPanel({ eventId }: PatientProcedureEventItemsPanelProps) {
  const { t, locale } = useLocalizedText();
  const { data: items, isLoading } = usePatientProcedureEventItems(eventId);

  if (isLoading) return null;

  return (
    <section className="rounded-medium border border-divider p-4">
      <header className="mb-3">
        <h2>{t("clinic.procedures.items.title")}</h2>
      </header>
      <div>
        {!items || items.length === 0 ? (
          <ClinicEmptyState title={t("common.states.empty")} />
        ) : (
          <ListView
            aria-label={t("clinic.procedures.items.title")}
            variant="secondary"
          >
            {items.map((item: EventItemSummary) => (
              <ListView.Item key={item.id} id={item.id} textValue={item.content}>
                <ListView.ItemContent>
                  <div className="flex flex-col gap-1">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-medium uppercase text-muted">
                        {t(itemTypeLabels[item.type] ?? "clinic.procedures.items.note")}
                      </span>
                      {item.author && (
                        <span className="text-xs text-muted">Ã¢â‚¬â€ {item.author}</span>
                      )}
                    </div>
                    <ListView.Description className="line-clamp-2">
                      {item.content}
                    </ListView.Description>
                    <span className="text-xs text-muted">
                      {formatDateTime(item.createdAt, locale)}
                    </span>
                  </div>
                </ListView.ItemContent>
              </ListView.Item>
            ))}
          </ListView>
        )}
      </div>
    </section>
  );
}
