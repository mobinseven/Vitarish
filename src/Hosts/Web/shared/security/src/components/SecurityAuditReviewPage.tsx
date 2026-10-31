"use client";

import { Alert, Button, Chip, Dropdown, Input, Label, Spinner, Surface, TextField, Tooltip } from "@heroui/react";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { useMemo, useState } from "react";
import { useAuditSecurityEventsList } from "../api/generated/audit/audit";
import type { AuditSecurityEventsListParams } from "../api/generated/audit/model";
import { formatDateTime, formatNumber, localizeApiError, useLocalizedText } from "@vitarish/localization";
import { localizeSecurityAuditOutcome } from "../localization";
import { TransferredFileDescription } from "./transferredFile";
import { AuditDateFilter } from "./AuditDateFilter";
import { auditOutcomeColor } from "./auditOutcomeColor";
import { auditSortFields, type AuditSortField } from "../api/securityAuditSortFields";

const pageSize = 25;
function utcBoundary(value: string, endOfDay: boolean): string | undefined {
  if (!value) return undefined;
  return new Date(`${value}T${endOfDay ? "23:59:59.999" : "00:00:00.000"}Z`).toISOString();
}

function errorMessage(error: unknown, t: ReturnType<typeof useLocalizedText>["t"]): string {
  const status = typeof error === "object" && error !== null && "status" in error
    ? Number((error as { status?: number | string }).status)
    : 0;
  if (status === 503) return t("security.audit.unavailable");
  return localizeApiError(error, t);
}

function sortLabel(field: AuditSortField, t: ReturnType<typeof useLocalizedText>["t"]): string {
  switch (field) {
    case auditSortFields.actor:
      return t("security.audit.sortActor");
    case auditSortFields.actorKind:
      return t("security.audit.sortActorKind");
    case auditSortFields.sourceIpKind:
      return t("security.audit.sortSourceIpKind");
    case auditSortFields.eventType:
      return t("security.audit.sortEventType");
    case auditSortFields.sourceIpAddress:
      return t("security.audit.sortSourceIpAddress");
    default:
      return t("security.audit.sortOccurredAt");
  }
}

function outcomeLabel(outcome: string | null | undefined, locale: Parameters<typeof localizeSecurityAuditOutcome>[0]): string {
  return localizeSecurityAuditOutcome(locale, outcome, "—");
}

