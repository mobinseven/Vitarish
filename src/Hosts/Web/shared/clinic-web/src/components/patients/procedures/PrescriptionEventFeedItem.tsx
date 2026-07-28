"use client";

import { useEffect, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Button,
  Chip,
  Input,
  Label,
  ListBox,
  SearchField,
  TextArea,
  TextField,
  Tooltip,
} from "@heroui/react";
import { ItemCard, PressableFeedback, Sheet } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import {
  medicalEditPrescription,
  medicalGetPrescription,
  medicalSearchPrescriptionReferenceCodes,
} from "../../../api/generated/medical/medical";
import type {
  PrescriptionItemDto,
  PrescriptionSummaryDto,
} from "../../../api/generated/medical/model";
import {
  formatDateTime,
  formatNumber,
  useLocalizedText,
} from "../../../localization";
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

export function appendPrescriptionEditItem(
  items: PrescriptionItemDto[],
  result: { category: number; referenceCode: string; displayName: string },
  id: string,
): PrescriptionItemDto[] {
  if (items.some((item) => item.referenceCode === result.referenceCode))
    return items;
  return [
    ...items,
    {
      id,
      category: result.category,
      referenceCode: result.referenceCode,
      displayName: result.displayName,
      quantity: 1,
      durationDays: null,
      dosageJson: null,
      sortOrder: items.length + 1,
    },
  ];
}

