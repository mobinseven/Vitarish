"use client";

import { Alert, Button, Chip, ListBox, Select, Spinner } from "@heroui/react";
import { Sheet, Widget } from "@heroui-pro/react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { FaIcon } from "@vitarish/ui";
import { medicalGetPatientPrescriptionDashboard, medicalGetPrescription, medicalListPatientPrescriptions } from "../../../api/generated/medical/medical";
import type { PrescriptionSummaryDto } from "../../../api/generated/medical/model";
import { useLocalizedText } from "../../../localization";

export const patientPrescriptionHistoryQueryKey = (patientId: string) => ["medical", "patient-prescriptions", patientId];

export function PatientPrescriptionDashboard({ patientId, onNewPrescription }: { patientId: string; onNewPrescription: () => void }) {
  const { t } = useLocalizedText();
  const [take, setTake] = useState(20);
  const [provider, setProvider] = useState("");
  const [status, setStatus] = useState("");
  const [selectedId, setSelectedId] = useState<string>();
  const dashboard = useQuery({ queryKey: patientPrescriptionHistoryQueryKey(patientId), queryFn: ({ signal }) => medicalGetPatientPrescriptionDashboard({ patientId, attentionTake: 8 }, undefined, signal) });
  const history = useQuery({ queryKey: [...patientPrescriptionHistoryQueryKey(patientId), "history", take], queryFn: ({ signal }) => medicalListPatientPrescriptions({ patientId, skip: 0, take }, undefined, signal) });
  const detail = useQuery({ queryKey: ["medical", "prescription", selectedId], enabled: Boolean(selectedId), queryFn: ({ signal }) => medicalGetPrescription(selectedId!, undefined, signal) });
  const records = (history.data ?? []) as PrescriptionSummaryDto[];
  const providers = [...new Set(records.map((item) => item.selectedProviderKey).filter(Boolean))] as string[];
  const statuses = [...new Set(records.map((item) => String(item.status)).filter(Boolean))];
  const filtered = records.filter((item) => (!provider || item.selectedProviderKey === provider) && (!status || String(item.status) === status));
  const selectValue = (value: string) => (value ? value : null);

  return (
    <Widget className="w-full" aria-label={t("clinic.prescriptionFeed.title")}>
      <Widget.Header className="flex-wrap py-3">
          <div>
            <p className="text-xs uppercase tracking-wider text-muted">{t("clinic.prescriptionFeed.title")}</p>
            <h2 className="text-xl font-semibold">{dashboard.data?.totalCount ?? 0} {t("clinic.prescriptionFeed.title")}</h2>
            <p className="text-sm text-muted">{dashboard.data?.attentionCount ?? dashboard.data?.attentionQueue?.length ?? 0} {t("clinic.prescriptionFeed.attentionTitle")}</p>
          </div>
          <Button variant="tertiary" onPress={onNewPrescription}><FaIcon icon="prescription-bottle-medical" aria-hidden="true" />{t("clinic.patient.quickActions.prescription")}</Button>
      </Widget.Header>
      <Widget.Content className="space-y-5">
        {dashboard.isLoading || history.isLoading ? <div className="flex justify-center py-8"><Spinner /></div> : dashboard.isError || history.isError ? <Alert status="danger"><Alert.Indicator /><Alert.Content><Alert.Title>{t("clinic.prescriptionFeed.unavailable")}</Alert.Title><Alert.Description><Button variant="outline" onPress={() => { void dashboard.refetch(); void history.refetch(); }}>{t("clinic.prescriptionFeed.retry")}</Button></Alert.Description></Alert.Content></Alert> : (
          <div className="space-y-3">
            {(dashboard.data?.attentionQueue?.length ?? 0) > 0 ? <Alert status="warning"><Alert.Indicator /><Alert.Content><Alert.Title>{t("clinic.prescriptionFeed.attentionTitle")}</Alert.Title><div className="mt-2 space-y-2">{dashboard.data?.attentionQueue?.map((item) => <div key={item.id} className="flex items-center justify-between gap-3 text-sm"><span>{item.itemDisplayNames?.join(", ") || t("clinic.prescriptionFeed.unnamedItem")}</span><Chip size="sm" variant="soft">{String(item.status ?? "")}</Chip></div>)}</div></Alert.Content></Alert> : null}
            <h3 className="font-semibold">{t("clinic.prescriptionFeed.detailsTitle")}</h3>
            <div className="grid gap-3 sm:grid-cols-2">
              <Select aria-label={t("clinic.prescriptionFeed.allProviders")} placeholder={t("clinic.prescriptionFeed.allProviders")} selectedKey={selectValue(provider)} onSelectionChange={(key) => setProvider(key === "all-providers" ? "" : String(key ?? ""))}>
                <Select.Trigger><Select.Value /></Select.Trigger>
                <Select.Popover><ListBox><ListBox.Item id="all-providers" textValue={t("clinic.prescriptionFeed.allProviders")}>{t("clinic.prescriptionFeed.allProviders")}</ListBox.Item>{providers.map((value) => <ListBox.Item key={value} id={value} textValue={value}>{value}</ListBox.Item>)}</ListBox></Select.Popover>
              </Select>
              <Select aria-label={t("clinic.prescriptionFeed.allStatuses")} placeholder={t("clinic.prescriptionFeed.allStatuses")} selectedKey={selectValue(status)} onSelectionChange={(key) => setStatus(key === "all-statuses" ? "" : String(key ?? ""))}>
                <Select.Trigger><Select.Value /></Select.Trigger>
                <Select.Popover><ListBox><ListBox.Item id="all-statuses" textValue={t("clinic.prescriptionFeed.allStatuses")}>{t("clinic.prescriptionFeed.allStatuses")}</ListBox.Item>{statuses.map((value) => <ListBox.Item key={value} id={value} textValue={value}>{value}</ListBox.Item>)}</ListBox></Select.Popover>
              </Select>
            </div>
            {filtered.length === 0 ? <p className="rounded-lg border border-dashed p-6 text-center text-muted">{t("clinic.prescriptionFeed.noItems")}</p> : filtered.map((item) => <div key={item.id} className="flex items-center justify-between gap-3 rounded-lg border border-border p-3"><Button variant="ghost" className="min-w-0 flex-1 justify-start text-left" onPress={() => setSelectedId(item.id)}><span><p className="font-medium">{item.itemDisplayNames?.join(", ") || t("clinic.prescriptionFeed.unnamedItem")}</p><p className="text-sm text-muted">{item.selectedProviderKey || "—"} · {item.itemCount ?? 0} items</p></span></Button><Chip size="sm" variant="soft">{String(item.status ?? "")}</Chip></div>)}
            {records.length >= take ? <Button variant="outline" onPress={() => setTake((value) => value + 20)}>{t("clinic.prescriptionFeed.loadMore")}</Button> : null}
          </div>
        )}
        <Sheet isOpen={Boolean(selectedId)} onOpenChange={(open) => !open && setSelectedId(undefined)}><Sheet.Backdrop><Sheet.Content className="max-w-xl"><Sheet.Dialog><Sheet.Header><Sheet.Heading>{t("clinic.prescriptionFeed.detailsTitle")}</Sheet.Heading><Sheet.CloseTrigger aria-label={t("common.actions.close")}><FaIcon icon="xmark" /></Sheet.CloseTrigger></Sheet.Header><Sheet.Body>{detail.isLoading ? <Spinner /> : detail.data ? <div className="space-y-3"><p><strong>{t("clinic.prescriptionFeed.title")}</strong> · {String(detail.data.status)}</p>{detail.data.items?.map((item) => <div key={item.id} className="rounded border p-3">{item.displayName || t("clinic.prescriptionFeed.unnamedItem")} · {item.quantity}</div>)}</div> : <p>{t("clinic.prescriptionFeed.unavailable")}</p>}</Sheet.Body></Sheet.Dialog></Sheet.Content></Sheet.Backdrop></Sheet>
      </Widget.Content>
    </Widget>
  );
}
