"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  Button,
  Input,
  Label,
  ListBox,
  SearchField,
  Select,
  Surface,
  TextArea,
  TextField,
} from "@heroui/react";
import { EmptyState } from "@heroui-pro/react";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { useLocalizedText } from "../../localization";
import {
  medicalGetAvailableProviders,
  medicalGetPrescriptionTemplate,
  medicalListPrescriptionTemplates,
  medicalSavePrescriptionTemplate,
  medicalSearchPrescriptionReferenceCodes,
} from "../../api/generated/medical/medical";

type DraftItem = {
  id: string;
  category: number;
  name: string;
  code: string;
  providerKey: string;
  catalogIdentity: string;
  dosage: string;
  quantity: string;
  duration: string;
};
type Draft = {
  id?: string;
  name: string;
  description: string;
  items: DraftItem[];
};
type CatalogResult = {
  providerKey: string;
  codeSystem: string;
  prescriptionCategory: number;
  referenceCode: string;
  displayName: string;
  metadata?: Record<string, string | null>;
};
type TemplateSummary = {
  id: string;
  name: string;
  description?: string;
  itemCount?: number;
};
type Provider = { key: string; name: string };

const categoryKeys = ["drug", "lab", "imaging", "service", "referral"] as const;
const asArray = <T,>(value: unknown): T[] =>
  Array.isArray(value) ? (value as T[]) : [];

