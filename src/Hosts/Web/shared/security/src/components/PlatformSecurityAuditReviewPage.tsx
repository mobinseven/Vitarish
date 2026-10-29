"use client";

import { Alert, Button, Checkbox, Chip, Input, Label, ListBox, Select, Spinner, Surface, TextField, Tooltip } from "@heroui/react";
import { DataGrid, Sheet, type DataGridColumn } from "@heroui-pro/react";
import { ApiError } from "@vitarish/api-client";
import { formatDateTime, formatNumber, localizeApiError, parseIsoDateValue, useLocalizedText, type SupportedLocale } from "@vitarish/localization";
import { FaIcon, LoadingSpinner  } from "@vitarish/ui";
import { useMemo, useState, type FormEvent, type ReactNode } from "react";
import { describeTransferredFile } from "./transferredFile";
import {
  localizePlatformAuditCategory,
  localizePlatformAuditChangeField,
  localizePlatformAuditChangeValue,
  localizePlatformAuditEvent,
  localizeSecurityAuditOutcome,
} from "../localization";
import { AuditDateFilter } from "./AuditDateFilter";
import { auditOutcomeColor } from "./auditOutcomeColor";
import {
  auditPlatformSecurityEventsExport,
  useAuditPlatformSecurityEventCatalogGet,
  useAuditPlatformSecurityEventsList,
} from "../api/generated/audit/audit";
import type {
  AuditPlatformSecurityEventsExportParams,
  AuditPlatformSecurityEventsListParams,
  SecurityAuditChange,
  SecurityAuditEventReadDto,
  SecurityAuditSortField,
} from "../api/generated/audit/model";
import { SecurityAuditChangeField } from "../api/generated/audit/model";
import { auditSortFields } from "../api/securityAuditSortFields";

const pageSize = 25;
const securityAuditExportFileName = "security-audit-events.csv";
export interface PlatformAuditTenantOption {
  id?: string;
  name?: string;
}

export interface PlatformSecurityAuditReviewPageProps {
  tenants: readonly PlatformAuditTenantOption[];
  tenantsLoading?: boolean;
  tenantsError?: boolean;
  onRetryTenants?: () => void;
}

interface AuditFilters {
  tenantId: string;
  eventId: string;
  actorId: string;
  category: string;
  eventName: string;
  operation: string;
  outcome: string;
  fromDate: string;
  toDate: string;
  uncataloguedOnly: boolean;
}

type AuditFilterKey = keyof AuditFilters;

type AuditChipColor = "accent" | "default" | "danger" | "success" | "warning";

interface ActiveFilterChip {
  key: AuditFilterKey;
  label: string;
  color: AuditChipColor;
}

function activeFilterColor(key: AuditFilterKey): ActiveFilterChip["color"] {
  switch (key) {
    case "outcome":
      return "success";
    case "uncataloguedOnly":
      return "warning";
    case "category":
    case "eventName":
      return "accent";
    default:
      return "default";
  }
}

function createInitialFilters(): AuditFilters {
  const today = new Date();
  const from = new Date(today);
  from.setUTCDate(today.getUTCDate() - 6);
  return {
    tenantId: "",
    eventId: "",
    actorId: "",
    category: "",
    eventName: "",
    operation: "",
    outcome: "",
    fromDate: from.toISOString().slice(0, 10),
    toDate: today.toISOString().slice(0, 10),
    uncataloguedOnly: false,
  };
}

function toUtcBoundary(value: string, endOfDay: boolean): string | undefined {
  const dateValue = parseIsoDateValue(value);
  if (!dateValue) return undefined;
  const date = dateValue.toDate("UTC");
  if (endOfDay) date.setUTCHours(23, 59, 59, 999);
  return date.toISOString();
}

function displayValue(value: string | null | undefined, fallback: string): string {
  return value?.trim() || fallback;
}

function errorStatus(error: unknown): number {
  return error instanceof ApiError ? error.status : typeof error === "object" && error !== null && "status" in error
    ? Number((error as { status?: number | string }).status)
    : 0;
}

function errorMessage(error: unknown, t: ReturnType<typeof useLocalizedText>["t"]): string {
  switch (errorStatus(error)) {
    case 403:
      return t("security.platformAudit.forbidden");
    case 413:
      return t("security.platformAudit.exportTooLarge");
    case 503:
      return t("security.platformAudit.unavailable");
    default:
      return localizeApiError(error, t);
  }
}

