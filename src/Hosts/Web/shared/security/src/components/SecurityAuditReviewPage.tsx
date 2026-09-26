"use client";

import { Alert, Button, Card, Dropdown, Input, Label, Spinner, TextField, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useMemo, useState } from "react";
import { useAuditSecurityEventsList } from "../api/generated/audit/audit";
import type { AuditSecurityEventsListParams } from "../api/generated/audit/model";
import { formatDateTime, formatNumber, localizeApiError, useLocalizedText } from "@vitarish/localization";

const pageSize = 25;
// OpenAPI currently emits the C# SecurityAuditSortField enum as an integer.
const auditSortFields = {
  actor: 0,
  occurredAt: 1,
  eventType: 2,
  actorKind: 3,
  sourceIpKind: 4,
  sourceIpAddress: 5,
} as const;
type AuditSortField = typeof auditSortFields[keyof typeof auditSortFields];

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

function outcomeLabel(outcome: string | null | undefined, t: ReturnType<typeof useLocalizedText>["t"]): string {
  switch (outcome) {
    case "succeeded":
      return t("security.audit.outcomeSucceeded");
    case "failed":
      return t("security.audit.outcomeFailed");
    case "denied":
      return t("security.audit.outcomeDenied");
    case "pending":
      return t("security.audit.outcomePending");
    case "started":
      return t("security.audit.outcomeStarted");
    default:
      return outcome ?? "—";
  }
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
      <div>
        <h1 className="text-2xl font-bold">{t("security.audit.title")}</h1>
        <p className="text-sm text-muted">{t("security.audit.description")}</p>
      </div>

      {result?.capacityThresholdReached ? (
        <Alert status="warning">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("security.audit.capacityWarning")}</Alert.Title>
          </Alert.Content>
        </Alert>
      ) : null}

      <Card>
        <Card.Header>
          <Card.Title>{t("security.audit.filtersTitle")}</Card.Title>
          <Card.Description>{t("security.audit.filtersDescription")}</Card.Description>
        </Card.Header>
        <Card.Content>
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
            <TextField value={fromDate} onChange={setFromDate}>
              <Label>{t("security.audit.fromDate")}</Label>
              <Input type="date" />
            </TextField>
            <TextField value={toDate} onChange={setToDate}>
              <Label>{t("security.audit.toDate")}</Label>
              <Input type="date" />
            </TextField>
            <div className="flex items-end gap-2">
              <Tooltip>
                <Dropdown>
                  <Dropdown.Trigger aria-label={t("security.audit.sortBy")} className="flex-1">
                    <FaIcon icon="arrow-down-a-z" aria-hidden="true" />
                    <span className="truncate">{sortLabel(sortBy, t)}</span>
                  </Dropdown.Trigger>
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
        </Card.Content>
      </Card>

      <Card>
        <Card.Header className="flex-row items-start justify-between gap-3">
          <div>
            <Card.Title>{t("security.audit.eventsTitle")}</Card.Title>
            <Card.Description>{t("security.audit.eventsCount", { count: formatNumber(totalCount, locale) })}</Card.Description>
          </div>
          <FaIcon icon="shield-halved" className="text-primary" aria-hidden="true" />
        </Card.Header>
        <Card.Content>
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
              <table className="w-full min-w-[760px] text-left text-sm">
                <thead className="border-b text-xs text-muted">
                  <tr>
                    <th className="px-3 py-3 font-medium">{t("security.audit.eventName")}</th>
                    <th className="px-3 py-3 font-medium">{t("security.audit.actor")}</th>
                    <th className="px-3 py-3 font-medium">{t("security.audit.outcome")}</th>
                    <th className="px-3 py-3 font-medium">{t("security.audit.sourceIp")}</th>
                    <th className="px-3 py-3 font-medium">{t("security.audit.occurredAt")}</th>
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((item) => (
                    <tr key={item.id} className="border-b last:border-0">
                      <td className="px-3 py-3 font-medium">{item.eventName ?? "—"}</td>
                      <td className="px-3 py-3">{item.actorId ?? item.actorKind ?? t("security.audit.unknownActor")}</td>
                      <td className="px-3 py-3">{outcomeLabel(item.outcome, t)}</td>
                      <td className="px-3 py-3">{item.sourceIpAddress ?? item.sourceIpKind ?? "—"}</td>
                      <td className="px-3 py-3 whitespace-nowrap">{item.occurredAt ? formatDateTime(item.occurredAt, locale) : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="rounded-lg border border-dashed p-8 text-center text-muted">{t("security.audit.empty")}</p>
          )}
        </Card.Content>
        <Card.Footer className="flex items-center justify-between gap-3">
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
        </Card.Footer>
      </Card>
    </div>
  );
}