function PrescriptionItemRow({ item }: { item: PrescriptionItemDto }) {
  const { t, locale } = useLocalizedText();
  const categoryKeys = [
    "drug",
    "lab",
    "imaging",
    "service",
    "referral",
  ] as const;
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
      ? t("clinic.prescriptionFeed.referenceCode", {
          value: item.referenceCode,
        })
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
  const [isEditing, setIsEditing] = useState(false);
  const [editReason, setEditReason] = useState("");
  const [editItems, setEditItems] = useState<PrescriptionItemDto[]>([]);
  const [editMessage, setEditMessage] = useState("");
  const [editSearch, setEditSearch] = useState("");
  const [editResults, setEditResults] = useState<
    Array<{ category: number; referenceCode: string; displayName: string }>
  >([]);
  const [isSaving, setIsSaving] = useState(false);
  const editSearchSequence = useRef(0);
  const prescription = item.payload;
  const statusKey = statusKeys[prescription.status ?? -1] ?? "unknown";
  const detail = useQuery({
    queryKey: ["medical", "prescription", prescription.id],
    enabled: isOpen && Boolean(prescription.id),
    queryFn: ({ signal }) =>
      medicalGetPrescription(prescription.id!, undefined, signal),
    retry: false,
  });
  useEffect(() => {
    if (detail.data && !isEditing)
      setEditItems((detail.data.items ?? []).map((value) => ({ ...value })));
  }, [detail.data, isEditing]);
  const canEdit =
    detail.data?.status === 4 &&
    detail.data.selectedProviderKey?.toLowerCase() === "tamin";
  async function searchEditItems(value: string) {
    const sequence = ++editSearchSequence.current;
    setEditSearch(value);
    setEditMessage("");
    if (value.trim().length < 2 || !detail.data?.selectedProviderKey) {
      setEditResults([]);
      return;
    }
    try {
      const results = await medicalSearchPrescriptionReferenceCodes({
        providerKey: detail.data.selectedProviderKey,
        textOrCode: value.trim(),
        activeOnly: true,
        limit: 20,
      });
      if (sequence !== editSearchSequence.current) return;
      setEditResults(
        results
          .filter(
            (result) =>
              result.referenceCode &&
              result.displayName &&
              !editItems.some(
                (item) => item.referenceCode === result.referenceCode,
              ),
          )
          .map((result) => ({
            category: result.prescriptionCategory ?? 0,
            referenceCode: result.referenceCode!,
            displayName: result.displayName!,
          })),
      );
    } catch {
      if (sequence !== editSearchSequence.current) return;
      setEditMessage(t("clinic.prescriptionFeed.searchFailed"));
    }
  }
  function addEditItem(result: {
    category: number;
    referenceCode: string;
    displayName: string;
  }) {
    if (editItems.some((item) => item.referenceCode === result.referenceCode)) {
      setEditMessage(t("clinic.prescriptionFeed.duplicateItem"));
      return;
    }
    setEditItems((current) =>
      appendPrescriptionEditItem(current, result, crypto.randomUUID()),
    );
    setEditSearch("");
    setEditResults([]);
  }
  async function saveEdit() {
    if (!detail.data?.id || !editReason.trim() || editItems.length === 0)
      return;
    setIsSaving(true);
    setEditMessage("");
    try {
      const result = await medicalEditPrescription(detail.data.id, {
        prescriptionId: detail.data.id,
        expectedVersion: detail.data.versionNumber ?? 0,
        idempotencyKey: crypto.randomUUID(),
        reason: editReason.trim(),
        items: editItems.map((value) => ({
          category: value.category ?? 0,
          referenceCode: value.referenceCode ?? null,
          displayName: value.displayName ?? null,
          dosageJson: value.dosageJson ?? null,
          quantity: Math.max(1, Number(value.quantity ?? 1)),
          durationDays: value.durationDays ?? null,
          metadataJson: null,
        })),
      });
      if (result.accepted) {
        setIsEditing(false);
        setEditReason("");
        await detail.refetch();
      } else
        setEditMessage(
          result.userSafeMessage ?? t("clinic.prescriptionFeed.editFailed"),
        );
    } catch {
      setEditMessage(t("clinic.prescriptionFeed.editFailed"));
    } finally {
      setIsSaving(false);
    }
  }
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
        <Tooltip.Content>
          {t("clinic.prescriptionFeed.openDetails")}
        </Tooltip.Content>
      </Tooltip>

      <Sheet isOpen={isOpen} onOpenChange={setIsOpen}>
        <Sheet.Backdrop>
          <Sheet.Content className="max-w-xl">
            <Sheet.Dialog className="flex max-h-[92vh] flex-col">
              <Sheet.Header>
                <Sheet.Heading>
                  {t("clinic.prescriptionFeed.detailsTitle")}
                </Sheet.Heading>
                <Sheet.CloseTrigger aria-label={t("common.actions.close")}>
                  <FaIcon icon="xmark" aria-hidden="true" />
                </Sheet.CloseTrigger>
              </Sheet.Header>
              <Sheet.Body className="min-h-0 space-y-3 overflow-y-auto">
                {detail.isLoading ? (
                  <p className="text-sm text-muted">
                    {t("common.states.loading")}
                  </p>
                ) : detail.error ? (
                  <p className="text-sm text-danger" role="alert">
                    {t("clinic.prescriptionFeed.unavailable")}
                  </p>
                ) : detail.data ? (
                  <>
                    <p className="text-sm text-muted">{metadata.join(" · ")}</p>
                    {(isEditing ? editItems : (detail.data.items ?? []))
                      .slice()
                      .sort(
                        (left, right) =>
                          Number(left.sortOrder ?? 0) -
                          Number(right.sortOrder ?? 0),
                      )
                      .map((prescriptionItem, index) =>
                        isEditing ? (
                          <div
                            key={prescriptionItem.id ?? index}
                            className="flex items-center gap-2 rounded-xl border border-separator p-3"
                          >
                            <div className="min-w-0 flex-1">
                              <p className="truncate text-sm font-medium">
                                {prescriptionItem.displayName}
                              </p>
                              <TextField
                                value={String(prescriptionItem.quantity ?? 1)}
                                onChange={(value) =>
                                  setEditItems((current) =>
                                    current.map((item) =>
                                      item.id === prescriptionItem.id
                                        ? {
                                            ...item,
                                            quantity: Math.max(
                                              1,
                                              Number(value) || 1,
                                            ),
                                          }
                                        : item,
                                    ),
                                  )
                                }
                              >
                                <Label>
                                  {t("clinic.prescriptionFeed.editQuantity")}
                                </Label>
                                <Input inputMode="numeric" />
                              </TextField>
                              <TextField
                                value={prescriptionItem.dosageJson ?? ""}
                                onChange={(value) =>
                                  setEditItems((current) =>
                                    current.map((item) =>
                                      item.id === prescriptionItem.id
                                        ? { ...item, dosageJson: value || null }
                                        : item,
                                    ),
                                  )
                                }
                              >
                                <Label>
                                  {t("clinic.prescriptionFeed.editDosage")}
                                </Label>
                                <Input />
                              </TextField>
                              <TextField
                                value={String(
                                  prescriptionItem.durationDays ?? "",
                                )}
                                onChange={(value) =>
                                  setEditItems((current) =>
                                    current.map((item) =>
                                      item.id === prescriptionItem.id
                                        ? {
                                            ...item,
                                            durationDays: value
                                              ? Math.max(1, Number(value) || 1)
                                              : null,
                                          }
                                        : item,
                                    ),
                                  )
                                }
                              >
                                <Label>
                                  {t("clinic.prescriptionFeed.editDuration")}
                                </Label>
                                <Input inputMode="numeric" />
                              </TextField>
                            </div>
                            <Tooltip>
                              <Button
                                isIconOnly
                                variant="ghost"
                                aria-label={t(
                                  "clinic.prescriptionFeed.removeItem",
                                )}
                                onPress={() =>
                                  setEditItems((current) =>
                                    current.filter(
                                      (item) => item.id !== prescriptionItem.id,
                                    ),
                                  )
                                }
                              >
                                <FaIcon icon="trash" aria-hidden="true" />
                              </Button>
                              <Tooltip.Content>
                                {t("clinic.prescriptionFeed.removeItem")}
                              </Tooltip.Content>
                            </Tooltip>
                          </div>
                        ) : (
                          <PrescriptionItemRow
                            key={
                              prescriptionItem.id ??
                              `${prescription.id}:${index}`
                            }
                            item={prescriptionItem}
                          />
                        ),
                      )}
                    {!detail.data.items?.length ? (
                      <p className="text-sm text-muted">
                        {t("clinic.prescriptionFeed.noItems")}
                      </p>
                    ) : null}
                    {isEditing ? (
                      <>
                        <SearchField
                          value={editSearch}
                          onChange={(value) => void searchEditItems(value)}
                        >
                          <Label>{t("clinic.prescriptionFeed.addItem")}</Label>
                          <SearchField.Group>
                            <SearchField.SearchIcon />
                            <SearchField.Input
                              placeholder={t(
                                "clinic.prescriptionFeed.searchItems",
                              )}
                            />
                            <SearchField.ClearButton />
                          </SearchField.Group>
                        </SearchField>
                        {editResults.length ? (
                          <ListBox
                            aria-label={t(
                              "clinic.prescriptionFeed.searchItems",
                            )}
                          >
                            {editResults.map((result) => (
                              <ListBox.Item
                                key={result.referenceCode}
                                id={result.referenceCode}
                                onAction={() => addEditItem(result)}
                              >
                                {result.displayName}
                                <span className="ms-auto text-xs text-muted">
                                  {result.referenceCode}
                                </span>
                              </ListBox.Item>
                            ))}
                          </ListBox>
                        ) : null}
                        <TextField value={editReason} onChange={setEditReason}>
                          <Label>
                            {t("clinic.prescriptionFeed.editReason")}
                          </Label>
                          <TextArea />
                        </TextField>
                        {editMessage ? (
                          <p role="alert" className="text-sm text-danger">
                            {editMessage}
                          </p>
                        ) : null}
                        <div className="flex justify-end gap-2">
                          <Tooltip>
                            <Button
                              variant="ghost"
                              onPress={() => setIsEditing(false)}
                            >
                              <FaIcon icon="xmark" aria-hidden="true" />
                              {t("common.actions.cancel")}
                            </Button>
                            <Tooltip.Content>
                              {t("common.actions.cancel")}
                            </Tooltip.Content>
                          </Tooltip>
                          <Tooltip>
                            <Button
                              variant="primary"
                              isPending={isSaving}
                              isDisabled={
                                !editReason.trim() || editItems.length === 0
                              }
                              onPress={() => void saveEdit()}
                            >
                              <FaIcon icon="paper-plane" aria-hidden="true" />
                              {t("clinic.prescriptionFeed.submitEdit")}
                            </Button>
                            <Tooltip.Content>
                              {t("clinic.prescriptionFeed.submitEdit")}
                            </Tooltip.Content>
                          </Tooltip>
                        </div>
                      </>
                    ) : canEdit ? (
                      <Tooltip>
                        <Button
                          variant="secondary"
                          onPress={() => setIsEditing(true)}
                        >
                          <FaIcon icon="pen" aria-hidden="true" />
                          {t("clinic.prescriptionFeed.edit")}
                        </Button>
                        <Tooltip.Content>
                          {t("clinic.prescriptionFeed.edit")}
                        </Tooltip.Content>
                      </Tooltip>
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