export function SecurityAuditReviewPage() {
  const { t, locale, direction } = useLocalizedText();
  const [actorId, setActorId] = useState("");
  const [eventName, setEventName] = useState("");
  const [eventCategory, setEventCategory] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState<AuditSortField>(auditSortFields.occurredAt);
  const [descending, setDescending] = useState(true);
  const [filters, setFilters] = useState({ actorId: "", eventName: "", eventCategory: "", fromDate: "", toDate: "" });

  const params = useMemo<AuditSecurityEventsListParams>(() => ({
    page,
    pageSize,
    actorId: filters.actorId || undefined,
    eventName: filters.eventName || undefined,
    eventCategory: filters.eventCategory || undefined,
    fromUtc: utcBoundary(filters.fromDate, false),
    toUtc: utcBoundary(filters.toDate, true),
    sortBy,
    descending,
  }), [descending, filters, page, sortBy]);
  const query = useAuditSecurityEventsList(params);
  const result = query.data;
  const totalCount = Number(result?.totalCount ?? 0);
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const applyFilters = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPage(1);
    setFilters({ actorId, eventName, eventCategory, fromDate, toDate });
  };

  return (
    <div className="mx-auto flex w-full max-w-6xl flex-col gap-6 pt-8">
      <header className="flex items-center gap-4">
        <HeaderIcon icon="shield-halved" level="page" />
        <div>
          <h1 className="text-2xl font-bold text-foreground">{t("security.audit.title")}</h1>
          <p className="text-sm text-muted">{t("security.audit.description")}</p>
        </div>
      </header>

      {result?.capacityThresholdReached ? (
        <Alert status="warning">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("security.audit.capacityWarning")}</Alert.Title>
          </Alert.Content>
        </Alert>
      ) : null}

      <Surface className="flex flex-col gap-6 rounded-3xl p-6">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="filter" />
          <div>
            <h2 className="text-lg font-semibold text-foreground">{t("security.audit.filtersTitle")}</h2>
            <p className="text-sm text-muted">{t("security.audit.filtersDescription")}</p>
          </div>
        </div>
          <form className="grid gap-4 md:grid-cols-2 lg:grid-cols-3" onSubmit={applyFilters}>
            <TextField value={actorId} onChange={setActorId}>
              <Label>{t("security.audit.actor")}</Label>
              <Input />
            </TextField>
            <TextField value={eventName} onChange={setEventName}>
              <Label>{t("security.audit.eventName")}</Label>
              <Input />
            </TextField>
            <TextField value={eventCategory} onChange={setEventCategory}>
              <Label>{t("security.audit.eventCategory")}</Label>
              <Input />
            </TextField>
            <AuditDateFilter label={t("security.audit.fromDate")} value={fromDate} onChange={setFromDate} onClear={() => setFromDate("")} clearLabel={t("security.audit.clearDate")} locale={locale} />
            <AuditDateFilter label={t("security.audit.toDate")} value={toDate} onChange={setToDate} onClear={() => setToDate("")} clearLabel={t("security.audit.clearDate")} locale={locale} />
            <div className="flex flex-wrap items-end gap-2">
              <Tooltip>
                <Dropdown>
                  <Button aria-label={t("security.audit.sortBy")} className="flex-1" variant="secondary">
                    <FaIcon icon="arrow-down-a-z" aria-hidden="true" className="text-accent" />
                    <span className="truncate">{sortLabel(sortBy, t)}</span>
                  </Button>
                  <Dropdown.Popover>
                    <Dropdown.Menu aria-label={t("security.audit.sortBy")} onAction={(key) => {
                      const nextSort = Number(key) as AuditSortField;
                      if (nextSort === sortBy) {
                        setDescending((value) => !value);
                      } else {
                        setSortBy(nextSort);
                        setDescending(true);
                      }
                      setPage(1);
                    }}>
                      {(Object.values(auditSortFields) as AuditSortField[]).map((field) => (
                        <Dropdown.Item key={field} id={String(field)} textValue={sortLabel(field, t)}>
                          {sortLabel(field, t)}{sortBy === field ? (descending ? ` (${t("security.audit.sortDescending")})` : ` (${t("security.audit.sortAscending")})`) : ""}
                        </Dropdown.Item>
                      ))}
                    </Dropdown.Menu>
                  </Dropdown.Popover>
                </Dropdown>
                <Tooltip.Content>{t("security.audit.sortBy")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button
                  variant="outline"
                  aria-label={descending ? t("security.audit.sortDescending") : t("security.audit.sortAscending")}
                  onPress={() => {
                    setDescending((value) => !value);
                    setPage(1);
                  }}
                >
                  <FaIcon icon={descending ? "arrow-down-a-z" : "arrow-up-a-z"} aria-hidden="true" />
                </Button>
                <Tooltip.Content>{descending ? t("security.audit.sortDescending") : t("security.audit.sortAscending")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button type="submit" className="flex-1">
                  <FaIcon icon="filter" aria-hidden="true" />
                  {t("security.audit.applyFilters")}
                </Button>
                <Tooltip.Content>{t("security.audit.applyFilters")}</Tooltip.Content>
              </Tooltip>
            </div>
          </form>
      </Surface>

      <Surface className="flex flex-col gap-6 rounded-3xl p-6">
        <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3">
            <HeaderIcon icon="list-check" tone="success" />
            <h2 className="text-lg font-semibold text-foreground">{t("security.audit.eventsTitle")}</h2>
          </div>
          <Chip color="accent" variant="soft">
            <Chip.Label>{t("security.audit.eventsCount", { count: formatNumber(totalCount, locale) })}</Chip.Label>
          </Chip>
        </div>
          {query.isPending ? (
            <div className="flex justify-center py-10"><Spinner aria-label={t("security.audit.loading")} /></div>
          ) : query.isError ? (
            <Alert status="danger">
              <Alert.Indicator />
              <Alert.Content>
                <Alert.Title>{errorMessage(query.error, t)}</Alert.Title>
                <Alert.Description>{t("security.audit.retryDescription")}</Alert.Description>
              </Alert.Content>
            </Alert>
          ) : result?.items.length ? (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[760px] text-start text-sm">
                <thead className="bg-accent-soft text-xs text-accent-soft-foreground">
                  <tr>
                    <th className="px-3 py-3 text-start font-semibold first:rounded-s-xl last:rounded-e-xl">{t("security.audit.eventName")}</th>
                    <th className="px-3 py-3 text-start font-semibold first:rounded-s-xl last:rounded-e-xl">{t("security.audit.actor")}</th>
                    <th className="px-3 py-3 text-start font-semibold first:rounded-s-xl last:rounded-e-xl">{t("security.audit.outcome")}</th>
                    <th className="px-3 py-3 text-start font-semibold first:rounded-s-xl last:rounded-e-xl">{t("security.audit.sourceIp")}</th>
                    <th className="px-3 py-3 text-start font-semibold first:rounded-s-xl last:rounded-e-xl">{t("security.audit.occurredAt")}</th>
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((item) => (
                    <tr key={item.id} className="border-b border-separator transition-colors last:border-0 hover:bg-surface-secondary">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-accent">
                        <span dir="ltr">{item.eventName ?? "—"}</span>
                        <TransferredFileDescription
                          event={item}
                          locale={locale}
                          className="mt-1 block font-sans text-sm font-normal text-muted"
                          accessibleLabel={t("security.audit.transferredFile")}
                        />
                      </td>
                      <td className="px-3 py-3 text-foreground">{item.actorId ?? item.actorKind ?? t("security.audit.unknownActor")}</td>
                      <td className="px-3 py-3">
                        <Chip color={auditOutcomeColor(item.outcome)} size="sm" variant="soft">
                          <Chip.Label>{outcomeLabel(item.outcome, locale)}</Chip.Label>
                        </Chip>
                      </td>
                      <td className="px-3 py-3 font-mono text-xs text-muted"><span dir="ltr">{item.sourceIpAddress ?? item.sourceIpKind ?? "—"}</span></td>
                      <td className="px-3 py-3 whitespace-nowrap text-muted">{item.occurredAt ? formatDateTime(item.occurredAt, locale) : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <Surface variant="secondary" className="flex flex-col items-center gap-3 rounded-2xl p-8 text-center text-muted">
              <FaIcon icon="magnifying-glass" className="text-2xl text-accent" aria-hidden="true" />
              <p>{t("security.audit.empty")}</p>
            </Surface>
          )}
        <div className="flex items-center justify-between gap-3">
          <span className="text-sm text-muted">{t("security.audit.page", { page: formatNumber(page, locale), totalPages: formatNumber(totalPages, locale) })}</span>
          <div className="flex gap-2">
            <Tooltip>
              <Button variant="outline" isDisabled={page <= 1 || query.isFetching} onPress={() => setPage((value) => Math.max(1, value - 1))}>
                <FaIcon icon={direction === "rtl" ? "arrow-right" : "arrow-left"} aria-hidden="true" />
                {t("security.audit.previous")}
              </Button>
              <Tooltip.Content>{t("security.audit.previous")}</Tooltip.Content>
            </Tooltip>
            <Tooltip>
              <Button variant="outline" isDisabled={page >= totalPages || query.isFetching} onPress={() => setPage((value) => Math.min(totalPages, value + 1))}>
                {t("security.audit.next")}
                <FaIcon icon={direction === "rtl" ? "arrow-left" : "arrow-right"} aria-hidden="true" />
              </Button>
              <Tooltip.Content>{t("security.audit.next")}</Tooltip.Content>
            </Tooltip>
          </div>
        </div>
      </Surface>
    </div>
  );
}
