"use client";

import * as React from "react";
import { Button, Card, Chip, Disclosure, Tooltip } from "@heroui/react";
import {
  formatDateTime,
  formatFileSize,
  formatNumber,
  useLocalizedText,
} from "@vitarish/web-localization";
import { Routes } from "../app/routes";

export type ProjectManagementTextPart = { type: "text"; text: string };
export type ProjectManagementCardPart = {
  type: "resource-card";
  family: string;
  operation: string;
  id?: string;
  parentId?: string;
  routeKind: string;
  routeCoordinates: Record<string, string>;
  capturedAt: string;
  fields: Record<string, unknown>;
  items: Array<Record<string, unknown>>;
};
export type ProjectManagementDisplayPart =
  ProjectManagementTextPart | ProjectManagementCardPart;

export function ProjectManagementMessageParts({
  parts,
  onOpen,
}: {
  parts: ProjectManagementDisplayPart[];
  onOpen?: (route: string) => void;
}) {
  return (
    <div className="space-y-4">
      {parts.map((part, index) =>
        part.type === "text" ? (
          <p
            key={`text-${index}`}
            dir="auto"
            className="whitespace-pre-wrap leading-7"
          >
            {part.text}
          </p>
        ) : (
          <ResourceCard
            key={`${part.operation}-${part.id ?? index}`}
            part={part}
            onOpen={onOpen}
          />
        ),
      )}
    </div>
  );
}

function ResourceCard({
  part,
  onOpen,
}: {
  part: ProjectManagementCardPart;
  onOpen?: (route: string) => void;
}) {
  const { t, locale } = useLocalizedText();
  const route = resourceRoute(part);
  const collection = part.items.length > 0;
  const content = collection ? (
    <ResourceList family={part.family} items={part.items} locale={locale} />
  ) : (
    <ResourceSummary family={part.family} data={part.fields} locale={locale} />
  );

  return (
    <Card className="w-full" variant="secondary">
      <Card.Header className="flex-row items-start justify-between gap-4 pb-2">
        <div className="min-w-0 space-y-1">
          <Card.Title className="flex items-center gap-2 text-sm font-semibold">
            <i
              className={`fa-light ${iconFor(part.family)} text-muted`}
              aria-hidden="true"
            />
            {t(`pm.cards.${part.family}`)}
          </Card.Title>
          <Card.Description>
            {t("pm.cards.viewed")} · {formatDate(part.capturedAt, locale)}
          </Card.Description>
        </div>
        {route && onOpen ? (
          <Tooltip>
            <Button size="sm" variant="ghost" onPress={() => onOpen(route)}>
              <i
                className="fa-light fa-arrow-up-right-from-square"
                aria-hidden="true"
              />
              {t("pm.cards.open")}
            </Button>
            <Tooltip.Content>{t("pm.cards.open")}</Tooltip.Content>
          </Tooltip>
        ) : null}
      </Card.Header>
      <Card.Content className="pt-2">
        {collection ? (
          <Disclosure defaultExpanded={part.items.length <= 5}>
            <Disclosure.Trigger>
              <span className="text-sm font-medium">
                {t("pm.cards.itemCount", {
                  count: formatNumber(
                    part.items.length,
                    locale === "fa" ? "fa" : "en",
                  ),
                })}
              </span>
              <Disclosure.Indicator />
            </Disclosure.Trigger>
            <Disclosure.Content>
              <Disclosure.Body className="pt-2">{content}</Disclosure.Body>
            </Disclosure.Content>
          </Disclosure>
        ) : (
          content
        )}
      </Card.Content>
    </Card>
  );
}

function ResourceList({
  family,
  items,
  locale,
}: {
  family: string;
  items: Array<Record<string, unknown>>;
  locale: string;
}) {
  return (
    <ul className="divide-y divide-separator">
      {items.map((item, index) => (
        <li key={index} className="py-3 first:pt-0 last:pb-0">
          <ResourceSummary
            family={family}
            data={item}
            locale={locale}
            compact
          />
        </li>
      ))}
    </ul>
  );
}

function ResourceSummary({
  family,
  data,
  locale,
  compact = false,
}: {
  family: string;
  data: Record<string, unknown>;
  locale: string;
  compact?: boolean;
}) {
  const { t } = useLocalizedText();
  const title = textValue(
    data,
    family === "attachment"
      ? "fileName"
      : family === "reminder"
        ? "subject"
        : "title",
    "name",
    "type",
  );
  const description = textValue(
    data,
    family === "reminder" ? "textBody" : "description",
    "preview",
  );
  const metadata = resourceMetadata(family, data, locale, t);

  if (family === "statistics")
    return <Statistics data={data} locale={locale} />;
  if (family === "status") return <StatusSummary data={data} />;

  return (
    <div className={compact ? "space-y-1.5" : "space-y-3"}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          {title ? (
            <p
              dir="auto"
              className="break-words text-sm font-medium text-foreground"
            >
              {title}
            </p>
          ) : null}
          {description ? (
            <p
              dir="auto"
              className="mt-1 line-clamp-2 text-sm leading-6 text-muted"
            >
              {description}
            </p>
          ) : null}
        </div>
        {family === "event" ? (
          <EventStatus committed={data.isCommitted} />
        ) : null}
      </div>
      {metadata.length > 0 ? (
        <dl className="flex flex-wrap gap-x-5 gap-y-2 text-xs text-muted">
          {metadata.map((item) => (
            <div
              key={item.label}
              className="flex min-w-0 items-baseline gap-1.5"
            >
              <dt>{item.label}</dt>
              <dd dir="auto" className="font-medium text-foreground">
                {item.value}
              </dd>
            </div>
          ))}
        </dl>
      ) : null}
    </div>
  );
}

