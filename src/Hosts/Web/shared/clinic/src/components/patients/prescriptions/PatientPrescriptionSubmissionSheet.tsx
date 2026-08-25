"use client";

import { useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, ListBox, SearchField } from "@heroui/react";
import { Sheet } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/ui";
import { clinicStartVisitPrescriptionDraft } from "../../../api/generated/clinic/clinic";
import {
  medicalAddPrescriptionItem,
  medicalApplyPrescriptionTemplate,
  medicalGetAvailableProviders,
  medicalListPrescriptionTemplates,
  medicalSearchPrescriptionReferenceCodes,
  medicalSubmitPrescription,
  medicalValidatePrescriptionDraft,
} from "../../../api/generated/medical/medical";
import type { PatientViewDto } from "../../../models/patients/patientSearchModels";
import { useLocalizedText } from "../../../localization";
import { prescriptionEventFeedQueryKey } from "../procedures/eventFeedPlugins";

interface Props {
  patient: PatientViewDto;
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated?: (eventId: string) => void;
}
type CatalogItem = {
  providerKey: string;
  prescriptionCategory: number;
  referenceCode: string;
  displayName: string;
};

export function PatientPrescriptionSubmissionSheet({
  patient,
  isOpen,
  onOpenChange,
  onCreated,
}: Props) {
  const { t } = useLocalizedText();
  const queryClient = useQueryClient();
  const idempotencyKey = useRef(crypto.randomUUID());
  const [providerKey, setProviderKey] = useState("");
  const [prescriptionId, setPrescriptionId] = useState<string>();
  const [eventId, setEventId] = useState<string>();
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<CatalogItem[]>([]);
  const [items, setItems] = useState<CatalogItem[]>([]);
  const [message, setMessage] = useState("");
  const [isWorking, setIsWorking] = useState(false);
  const providers = useQuery({
    queryKey: ["medical", "prescription-providers"],
    enabled: isOpen,
    queryFn: ({ signal }) =>
      medicalGetAvailableProviders({}, undefined, signal),
  });
  const templates = useQuery({
    queryKey: ["medical", "prescription-templates"],
    enabled: Boolean(prescriptionId),
    queryFn: ({ signal }) =>
      medicalListPrescriptionTemplates({}, undefined, signal),
  });

  async function chooseProvider(next: string) {
    if (!next || !patient.id || prescriptionId) return;
    setIsWorking(true);
    setMessage("");
    try {
      const draft = await clinicStartVisitPrescriptionDraft({
        patientId: patient.id,
        providerKey: next,
        eventTitle: t("clinic.prescriptionSubmission.eventTitle"),
        idempotencyKey: idempotencyKey.current,
      });
      setProviderKey(next);
      setPrescriptionId(draft.prescriptionId);
      setEventId(draft.eventId);
    } catch {
      setMessage(t("clinic.prescriptionSubmission.providerFailed"));
    } finally {
      setIsWorking(false);
    }
  }
  async function search(value: string) {
    setQuery(value);
    if (!prescriptionId || value.trim().length < 2) {
      setResults([]);
      return;
    }
    try {
      setResults(
        (
          await medicalSearchPrescriptionReferenceCodes({
            providerKey,
            textOrCode: value,
            activeOnly: true,
            limit: 20,
          })
        )
          .filter((item): item is CatalogItem =>
            Boolean(item.providerKey && item.referenceCode && item.displayName),
          )
          .map((item) => ({
            providerKey: item.providerKey!,
            prescriptionCategory: item.prescriptionCategory ?? 0,
            referenceCode: item.referenceCode!,
            displayName: item.displayName!,
          })),
      );
    } catch {
      setMessage(t("clinic.prescriptionSubmission.searchFailed"));
    }
  }
  async function add(item: CatalogItem) {
    if (!prescriptionId) return;
    setIsWorking(true);
    try {
      await medicalAddPrescriptionItem(prescriptionId, {
        prescriptionId,
        category: item.prescriptionCategory,
        referenceCode: item.referenceCode,
        displayName: item.displayName,
        dosageJson: null,
        quantity: 1,
        durationDays: null,
      });
      setItems((current) => [...current, item]);
      setResults([]);
      setQuery("");
    } catch {
      setMessage(t("clinic.prescriptionSubmission.itemFailed"));
    } finally {
      setIsWorking(false);
    }
  }
  async function applyTemplate(templateId: string) {
    if (!prescriptionId) return;
    setIsWorking(true);
    try {
      await medicalApplyPrescriptionTemplate(templateId, {
        templateId,
        prescriptionId,
        tenantId: "",
      });
      setMessage(t("clinic.prescriptionSubmission.templateApplied"));
    } catch {
      setMessage(t("clinic.prescriptionSubmission.templateFailed"));
    } finally {
      setIsWorking(false);
    }
  }
  async function submit() {
    if (!prescriptionId) return;
    setIsWorking(true);
    setMessage("");
    try {
      const validation = await medicalValidatePrescriptionDraft(prescriptionId);
      if (!validation.isValid) {
        setMessage(
          validation.errors?.join(" ") ||
            t("clinic.prescriptionSubmission.validationFailed"),
        );
        return;
      }
      const result = await medicalSubmitPrescription(prescriptionId, {
        prescriptionId,
        idempotencyKey: crypto.randomUUID(),
      });
      setMessage(
        result.accepted
          ? t("clinic.prescriptionSubmission.submitted")
          : (result.userSafeMessage ??
              t("clinic.prescriptionSubmission.submitFailed")),
      );
      if (result.accepted && eventId) {
        await queryClient.invalidateQueries({
          queryKey: prescriptionEventFeedQueryKey({
            patientId: patient.id!,
            eventId,
          }),
        });
        onCreated?.(eventId);
      }
    } catch {
      setMessage(t("clinic.prescriptionSubmission.submitFailed"));
    } finally {
      setIsWorking(false);
    }
  }
  return (
    <Sheet isOpen={isOpen} placement="bottom" onOpenChange={onOpenChange}>
      <Sheet.Backdrop>
        <Sheet.Content className="mx-auto max-h-[96vh] max-w-3xl">
          <Sheet.Dialog className="flex h-[88vh] flex-col">
            <Sheet.Handle />
            <Sheet.Header>
              <Sheet.Heading>
                {t("clinic.prescriptionSubmission.title")}
              </Sheet.Heading>
              <Sheet.CloseTrigger aria-label={t("common.actions.close")}>
                <FaIcon icon="xmark" />
              </Sheet.CloseTrigger>
            </Sheet.Header>
            <Sheet.Body className="min-h-0 flex-1 overflow-y-auto space-y-6">
              <p className="text-sm text-muted">
                {patient.name ??
                  [patient.firstName, patient.lastName]
                    .filter(Boolean)
                    .join(" ")}
              </p>
              <section>
                <h2 className="font-semibold">
                  {t("clinic.prescriptionSubmission.provider")}
                </h2>
                <div className="mt-2 flex flex-wrap gap-2">
                  {(providers.data ?? [])
                    .filter((provider) => Boolean(provider.providerKey))
                    .map((provider) => (
                      <Button
                        key={provider.providerKey}
                        isDisabled={isWorking || Boolean(prescriptionId)}
                        variant={
                          provider.providerKey === providerKey
                            ? "primary"
                            : "outline"
                        }
                        onPress={() =>
                          void chooseProvider(provider.providerKey!)
                        }
                      >
                        {provider.providerKey}
                      </Button>
                    ))}
                </div>
              </section>
              {prescriptionId ? (
                <>
                  <section>
                    <h2 className="font-semibold">
                      {t("clinic.prescriptionSubmission.template")}
                    </h2>
                    <div className="mt-2 flex flex-wrap gap-2">
                      {(templates.data ?? []).map((template) => (
                        <Button
                          key={template.id}
                          size="sm"
                          variant="secondary"
                          onPress={() =>
                            template.id && void applyTemplate(template.id)
                          }
                        >
                          {template.name}
                        </Button>
                      ))}
                    </div>
                  </section>
                  <section>
                    <h2 className="font-semibold">
                      {t("clinic.prescriptionSubmission.items")}
                    </h2>
                    <SearchField value={query} onChange={search}>
                      <SearchField.Group>
                        <SearchField.SearchIcon />
                        <SearchField.Input
                          placeholder={t(
                            "clinic.prescriptionSubmission.search",
                          )}
                        />
                        <SearchField.ClearButton />
                      </SearchField.Group>
                    </SearchField>
                    {results.length ? (
                      <ListBox
                        aria-label={t("clinic.prescriptionSubmission.search")}
                        className="mt-2"
                      >
                        {results.map((item) => (
                          <ListBox.Item
                            key={`${item.providerKey}:${item.referenceCode}`}
                            id={item.referenceCode}
                            onAction={() => void add(item)}
                          >
                            {item.displayName}
                            <span className="ms-auto text-xs text-muted">
                              {item.referenceCode}
                            </span>
                          </ListBox.Item>
                        ))}
                      </ListBox>
                    ) : null}
                    <ul className="mt-3 space-y-2">
                      {items.map((item, index) => (
                        <li
                          key={`${item.referenceCode}-${index}`}
                          className="rounded-xl bg-surface px-3 py-2 text-sm"
                        >
                          {item.displayName}
                          <span className="ms-2 text-muted">
                            {item.referenceCode}
                          </span>
                        </li>
                      ))}
                    </ul>
                  </section>
                </>
              ) : (
                <p className="rounded-xl bg-surface p-3 text-sm text-muted">
                  {t("clinic.prescriptionSubmission.providerRequired")}
                </p>
              )}
              {message ? (
                <p role="status" className="text-sm">
                  {message}
                </p>
              ) : null}
            </Sheet.Body>
            <Sheet.Footer>
              <Button
                variant="primary"
                isDisabled={!prescriptionId || isWorking}
                isPending={isWorking}
                onPress={() => void submit()}
              >
                <FaIcon icon="paper-plane" />
                {t("clinic.prescriptionSubmission.submit")}
              </Button>
            </Sheet.Footer>
          </Sheet.Dialog>
        </Sheet.Content>
      </Sheet.Backdrop>
    </Sheet>
  );
}