function sortLabel(field: SecurityAuditSortField, t: ReturnType<typeof useLocalizedText>["t"]): string {
  switch (field) {
    case auditSortFields.actor:
      return t("security.platformAudit.sortActor");
    case auditSortFields.actorKind:
      return t("security.platformAudit.sortActorKind");
    case auditSortFields.sourceIpKind:
      return t("security.platformAudit.sortSourceIpKind");
    case auditSortFields.eventType:
      return t("security.platformAudit.sortEventType");
    case auditSortFields.sourceIpAddress:
      return t("security.platformAudit.sortSourceIpAddress");
    default:
      return t("security.platformAudit.sortOccurredAt");
  }
}

function FilterSelect({
  label,
  value,
  options,
  placeholder,
  isDisabled,
  onChange,
}: {
  label: string;
  value: string;
  options: readonly { id: string; label: string }[];
  placeholder: string;
  isDisabled?: boolean;
  onChange: (value: string) => void;
}) {
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <Label>{label}</Label>
      <Select
        aria-label={label}
        className="min-w-0"
        isDisabled={isDisabled}
        placeholder={placeholder}
        value={value}
        variant="secondary"
        onChange={(nextValue) => onChange(nextValue === null ? "" : String(nextValue))}
      >
        <Select.Trigger>
          <Select.Value className="min-w-0 overflow-hidden text-ellipsis whitespace-nowrap" />
          <Select.Indicator />
        </Select.Trigger>
        <Select.Popover>
          <ListBox>
            {options.map((option) => (
              <ListBox.Item key={option.id} id={option.id} textValue={option.label}>
                {option.label}
                <ListBox.ItemIndicator />
              </ListBox.Item>
            ))}
          </ListBox>
        </Select.Popover>
      </Select>
    </div>
  );
}

// The change fields whose values are catalogued codes rather than user data.
const CODED_CHANGE_FIELDS: ReadonlySet<SecurityAuditChangeField> = new Set([SecurityAuditChangeField.accountStatus, SecurityAuditChangeField.tenantRole]);

function AuditChangeList({
  changes,
  locale,
  t,
}: {
  changes: SecurityAuditChange[];
  locale: SupportedLocale;
  t: ReturnType<typeof useLocalizedText>["t"];
}) {
  if (changes.length === 0) return <>{t("security.platformAudit.notAvailable")}</>;
  const value = (field: SecurityAuditChangeField, text: string | null | undefined) => {
    if (text == null) return t("security.platformAudit.changeNoValue");
    return CODED_CHANGE_FIELDS.has(field) ? localizePlatformAuditChangeValue(locale, text, text) : text;
  };
  return (
    <ul className="space-y-1">
      {changes.map((change) => (
        <li key={change.field}>
          {t("security.platformAudit.changeValue", {
            field: localizePlatformAuditChangeField(locale, change.field, change.field),
            previous: `\u2068${value(change.field, change.previousValue)}\u2069`,
            next: `\u2068${value(change.field, change.newValue)}\u2069`,
          })}
        </li>
      ))}
    </ul>
  );
}

function DetailField({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="grid grid-cols-[minmax(7rem,0.7fr)_minmax(0,1.3fr)] gap-3 border-b border-separator py-3 last:border-0">
      <dt className="text-xs font-medium uppercase tracking-wide text-muted">{label}</dt>
      <dd className="min-w-0 break-words text-sm text-foreground">{value}</dd>
    </div>
  );
}