function EventStatus({ committed }: { committed: unknown }) {
  const { t } = useLocalizedText();
  return committed === true ? (
    <Chip color="success" size="sm" variant="soft">
      {t("pm.cards.committed")}
    </Chip>
  ) : (
    <Chip color="default" size="sm" variant="soft">
      {t("pm.cards.draft")}
    </Chip>
  );
}

function Statistics({
  data,
  locale,
}: {
  data: Record<string, unknown>;
  locale: string;
}) {
  const { t } = useLocalizedText();
  const keys = [
    "projects",
    "procedures",
    "events",
    "roles",
    "attachments",
    "reminders",
  ] as const;
  return (
    <dl className="grid grid-cols-2 gap-x-6 gap-y-3 sm:grid-cols-3">
      {keys.flatMap((key) =>
        typeof data[key] === "number"
          ? [
              <div key={key}>
                <dt className="text-xs text-muted">{t(`sea.stats.${key}`)}</dt>
                <dd className="mt-0.5 text-base font-semibold tabular-nums">
                  {formatNumber(data[key], locale === "fa" ? "fa" : "en")}
                </dd>
              </div>,
            ]
          : [],
      )}
    </dl>
  );
}

function StatusSummary({ data }: { data: Record<string, unknown> }) {
  const { t } = useLocalizedText();
  const healthy =
    data.succeeded === true || data.canRead === true || data.canManage === true;
  return (
    <div className="flex items-center gap-2">
      <i
        className={`fa-light ${healthy ? "fa-circle-check text-success" : "fa-circle-xmark text-danger"}`}
        aria-hidden="true"
      />
      <span className="text-sm font-medium">
        {healthy ? t("pm.cards.available") : t("pm.cards.unavailable")}
      </span>
    </div>
  );
}

function resourceMetadata(
  family: string,
  data: Record<string, unknown>,
  locale: string,
  t: (key: string) => string,
): Array<{ label: string; value: string }> {
  const metadata: Array<{ label: string; value: string }> = [];
  const updatedAt = dateValue(data.updatedAt, locale);
  const createdAt = dateValue(data.createdAt, locale);
  if (updatedAt)
    metadata.push({ label: t("pm.cards.updated"), value: updatedAt });
  else if (createdAt)
    metadata.push({ label: t("pm.cards.created"), value: createdAt });
  if (family === "attachment") {
    const contentType = textValue(data, "contentType");
    const size =
      typeof data.size === "number"
        ? formatFileSize(data.size, locale === "fa" ? "fa" : "en")
        : undefined;
    if (contentType)
      metadata.push({ label: t("pm.cards.fileType"), value: contentType });
    if (size) metadata.push({ label: t("pm.cards.size"), value: size });
  }
  if (family === "reminder") {
    const remindAt = dateValue(data.remindAt, locale);
    if (remindAt)
      metadata.push({ label: t("pm.cards.reminderAt"), value: remindAt });
    if (typeof data.isSent === "boolean")
      metadata.push({
        label: t("pm.cards.sent"),
        value: data.isSent ? t("pm.cards.sent") : t("pm.cards.notSent"),
      });
  }
  if (family === "role") {
    const access =
      data.isWrite === true
        ? t("pm.cards.write")
        : data.isWrite === false
          ? t("pm.cards.read")
          : textValue(data, "type");
    if (access) metadata.push({ label: t("pm.cards.access"), value: access });
  }
  return metadata;
}

function textValue(
  data: Record<string, unknown>,
  ...keys: string[]
): string | undefined {
  for (const key of keys) {
    const value = data[key];
    if (typeof value === "string" && value.trim()) return value;
    if (
      typeof value === "object" &&
      value !== null &&
      "text" in value &&
      typeof (value as { text?: unknown }).text === "string"
    )
      return (value as { text: string }).text;
  }
  return undefined;
}

function dateValue(value: unknown, locale: string): string | undefined {
  return typeof value === "string" && /[T ]\d{2}:\d{2}/.test(value)
    ? formatDate(value, locale)
    : undefined;
}

function formatDate(value: string, locale: string): string {
  try {
    return formatDateTime(value, locale === "fa" ? "fa" : "en");
  } catch {
    return value;
  }
}

function iconFor(family: string): string {
  return (
    (
      {
        project: "fa-folder-tree",
        procedure: "fa-list-check",
        event: "fa-calendar-check",
        attachment: "fa-paperclip",
        reminder: "fa-bell",
        role: "fa-shield-halved",
        statistics: "fa-chart-simple",
        status: "fa-heart-pulse",
      } as Record<string, string>
    )[family] ?? "fa-circle-info"
  );
}

function resourceRoute(part: ProjectManagementCardPart): string | null {
  const coordinates = part.routeCoordinates;
  if (part.routeKind === "project" && part.id) return Routes.Project(part.id);
  if (part.routeKind === "procedure" && part.id && coordinates.projectId)
    return Routes.Procedure(coordinates.projectId, part.id);
  if (
    part.routeKind === "event" &&
    part.id &&
    coordinates.projectId &&
    coordinates.procedureId
  )
    return Routes.Event(
      coordinates.projectId,
      coordinates.procedureId,
      part.id,
    );
  if (
    part.routeKind === "event" &&
    part.parentId &&
    coordinates.projectId &&
    coordinates.procedureId
  )
    return Routes.Event(
      coordinates.projectId,
      coordinates.procedureId,
      part.parentId,
    );
  if (part.routeKind === "roles") return Routes.Roles;
  if (part.routeKind === "statistics") return Routes.Statistics;
  return null;
}