export function PrescriptionTemplateWorkspace() {
  const { t, direction } = useLocalizedText();
  const [templates, setTemplates] = useState<TemplateSummary[]>([]);
  const [draft, setDraft] = useState<Draft>({
    name: "",
    description: "",
    items: [],
  });
  const [selectedId, setSelectedId] = useState<string>();
  const [providers, setProviders] = useState<Provider[]>([]);
  const [providerKey, setProviderKey] = useState("");
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<CatalogResult[]>([]);
  const [status, setStatus] = useState<
    "loading" | "ready" | "empty" | "error" | "unavailable"
  >("loading");
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const requestRef = useRef<AbortController | undefined>(undefined);
  const searchRef = useRef<ReturnType<typeof setTimeout> | undefined>(
    undefined,
  );

  const loadTemplates = useCallback(async () => {
    try {
      const response = await medicalListPrescriptionTemplates({});
      const next = asArray<TemplateSummary>(response);
      setTemplates(next);
      if (!selectedId && next[0]) setSelectedId(next[0].id);
    } catch {
      setStatus("error");
    }
  }, [selectedId]);

  useEffect(() => {
    void loadTemplates();
    void medicalGetAvailableProviders({})
      .then((value) => {
        const raw = asArray<Record<string, unknown>>(value);
        const next = raw
          .map((item) => ({
            key: String(item.key ?? item.providerKey ?? ""),
            name: String(item.name ?? item.displayName ?? item.key ?? ""),
          }))
          .filter((item) => item.key);
        setProviders(next);
        if (next[0]) setProviderKey(next[0].key);
        else setStatus("unavailable");
      })
      .catch(() => setStatus("unavailable"));
  }, [loadTemplates]);

  useEffect(() => {
    if (!selectedId) return;
    const controller = new AbortController();
    setStatus("loading");
    void medicalGetPrescriptionTemplate(selectedId, { templateId: selectedId })
      .then((value) => {
        const item = value as unknown as {
          id: string;
          name: string;
          description?: string;
          items?: Array<Record<string, unknown>>;
        };
        setDraft({
          id: item.id,
          name: item.name ?? "",
          description: item.description ?? "",
          items: asArray<Record<string, unknown>>(item.items).map((entry) => ({
            id: String(entry.id),
            category: Number(entry.category),
            name: String(entry.displayName ?? ""),
            code: String(entry.referenceCode ?? ""),
            providerKey: String(entry.providerKey ?? ""),
            catalogIdentity: String(
              entry.codeSystem ?? entry.referenceCode ?? "",
            ),
            dosage: String(entry.dosageJson ?? ""),
            quantity: String(entry.quantity ?? 1),
            duration:
              entry.durationDays == null ? "" : String(entry.durationDays),
          })),
        });
        setStatus("ready");
      })
      .catch(() => setStatus("error"));
    return () => controller.abort();
  }, [selectedId]);

  function search(value: string) {
    setQuery(value);
    if (searchRef.current) clearTimeout(searchRef.current);
    requestRef.current?.abort();
    if (!value.trim() || !providerKey) {
      setResults([]);
      setStatus(providerKey ? "ready" : "unavailable");
      return;
    }
    searchRef.current = setTimeout(() => {
      const controller = new AbortController();
      requestRef.current = controller;
      setStatus("loading");
      void medicalSearchPrescriptionReferenceCodes(
        { providerKey, textOrCode: value, activeOnly: true, limit: 20 },
        undefined,
        controller.signal,
      )
        .then((response) => {
          if (!controller.signal.aborted) {
            const next = asArray<CatalogResult>(response);
            setResults(next);
            setStatus(next.length ? "ready" : "empty");
          }
        })
        .catch(() => {
          if (!controller.signal.aborted) setStatus("error");
        });
    }, 300);
  }

  function add(result: CatalogResult) {
    setDraft((current) => ({
      ...current,
      items: [
        ...current.items,
        {
          id: `draft-${crypto.randomUUID()}`,
          category: result.prescriptionCategory,
          name: result.displayName,
          code: result.referenceCode,
          providerKey: result.providerKey,
          catalogIdentity: `${result.providerKey}:${result.codeSystem}:${result.referenceCode}`,
          dosage: "",
          quantity: "1",
          duration: "",
        },
      ],
    }));
    setQuery("");
    setResults([]);
    setStatus("ready");
  }
  function updateItem(id: string, patch: Partial<DraftItem>) {
    setDraft((current) => ({
      ...current,
      items: current.items.map((item) =>
        item.id === id ? { ...item, ...patch } : item,
      ),
    }));
  }
  async function save() {
    setSaving(true);
    setMessage("");
    try {
      await medicalSavePrescriptionTemplate({
        templateId: draft.id ?? null,
        tenantId: "",
        name: draft.name,
        description: draft.description || null,
        items: draft.items.map((item) => ({
          category: item.category,
          referenceCode: item.code,
          displayName: item.name,
          quantity: Number(item.quantity) || 1,
          durationDays: item.duration ? Number(item.duration) : null,
        })),
      });
      setMessage(t("prescriptionTemplates.saved"));
      await loadTemplates();
    } catch {
      setMessage(t("prescriptionTemplates.saveFailed"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div
      className="mx-auto flex h-full min-h-0 w-full max-w-[1200px] flex-col gap-5 px-4 py-6"
      dir={direction}
    >
      <header className="flex items-end justify-between gap-4">
        <div className="flex items-center gap-4">
          <HeaderIcon icon="prescription" level="page" />
          <div className="min-w-0">
            <p className="text-xs font-semibold uppercase tracking-[.16em] text-accent">
              {t("prescriptionTemplates.settingsLabel")}
            </p>
            <h1 className="mt-2 text-3xl font-semibold">
              {t("prescriptionTemplates.title")}
            </h1>
            <p className="mt-2 max-w-2xl text-sm text-muted">
              {t("prescriptionTemplates.description")}
            </p>
          </div>
        </div>
        <Button onPress={save} isPending={saving} variant="primary">
          <FaIcon icon="floppy-disk" aria-hidden="true" />
          {t("prescriptionTemplates.save")}
        </Button>
      </header>
      {message && (
        <p role="status" className="text-sm text-success">
          {message}
        </p>
      )}
      <div className="grid min-h-0 flex-1 gap-5 lg:grid-cols-[18rem_1fr]">
        <Surface className="flex min-h-0 flex-col gap-6 rounded-3xl p-6" role="complementary">
          <div className="min-h-0 flex-1 overflow-y-auto">
            <Button
              className="mb-3 w-full"
              variant="secondary"
              onPress={() => {
                setDraft({
                  name: t("prescriptionTemplates.newTemplateName"),
                  description: "",
                  items: [],
                });
                setSelectedId(undefined);
              }}
            >
              {t("prescriptionTemplates.newTemplate")}
            </Button>
            {templates.map((item) => (
              <Button
                key={item.id}
                className="mb-1 w-full justify-start text-start"
                variant={item.id === selectedId ? "primary" : "ghost"}
                onPress={() => setSelectedId(item.id)}
              >
                {item.name}
                <span className="ms-auto text-xs">{item.itemCount ?? 0}</span>
              </Button>
            ))}
          </div>
        </Surface>
        <Surface className="flex min-h-0 flex-col gap-6 rounded-3xl p-6">
          <div className="min-h-0 flex-1 overflow-y-auto">
            <div className="grid gap-4 sm:grid-cols-2">
              <TextField
                value={draft.name}
                onChange={(value) => setDraft({ ...draft, name: value })}
              >
                <Label>{t("prescriptionTemplates.name")}</Label>
                <Input />
              </TextField>
              <TextField
                value={draft.description}
                onChange={(value) => setDraft({ ...draft, description: value })}
              >
                <Label>{t("prescriptionTemplates.descriptionLabel")}</Label>
                <TextArea />
              </TextField>
            </div>
            <Surface className="mt-6 rounded-xl p-4" variant="secondary">
              <Label>{t("prescriptionTemplates.provider")}</Label>
              <select
                aria-label={t("prescriptionTemplates.provider")}
                value={providerKey}
                onChange={(event) => {
                  setProviderKey(event.target.value);
                  setResults([]);
                }}
                className="mt-1 w-full rounded-lg border border-separator bg-surface p-2"
              >
                {providers.map((provider) => (
                  <option key={provider.key} value={provider.key}>
                    {provider.name}
                  </option>
                ))}
              </select>
              <SearchField className="mt-3" value={query} onChange={search}>
                <Label>{t("prescriptionTemplates.catalogSearch")}</Label>
                <SearchField.Group>
                  <SearchField.SearchIcon />
                  <SearchField.Input
                    placeholder={t(
                      "prescriptionTemplates.catalogSearchPlaceholder",
                    )}
                  />
                  <SearchField.ClearButton />
                </SearchField.Group>
              </SearchField>
              <div
                role="status"
                aria-live="polite"
                className="mt-2 text-sm text-muted"
              >
                {status === "loading"
                  ? t("prescriptionTemplates.searching")
                  : status === "error"
                    ? t("prescriptionTemplates.searchFailed")
                    : status === "unavailable"
                      ? t("prescriptionTemplates.catalogUnavailable")
                      : status === "empty"
                        ? t("prescriptionTemplates.catalogEmpty")
                        : ""}
              </div>
              {results.length > 0 && (
                <ListBox aria-label={t("prescriptionTemplates.catalogResults")}>
                  {results.map((result) => (
                    <ListBox.Item
                      key={`${result.providerKey}:${result.referenceCode}`}
                      id={result.referenceCode}
                      textValue={`${result.displayName} ${result.referenceCode}`}
                      onAction={() => add(result)}
                    >
                      {result.displayName}
                      <span className="ms-auto text-xs text-muted">
                        {result.referenceCode} ·{" "}
                        {t(
                          `prescriptionTemplates.categories.${categoryKeys[result.prescriptionCategory] ?? "service"}`,
                        )}
                      </span>
                    </ListBox.Item>
                  ))}
                </ListBox>
              )}
            </Surface>
            <div className="mt-6 space-y-3">
              {draft.items.length === 0 ? (
                <EmptyState>
                  <EmptyState.Title>
                    {t("prescriptionTemplates.emptyItems")}
                  </EmptyState.Title>
                  <EmptyState.Description>
                    {t("prescriptionTemplates.addFromCatalog")}
                  </EmptyState.Description>
                </EmptyState>
              ) : (
                draft.items.map((item) => (
                  <Surface
                    key={item.id}
                    className="rounded-xl p-4"
                    variant="secondary"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-semibold">{item.name}</p>
                        <p className="text-sm text-muted">
                          {item.code} ·{" "}
                          {t(
                            `prescriptionTemplates.categories.${categoryKeys[item.category] ?? "service"}`,
                          )}
                        </p>
                        <p className="text-xs text-muted">
                          {t("prescriptionTemplates.identityLocked")}
                        </p>
                      </div>
                      <Button
                        variant="ghost"
                        onPress={() =>
                          setDraft({
                            ...draft,
                            items: draft.items.filter(
                              (entry) => entry.id !== item.id,
                            ),
                          })
                        }
                      >
                        {t("prescriptionTemplates.changeIdentity")}
                      </Button>
                    </div>
                    <div className="mt-4 grid gap-3 sm:grid-cols-3">
                      <TextField
                        value={item.dosage}
                        onChange={(value) =>
                          updateItem(item.id, { dosage: value })
                        }
                      >
                        <Label>{t("prescriptionTemplates.dosage")}</Label>
                        <Input />
                      </TextField>
                      <TextField
                        value={item.quantity}
                        onChange={(value) =>
                          updateItem(item.id, { quantity: value })
                        }
                      >
                        <Label>{t("prescriptionTemplates.quantity")}</Label>
                        <Input inputMode="numeric" />
                      </TextField>
                      <TextField
                        value={item.duration}
                        onChange={(value) =>
                          updateItem(item.id, { duration: value })
                        }
                      >
                        <Label>{t("prescriptionTemplates.duration")}</Label>
                        <Input inputMode="numeric" />
                      </TextField>
                    </div>
                  </Surface>
                ))
              )}
            </div>
          </div>
        </Surface>
      </div>
    </div>
  );
}