export function PlatformSecurityAuditReviewPage({
  tenants,
  tenantsLoading = false,
  tenantsError = false,
  onRetryTenants,
}: PlatformSecurityAuditReviewPageProps) {
  const { t, locale, direction } = useLocalizedText();
  const catalogQuery = useAuditPlatformSecurityEventCatalogGet();
  const [draftFilters, setDraftFilters] = useState<AuditFilters>(() => createInitialFilters());
  const [filters, setFilters] = useState<AuditFilters>(() => createInitialFilters());
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState<SecurityAuditSortField>(auditSortFields.occurredAt);
  const [descending, setDescending] = useState(true);
  const [selectedEvent, setSelectedEvent] = useState<SecurityAuditEventReadDto | null>(null);
  const [dateRangeError, setDateRangeError] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);

  const catalog = catalogQuery.data ?? [];
  const categoryOptions = useMemo(
    () => Array.from(new Set(catalog.map((entry) => entry.category))).map((category) => ({
      id: category,
      label: localizePlatformAuditCategory(locale, category, category),
    })),
    [catalog, locale],
  );
  const eventOptions = useMemo(
    () => Array.from(new Set(catalog.filter((entry) => !draftFilters.category || entry.category === draftFilters.category).map((entry) => entry.eventName)))
      .map((eventName) => ({ id: eventName, label: localizePlatformAuditEvent(locale, eventName, eventName) })),
    [catalog, draftFilters.category, locale],
  );
  const operationOptions = useMemo(
    () => Array.from(new Map(
      catalog
        .filter((entry) => (!draftFilters.category || entry.category === draftFilters.category) && (!draftFilters.eventName || entry.eventName === draftFilters.eventName) && !entry.isWildcard)
        .map((entry) => [entry.operation, entry] as const),
    ).keys()).map((operation) => ({ id: operation, label: operation })),
    [catalog, draftFilters.category, draftFilters.eventName],
  );
  const tenantOptions = useMemo(
    () => tenants
      .filter((tenant) => tenant.id)
      .map((tenant) => ({ id: tenant.id!, label: tenant.name?.trim() || tenant.id! })),
    [tenants],
  );
  const tenantNamesById = useMemo(
    () => new Map(tenantOptions.map((tenant) => [tenant.id, tenant.label])),
    [tenantOptions],
  );

  const params = useMemo<AuditPlatformSecurityEventsListParams>(() => ({
    page,
    pageSize,
    tenantId: filters.tenantId || undefined,
    eventId: filters.eventId || undefined,
    actorId: filters.actorId || undefined,
    eventName: filters.eventName || undefined,
    eventCategory: filters.category || undefined,
    fromUtc: toUtcBoundary(filters.fromDate, false),
    toUtc: toUtcBoundary(filters.toDate, true),
    sortBy,
    descending,
    operation: filters.operation || undefined,
    outcome: filters.outcome || undefined,
    uncataloguedOnly: filters.uncataloguedOnly,
  }), [descending, filters, page, sortBy]);
  const exportParams = useMemo<AuditPlatformSecurityEventsExportParams>(() => ({
    tenantId: filters.tenantId || undefined,
    eventId: filters.eventId || undefined,
    actorId: filters.actorId || undefined,
    eventName: filters.eventName || undefined,
    eventCategory: filters.category || undefined,
    fromUtc: toUtcBoundary(filters.fromDate, false),
    toUtc: toUtcBoundary(filters.toDate, true),
    sortBy,
    descending,
    operation: filters.operation || undefined,
    outcome: filters.outcome || undefined,
    uncataloguedOnly: filters.uncataloguedOnly,
  }), [descending, filters, sortBy]);
  const query = useAuditPlatformSecurityEventsList(params);
  const result = query.data;
  const totalCount = Number(result?.totalCount ?? 0);
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const filtersInvalid = Boolean(filters.uncataloguedOnly && filters.category);
  const activeFilterChips = useMemo<ActiveFilterChip[]>(() => {
    const chips: ActiveFilterChip[] = [];
    const add = (key: AuditFilterKey, label: string, value: string) => {
      if (value) chips.push({ key, label: `${label}: ${value}`, color: activeFilterColor(key) });
    };
    const tenant = tenantOptions.find((option) => option.id === filters.tenantId);
    add("tenantId", t("security.platformAudit.scope"), tenant?.label ?? filters.tenantId);
    add("eventId", t("security.platformAudit.eventId"), filters.eventId);
    add("actorId", t("security.platformAudit.actorId"), filters.actorId);
    add("category", t("security.platformAudit.category"), localizePlatformAuditCategory(locale, filters.category, filters.category));
    add("eventName", t("security.platformAudit.event"), localizePlatformAuditEvent(locale, filters.eventName, filters.eventName));
    add("operation", t("security.platformAudit.operation"), filters.operation);
    add("outcome", t("security.platformAudit.outcome"), filters.outcome);
    add("fromDate", t("security.platformAudit.fromDate"), filters.fromDate);
    add("toDate", t("security.platformAudit.toDate"), filters.toDate);
    if (filters.uncataloguedOnly) chips.push({ key: "uncataloguedOnly", label: t("security.platformAudit.uncataloguedOnly"), color: activeFilterColor("uncataloguedOnly") });
    return chips;
  }, [filters, locale, t, tenantOptions]);

  const updateDraft = <K extends keyof AuditFilters>(key: K, value: AuditFilters[K]) => {
    setDraftFilters((current) => ({ ...current, [key]: value }));
  };

  const applyFilters = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (draftFilters.fromDate && draftFilters.toDate && draftFilters.fromDate > draftFilters.toDate) {
      setDateRangeError(true);
      return;
    }
    setDateRangeError(false);
    setFilters({ ...draftFilters });
    setPage(1);
  };

  const clearFilters = () => {
    const cleared: AuditFilters = {
      tenantId: "",
      eventId: "",
      actorId: "",
      category: "",
      eventName: "",
      operation: "",
      outcome: "",
      fromDate: "",
      toDate: "",
      uncataloguedOnly: false,
    };
    setDraftFilters(cleared);
    setFilters(cleared);
    setDateRangeError(false);
    setPage(1);
  };

  const removeFilter = (key: AuditFilterKey) => {
    const next = { ...filters };
    switch (key) {
      case "category":
        next.category = "";
        next.eventName = "";
        next.operation = "";
        break;
      case "eventName":
        next.eventName = "";
        next.operation = "";
        break;
      case "uncataloguedOnly":
        next.uncataloguedOnly = false;
        next.category = "";
        next.eventName = "";
        next.operation = "";
        break;
      default:
        next[key] = "" as never;
        break;
    }
    setFilters(next);
    setDraftFilters(next);
    setPage(1);
  };

  const selectCategory = (category: string) => {
    setDraftFilters((current) => ({ ...current, category, eventName: "", operation: "" }));
  };

  const selectEvent = (eventName: string) => {
    setDraftFilters((current) => ({ ...current, eventName, operation: "" }));
  };

  const toggleUncatalogued = (isSelected: boolean) => {
    setDraftFilters((current) => ({
      ...current,
      uncataloguedOnly: isSelected,
      category: "",
      eventName: "",
      operation: "",
    }));
  };

  const exportRows = async () => {
    setIsExporting(true);
    setExportError(null);
    try {
      const blob = await auditPlatformSecurityEventsExport(exportParams);
      const objectUrl = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = objectUrl;
      anchor.download = securityAuditExportFileName;
      anchor.click();
      URL.revokeObjectURL(objectUrl);
    } catch (error) {
      setExportError(errorMessage(error, t));
    } finally {
      setIsExporting(false);
    }
  };

  const columns: DataGridColumn<SecurityAuditEventReadDto>[] = [
    {
      id: "occurredAt",
      header: t("security.platformAudit.occurredAt"),
      accessorKey: "occurredAt",
      isRowHeader: true,
      minWidth: 170,
      cell: (item) => <span className="whitespace-nowrap text-xs">{item.occurredAt ? formatDateTime(item.occurredAt, locale) : t("security.platformAudit.notAvailable")}</span>,
    },
    {
      id: "tenantId",
      header: t("security.platformAudit.scope"),
      accessorKey: "tenantId",
      minWidth: 150,
      cell: (item) => {
        const scope = displayValue(item.tenantId, t("security.platformAudit.notAvailable"));
        const tenantName = item.tenantId ? tenantNamesById.get(item.tenantId) : undefined;
        return <span className={tenantName ? "text-xs" : "font-mono text-xs"} title={tenantName ? scope : undefined}>{tenantName ?? scope}</span>;
      },
    },
    {
      id: "eventCategory",
      header: t("security.platformAudit.category"),
      accessorKey: "eventCategory",
      minWidth: 160,
      cell: (item) => <span className="text-xs">{localizePlatformAuditCategory(locale, item.eventCategory, t("security.platformAudit.uncatalogued"))}</span>,
    },
    {
      id: "eventName",
      header: t("security.platformAudit.event"),
      accessorKey: "eventName",
      minWidth: 280,
      cell: (item) => <span className="font-medium">{localizePlatformAuditEvent(locale, item.eventName, t("security.platformAudit.notAvailable"))}</span>,
    },
    {
      id: "operation",
      header: t("security.platformAudit.operation"),
      accessorKey: "operation",
      minWidth: 190,
      cell: (item) => <span className="font-mono text-xs">{displayValue(item.operation, t("security.platformAudit.notAvailable"))}</span>,
    },
    {
      id: "actorId",
      header: t("security.platformAudit.actor"),
      accessorKey: "actorId",
      minWidth: 150,
      cell: (item) => <span className="font-mono text-xs">{displayValue(item.actorId, displayValue(item.actorKind, t("security.platformAudit.unknownActor")))}</span>,
    },
    {
      id: "outcome",
      header: t("security.platformAudit.outcome"),
      accessorKey: "outcome",
      minWidth: 110,
       cell: (item) => <Chip color={auditOutcomeColor(item.outcome)} size="sm" variant="soft"><Chip.Label>{localizeSecurityAuditOutcome(locale, item.outcome, t("security.platformAudit.notAvailable"))}</Chip.Label></Chip>,
    },
    {
      id: "sourceIpAddress",
      header: t("security.platformAudit.sourceIp"),
      accessorKey: "sourceIpAddress",
      minWidth: 150,
      cell: (item) => <span className="font-mono text-xs">{displayValue(item.sourceIpAddress, displayValue(item.sourceIpKind, t("security.platformAudit.notAvailable")))}</span>,
    },
  ];

  return (
    <div className="mx-auto flex w-full max-w-[1600px] flex-col gap-8 px-4 pb-8 pt-8 sm:px-6 sm:pt-10 lg:px-8">
      <header className="flex flex-col gap-5 pb-2 sm:flex-row sm:items-end sm:justify-between">
        <div className="max-w-3xl">
          <div className="mb-3 flex items-center gap-3 text-accent">
            <FaIcon icon="shield-halved" aria-hidden="true" className="text-xl" />
            <span className="text-xs font-semibold uppercase tracking-[0.18em]">{t("security.platformAudit.eyebrow")}</span>
          </div>
          <h1 className="text-3xl font-semibold tracking-tight text-foreground">{t("security.platformAudit.title")}</h1>
        </div>
        <div className="flex items-center gap-2 text-xs text-muted">
          <FaIcon icon="database" aria-hidden="true" />
          <span>{t("security.platformAudit.readOnly")}</span>
        </div>
      </header>

      {result?.capacityThresholdReached ? (
        <Alert status="warning">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("security.platformAudit.capacityWarning")}</Alert.Title>
          </Alert.Content>
        </Alert>
      ) : null}

      {catalogQuery.isError ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{errorMessage(catalogQuery.error, t)}</Alert.Title>
            <Alert.Description>{t("security.platformAudit.catalogUnavailable")}</Alert.Description>
          </Alert.Content>
        </Alert>
      ) : null}

      {tenantsError ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("security.platformAudit.tenantsUnavailable")}</Alert.Title>
            {onRetryTenants ? <Alert.Description><Tooltip><Button size="sm" type="button" variant="outline" onPress={onRetryTenants}><FaIcon icon="rotate-right" aria-hidden="true" />{t("security.platformAudit.retry")}</Button><Tooltip.Content>{t("security.platformAudit.retry")}</Tooltip.Content></Tooltip></Alert.Description> : null}
          </Alert.Content>
        </Alert>
      ) : null}

      {exportError ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content><Alert.Title>{exportError}</Alert.Title></Alert.Content>
        </Alert>
      ) : null}

      <Surface className="flex flex-col gap-6 rounded-3xl p-6" variant="default">
        <div className="flex flex-col items-start gap-1">
          <h2 className="text-lg font-semibold text-foreground">{t("security.platformAudit.filtersTitle")}</h2>
        </div>
        <div>
          <form className="grid items-end gap-x-4 gap-y-5 md:grid-cols-2 lg:grid-cols-12" onSubmit={applyFilters}>
            <div className="min-w-0 lg:col-span-3">
              <FilterSelect
                label={t("security.platformAudit.scope")}
                placeholder={t("security.platformAudit.allScopes")}
                value={draftFilters.tenantId}
                options={[{ id: "", label: t("security.platformAudit.allScopes") }, ...tenantOptions]}
                isDisabled={tenantsLoading}
                onChange={(value) => updateDraft("tenantId", value)}
              />
              {tenantsLoading ? <p className="mt-1 text-xs text-muted">{t("security.platformAudit.tenantsLoading")}</p> : null}
            </div>
            <TextField className="min-w-0 lg:col-span-3" value={draftFilters.actorId} onChange={(value) => updateDraft("actorId", value)}>
              <Label>{t("security.platformAudit.actorId")}</Label>
              <Input placeholder={t("security.platformAudit.actorPlaceholder")} />
            </TextField>
            <TextField className="min-w-0 lg:col-span-3" value={draftFilters.eventId} onChange={(value) => updateDraft("eventId", value)}>
              <Label>{t("security.platformAudit.eventId")}</Label>
              <Input placeholder={t("security.platformAudit.eventIdPlaceholder")} />
            </TextField>
            <div className="min-w-0 lg:col-span-3">
              <FilterSelect
                label={t("security.platformAudit.category")}
                placeholder={t("security.platformAudit.allCategories")}
                value={draftFilters.category}
                options={[{ id: "", label: t("security.platformAudit.allCategories") }, ...categoryOptions]}
                isDisabled={draftFilters.uncataloguedOnly || catalogQuery.isLoading}
                onChange={selectCategory}
              />
            </div>
            {draftFilters.uncataloguedOnly ? (
              <TextField className="min-w-0 lg:col-span-3" value={draftFilters.eventName} onChange={(value) => updateDraft("eventName", value)}>
                <Label>{t("security.platformAudit.eventExact")}</Label>
                <Input placeholder={t("security.platformAudit.eventFallbackPlaceholder")} />
              </TextField>
            ) : (
              <div className="min-w-0 lg:col-span-3">
                <FilterSelect
                  label={t("security.platformAudit.event")}
                  placeholder={t("security.platformAudit.allEvents")}
                  value={draftFilters.eventName}
                  options={[{ id: "", label: t("security.platformAudit.allEvents") }, ...eventOptions]}
                  isDisabled={catalogQuery.isLoading || !draftFilters.category}
                  onChange={selectEvent}
                />
              </div>
            )}
            {draftFilters.uncataloguedOnly ? (
              <TextField className="min-w-0 lg:col-span-3" value={draftFilters.operation} onChange={(value) => updateDraft("operation", value)}>
                <Label>{t("security.platformAudit.operationExact")}</Label>
                <Input placeholder={t("security.platformAudit.operationFallbackPlaceholder")} />
              </TextField>
            ) : (
              <div className="min-w-0 lg:col-span-3">
                <FilterSelect
                  label={t("security.platformAudit.operation")}
                  placeholder={t("security.platformAudit.allOperations")}
                  value={draftFilters.operation}
                  options={[{ id: "", label: t("security.platformAudit.allOperations") }, ...operationOptions]}
                  isDisabled={catalogQuery.isLoading || !draftFilters.eventName || operationOptions.length === 0}
                  onChange={(value) => updateDraft("operation", value)}
                />
              </div>
            )}
            <TextField className="min-w-0 lg:col-span-3" value={draftFilters.outcome} onChange={(value) => updateDraft("outcome", value)}>
              <Label>{t("security.platformAudit.outcome")}</Label>
              <Input placeholder={t("security.platformAudit.outcomePlaceholder")} />
            </TextField>
            <div className="flex min-w-0 items-end lg:col-span-3">
              <Checkbox aria-label={t("security.platformAudit.uncataloguedOnly")} isSelected={draftFilters.uncataloguedOnly} onChange={toggleUncatalogued}>
                <Checkbox.Content>
                  <Checkbox.Control><Checkbox.Indicator /></Checkbox.Control>
                  <span>
                    <span className="block text-sm font-medium">{t("security.platformAudit.uncataloguedOnly")}</span>
                    <span className="block text-xs text-muted">{t("security.platformAudit.uncataloguedDescription")}</span>
                  </span>
                </Checkbox.Content>
              </Checkbox>
            </div>
            <div className="min-w-0 lg:col-span-3"><AuditDateFilter label={t("security.platformAudit.fromDate")} value={draftFilters.fromDate} onChange={(value) => updateDraft("fromDate", value)} onClear={() => updateDraft("fromDate", "")} clearLabel={t("security.platformAudit.clearDate")} locale={locale} /></div>
            <div className="min-w-0 lg:col-span-3"><AuditDateFilter label={t("security.platformAudit.toDate")} value={draftFilters.toDate} onChange={(value) => updateDraft("toDate", value)} onClear={() => updateDraft("toDate", "")} clearLabel={t("security.platformAudit.clearDate")} locale={locale} /></div>
            <div className="flex min-w-0 items-end gap-2 md:col-span-2 lg:col-span-3">
              <FilterSelect
                label={t("security.platformAudit.sortBy")}
                placeholder={t("security.platformAudit.sortOccurredAt")}
                value={String(sortBy)}
                options={(Object.values(auditSortFields) as SecurityAuditSortField[]).map((field) => ({ id: String(field), label: sortLabel(field, t) }))}
                onChange={(value) => { setSortBy(Number(value) as SecurityAuditSortField); setPage(1); }}
              />
              <Tooltip>
                <Button
                  aria-label={descending ? t("security.platformAudit.sortDescending") : t("security.platformAudit.sortAscending")}
                  isIconOnly
                  type="button"
                  variant="outline"
                  onPress={() => { setDescending((value) => !value); setPage(1); }}
                >
                  <FaIcon icon={descending ? "arrow-down-a-z" : "arrow-up-a-z"} aria-hidden="true" />
                </Button>
                <Tooltip.Content>{descending ? t("security.platformAudit.sortDescending") : t("security.platformAudit.sortAscending")}</Tooltip.Content>
              </Tooltip>
            </div>
            <div className="flex items-end gap-2 md:col-span-2 lg:col-span-3">
              <Tooltip>
                <Button type="submit" variant="primary">
                  <FaIcon icon="filter" aria-hidden="true" />
                  {t("security.platformAudit.applyFilters")}
                </Button>
                <Tooltip.Content>{t("security.platformAudit.applyFilters")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button aria-label={t("security.platformAudit.clearFilters")} type="button" variant="outline" onPress={clearFilters}>
                  <FaIcon icon="rotate-left" aria-hidden="true" />
                  <span className="hidden sm:inline">{t("security.platformAudit.clearFilters")}</span>
                </Button>
                <Tooltip.Content>{t("security.platformAudit.clearFilters")}</Tooltip.Content>
              </Tooltip>
            </div>
          </form>
          {dateRangeError ? <p className="mt-3 text-sm text-danger" role="alert">{t("security.platformAudit.invalidDateRange")}</p> : null}
          {filters.uncataloguedOnly ? <p className="mt-3 text-sm text-muted">{t("security.platformAudit.uncataloguedApplied")}</p> : null}
          {activeFilterChips.length > 0 ? (
            <div className="mt-8 flex flex-wrap items-center gap-2 border-t border-separator pt-5" aria-label={t("security.platformAudit.activeFilters")}>
              <span className="text-xs font-medium text-muted">{t("security.platformAudit.activeFilters")}</span>
              {activeFilterChips.map((chip) => (
                <div key={chip.key} className="flex items-center gap-0.5">
                  <Chip color={chip.color} size="sm" variant="soft"><Chip.Label>{chip.label}</Chip.Label></Chip>
                  <Tooltip>
                    <Button
                      aria-label={t("security.platformAudit.removeFilter", { filter: chip.label })}
                      isIconOnly
                      size="sm"
                      type="button"
                      variant="ghost"
                      onPress={() => removeFilter(chip.key)}
                    >
                      <FaIcon icon="xmark" aria-hidden="true" />
                    </Button>
                    <Tooltip.Content>{t("security.platformAudit.removeFilter", { filter: chip.label })}</Tooltip.Content>
                  </Tooltip>
                </div>
              ))}
            </div>
          ) : null}
        </div>
      </Surface>

      <Surface className="flex flex-col gap-6 rounded-3xl p-6">
          <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-lg font-semibold text-foreground">{t("security.platformAudit.eventsTitle")}</h2>
            <p className="text-sm text-muted">{t("security.platformAudit.eventsCount", { count: formatNumber(totalCount, locale) })}</p>
          </div>
          <div className="flex items-center gap-2">
            {query.isFetching && !query.isPending ? <Spinner aria-label={t("security.platformAudit.loading")} size="sm" /> : null}
            <Tooltip>
              <Button aria-label={t("security.platformAudit.export")} isDisabled={isExporting || query.isPending || Boolean(filtersInvalid)} onPress={() => void exportRows()} variant="secondary">
                <FaIcon icon="download" aria-hidden="true" />
                <span className="hidden sm:inline">{isExporting ? t("security.platformAudit.exporting") : t("security.platformAudit.export")}</span>
              </Button>
              <Tooltip.Content>{isExporting ? t("security.platformAudit.exporting") : t("security.platformAudit.export")}</Tooltip.Content>
            </Tooltip>
          </div>
        </div>
        <div>
          {query.isPending ? (
            <div className="flex justify-center py-16"><Spinner aria-label={t("security.platformAudit.loading")} /></div>
          ) : query.isError ? (
            <Alert status="danger">
              <Alert.Indicator />
              <Alert.Content>
                <Alert.Title>{errorMessage(query.error, t)}</Alert.Title>
                <Alert.Description>{t("security.platformAudit.retryDescription")}</Alert.Description>
              </Alert.Content>
            </Alert>
          ) : (
            <DataGrid
              aria-label={t("security.platformAudit.eventsTitle")}
              columns={columns}
              contentClassName="min-w-[1100px]"
              data={result?.items ?? []}
              getRowId={(item) => item.id ?? item.correlationId ?? `${item.occurredAt ?? "event"}-${item.eventName ?? "unknown"}`}
              renderEmptyState={() => <p className="p-10 text-center text-sm text-muted">{t("security.platformAudit.empty")}</p>}
              scrollContainerClassName="w-full max-w-full overflow-x-auto"
              variant="secondary"
              onRowAction={(key) => {
                const event = result?.items.find((item) => item.id === String(key));
                if (event) setSelectedEvent(event);
              }}
            />
          )}
        </div>
        <div className="flex items-center justify-between gap-3">
          <span className="text-sm text-muted">{t("security.platformAudit.page", { page: formatNumber(page, locale), totalPages: formatNumber(totalPages, locale) })}</span>
          <div className="flex gap-2">
            <Tooltip>
              <Button aria-label={t("security.platformAudit.previous")} isDisabled={page <= 1 || query.isFetching} type="button" variant="outline" onPress={() => setPage((value) => Math.max(1, value - 1))}>
                <FaIcon icon={direction === "rtl" ? "arrow-right" : "arrow-left"} aria-hidden="true" />
                <span className="hidden sm:inline">{t("security.platformAudit.previous")}</span>
              </Button>
              <Tooltip.Content>{t("security.platformAudit.previous")}</Tooltip.Content>
            </Tooltip>
            <Tooltip>
              <Button aria-label={t("security.platformAudit.next")} isDisabled={page >= totalPages || query.isFetching} type="button" variant="outline" onPress={() => setPage((value) => Math.min(totalPages, value + 1))}>
                <span className="hidden sm:inline">{t("security.platformAudit.next")}</span>
                <FaIcon icon={direction === "rtl" ? "arrow-left" : "arrow-right"} aria-hidden="true" />
              </Button>
              <Tooltip.Content>{t("security.platformAudit.next")}</Tooltip.Content>
            </Tooltip>
          </div>
        </div>
      </Surface>

      <EventDetailSheet event={selectedEvent} onClose={() => setSelectedEvent(null)} />
    </div>
  );
}

