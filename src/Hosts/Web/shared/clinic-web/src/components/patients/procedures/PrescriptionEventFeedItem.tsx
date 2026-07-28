"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Chip, Tooltip } from "@heroui/react";
import { ItemCard, PressableFeedback, Sheet } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import { medicalGetPrescription } from "../../../api/generated/medical/medical";
import type {
  PrescriptionItemDto,
  PrescriptionSummaryDto,
} from "../../../api/generated/medical/model";
import { formatDateTime, formatNumber, useLocalizedText } from "../../../localization";
import type { ProcedureEventPluginItem } from "./eventFeedPlugins";

const statusKeys = [
  "draft",
  "readyForSubmission",
  "submitting",
  "submitted",
  "acceptedByProvider",
  "rejectedByProvider",
  "needsCorrection",
  "editRequested",
  "edited",
  "cancelRequested",
  "cancelled",
  "paperFallback",
  "syncPending",
  "syncFailed",
  "duplicateRisk",
  "providerUnavailable",
  "manuallyReconciled",
] as const;

function statusColor(status?: number) {
  if (status === 4 || status === 16) return "success" as const;
  if ([5, 7, 9, 10, 13, 14, 15].includes(status ?? -1))
    return "danger" as const;
  if ([2, 6, 12].includes(status ?? -1)) return "warning" as const;
  return "default" as const;
}

function PrescriptionItemRow({ item }: { item: PrescriptionItemDto }) {
  const { t, locale } = useLocalizedText();
  const categoryKeys = ["drug", "lab", "imaging", "service", "referral"] as const;
  const categoryKey = categoryKeys[item.category ?? -1];
  const metadata = [
    item.quantity != null
      ? t("clinic.prescriptionFeed.quantity", {
          value: formatNumber(Number(item.quantity), locale),
        })
      : null,
    item.durationDays != null
      ? t("clinic.prescriptionFeed.duration", {
          value: formatNumber(Number(item.durationDays), locale),
        })
      : null,
    item.referenceCode
      ? t("clinic.prescriptionFeed.referenceCode", { value: item.referenceCode })
      : null,
  ].filter(Boolean);

  return (
    <ItemCard variant="secondary">
      <ItemCard.Content className="min-w-0">
        <ItemCard.Title className="whitespace-normal break-words">
          {item.displayName || t("clinic.prescriptionFeed.unnamedItem")}
        </ItemCard.Title>
        <ItemCard.Description className="whitespace-normal break-words">
          {categoryKey
            ? t(`clinic.prescriptionFeed.categories.${categoryKey}`)
            : t("clinic.prescriptionFeed.categories.unknown")}
          {metadata.length ? ` · ${metadata.join(" · ")}` : ""}
          {item.dosageJson ? ` · ${item.dosageJson}` : ""}
        </ItemCard.Description>
      </ItemCard.Content>
    </ItemCard>
  );
}

export function PrescriptionEventFeedItem({
  item,
}: {
  item: ProcedureEventPluginItem<PrescriptionSummaryDto>;
}) {
  const { t, locale } = useLocalizedText();
  const [isOpen, setIsOpen] = useState(false);
  const prescription = item.payload;
  const statusKey = statusKeys[prescription.status ?? -1] ?? "unknown";
  const detail = useQuery({
    queryKey: ["medical", "prescription", prescription.id],
    enabled: isOpen && Boolean(prescription.id),
    queryFn: ({ signal }) =>
      medicalGetPrescription(prescription.id!, undefined, signal),
    retry: false,
  });
  const metadata = [
    prescription.selectedProviderKey
      ? t("clinic.prescriptionFeed.provider", {
          value: prescription.selectedProviderKey,
        })
      : null,
    prescription.trackingCode
      ? t("clinic.prescriptionFeed.tracking", {
          value: prescription.trackingCode,
        })
      : null,
    formatDateTime(item.createdAt, locale),
  ].filter(Boolean);

  return (
    <>
      <Tooltip>
        <ItemCard<"button">
          aria-label={t("clinic.prescriptionFeed.openDetails")}
          className="relative w-full cursor-pointer overflow-hidden text-start"
          render={(props) => <button type="button" {...props} />}
          onClick={() => setIsOpen(true)}
        >
          <PressableFeedback.Highlight />
          <ItemCard.Icon>
            <FaIcon icon="prescription-bottle-medical" aria-hidden="true" />
          </ItemCard.Icon>
          <ItemCard.Content className="min-w-0">
            <ItemCard.Title className="flex flex-wrap items-center gap-2 whitespace-normal">
              {t("clinic.prescriptionFeed.title")}
              <Chip
                color={statusColor(prescription.status)}
                size="sm"
                variant="soft"
              >
                {t(`clinic.prescriptionFeed.statuses.${statusKey}`)}
              </Chip>
            </ItemCard.Title>
            <ItemCard.Description className="whitespace-normal break-words">
              {metadata.join(" · ")}
              {prescription.itemDisplayNames?.length
                ? ` — ${prescription.itemDisplayNames.join("، ")}`
                : ""}
            </ItemCard.Description>
          </ItemCard.Content>
          <ItemCard.Action>
            <FaIcon
              icon="chevron-right"
              aria-hidden="true"
              className="text-muted rtl:rotate-180"
            />
          </ItemCard.Action>
        </ItemCard>
        <Tooltip.Content>{t("clinic.prescriptionFeed.openDetails")}</Tooltip.Content>
      </Tooltip>

      <Sheet isOpen={isOpen} onOpenChange={setIsOpen}>
        <Sheet.Backdrop>
          <Sheet.Content className="max-w-xl">
            <Sheet.Dialog className="flex max-h-[92vh] flex-col">
              <Sheet.Header>
                <Sheet.Heading>{t("clinic.prescriptionFeed.detailsTitle")}</Sheet.Heading>
                <Sheet.CloseTrigger aria-label={t("common.actions.close")}>
                  <FaIcon icon="xmark" aria-hidden="true" />
                </Sheet.CloseTrigger>
              </Sheet.Header>
              <Sheet.Body className="min-h-0 space-y-3 overflow-y-auto">
                {detail.isLoading ? (
                  <p className="text-sm text-muted">{t("common.states.loading")}</p>
                ) : detail.error ? (
                  <p className="text-sm text-danger" role="alert">
                    {t("clinic.prescriptionFeed.unavailable")}
                  </p>
                ) : detail.data ? (
                  <>
                    <p className="text-sm text-muted">
                      {metadata.join(" · ")}
                    </p>
                    {(detail.data.items ?? [])
                      .slice()
                      .sort(
                        (left, right) =>
                          Number(left.sortOrder ?? 0) -
                          Number(right.sortOrder ?? 0),
                      )
                      .map((prescriptionItem, index) => (
                        <PrescriptionItemRow
                          key={prescriptionItem.id ?? `${prescription.id}:${index}`}
                          item={prescriptionItem}
                        />
                      ))}
                    {!detail.data.items?.length ? (
                      <p className="text-sm text-muted">
                        {t("clinic.prescriptionFeed.noItems")}
                      </p>
                    ) : null}
                  </>
                ) : (
                  <p className="text-sm text-muted">
                    {t("clinic.prescriptionFeed.notFound")}
                  </p>
                )}
              </Sheet.Body>
            </Sheet.Dialog>
          </Sheet.Content>
        </Sheet.Backdrop>
      </Sheet>
    </>
  );
}