function EventDetailSheet({ event, onClose }: { event: SecurityAuditEventReadDto | null; onClose: () => void }) {
  const { t, locale } = useLocalizedText();
  return <PlatformAuditDetailSheet event={event} onClose={onClose} locale={locale} t={t} />;
}

function PlatformAuditDetailSheet({
  event,
  onClose,
  locale,
  t,
}: {
  event: SecurityAuditEventReadDto | null;
  onClose: () => void;
  locale: SupportedLocale;
  t: ReturnType<typeof useLocalizedText>["t"];
}) {
  return (
    <Sheet isOpen={Boolean(event)} placement="right" onOpenChange={(isOpen) => !isOpen && onClose()}>
      <Sheet.Backdrop variant="blur">
        <Sheet.Content className="w-full max-w-xl">
          <Sheet.Dialog className="flex h-full max-h-screen flex-col">
            <Sheet.Header>
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.16em] text-accent">{t("security.platformAudit.detailEyebrow")}</p>
                <Sheet.Heading className="mt-2">{t("security.platformAudit.detailTitle")}</Sheet.Heading>
              </div>
              <Tooltip>
                <Sheet.CloseTrigger aria-label={t("security.platformAudit.closeDetail")}>
                  <FaIcon icon="xmark" aria-hidden="true" />
                </Sheet.CloseTrigger>
                <Tooltip.Content>{t("security.platformAudit.closeDetail")}</Tooltip.Content>
              </Tooltip>
            </Sheet.Header>
            <Sheet.Body className="min-h-0 flex-1 overflow-y-auto">
              {event ? (
                <>
                  <dl>
                    <DetailField label={t("security.platformAudit.id")} value={displayValue(event.id, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.event")} value={localizePlatformAuditEvent(locale, event.eventName, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.category")} value={localizePlatformAuditCategory(locale, event.eventCategory, t("security.platformAudit.uncatalogued"))} />
                    <DetailField label={t("security.platformAudit.operation")} value={displayValue(event.operation, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.outcome")} value={localizeSecurityAuditOutcome(locale, event.outcome, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.actorKind")} value={displayValue(event.actorKind, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.actor")} value={displayValue(event.actorId, t("security.platformAudit.unknownActor"))} />
                    <DetailField label={t("security.platformAudit.subject")} value={displayValue(event.subjectId, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.scope")} value={displayValue(event.tenantId, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.resource")} value={displayValue(event.resource, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.transferredFile")} value={<bdi>{describeTransferredFile(event, locale) ?? t("security.platformAudit.notAvailable")}</bdi>} />
                    <DetailField label={t("security.platformAudit.occurredAt")} value={event.occurredAt ? formatDateTime(event.occurredAt, locale) : t("security.platformAudit.notAvailable")} />
                    <DetailField label={t("security.platformAudit.correlationId")} value={displayValue(event.correlationId, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.sourceBoundary")} value={displayValue(event.sourceBoundary, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.sourceIpKind")} value={displayValue(event.sourceIpKind, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.sourceIpAddress")} value={displayValue(event.sourceIpAddress, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.redactedReason")} value={displayValue(event.redactedReason, t("security.platformAudit.notAvailable"))} />
                    <DetailField label={t("security.platformAudit.changes")} value={<AuditChangeList changes={event.changes ?? []} locale={locale} t={t} />} />
                  </dl>
                  <p className="mt-6 border-t border-separator pt-5 text-xs leading-5 text-muted">{t("security.platformAudit.detailReadOnly")}</p>
                </>
              ) : null}
            </Sheet.Body>
          </Sheet.Dialog>
        </Sheet.Content>
      </Sheet.Backdrop>
    </Sheet>
  );
}
