"use client";

import { Button, Tooltip } from "@heroui/react";
import { ItemCard, PressableFeedback } from "@heroui-pro/react";
import { BarChart } from "@heroui-pro/react/bar-chart";
import { KPI } from "@heroui-pro/react/kpi";
import { Catalog, type ComponentApi } from "@a2ui/web_core";
import { createComponentImplementation, useSignalValue, type ReactComponentImplementation } from "@a2ui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { useMemo, type ReactNode } from "react";
import catalogDocument from "./assistant-catalog.json";
import { usePresentedSurfaceAppearance } from "./PresentedSurfaceContext";
import { asPresentedResult, formatPresentedValue, type PresentedRecord } from "./presentedData";

/** Rows a list shows before it links to the full screen; inline cards never scroll inside. */
export const presentedListLimit = 5;

/** Fields a card or row shows at most (inline card guidance: two or three facts). */
const presentedFieldLimit = 3;

type Props = Record<string, unknown>;
type Render = (props: { props: Props; buildChild: (id: string) => ReactNode }) => ReactNode;

function useRecordText(record: PresentedRecord, fields: readonly string[]) {
  const { locale } = useLocalizedText();
  const appearance = usePresentedSurfaceAppearance();
  const status = (value: string) => appearance.statusLabel?.(record.kind, value) ?? value;
  const title = formatPresentedValue(record.title, locale, status) ?? "";
  const facts = fields.slice(0, presentedFieldLimit).flatMap((field) => {
    const value = formatPresentedValue(record.fields[field], locale, status);
    return value ? [`${appearance.fieldLabel(record.kind, field)}: ${value}`] : [];
  });
  return { title, facts };
}

function stringList(value: unknown): string[] {
  return Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : [];
}

function RecordRow({ record, fields, variant }: { record: PresentedRecord; fields: readonly string[]; variant: "outline" | "secondary" }) {
  const { t } = useLocalizedText();
  const appearance = usePresentedSurfaceAppearance();
  const { title, facts } = useRecordText(record, fields);
  const openable = Boolean(appearance.openRecord) && (appearance.canOpen?.(record) ?? true);
  const icon = appearance.kindIcon?.(record.kind) ?? "file-lines";
  const content = (
    <>
      {openable ? <PressableFeedback.Highlight /> : null}
      <ItemCard.Icon className="bg-surface-secondary">
        <FaIcon icon={icon} aria-hidden="true" />
      </ItemCard.Icon>
      <ItemCard.Content>
        <ItemCard.Title>{title}</ItemCard.Title>
        {facts.length > 0 ? <ItemCard.Description>{facts.join(" · ")}</ItemCard.Description> : null}
      </ItemCard.Content>
    </>
  );
  if (!openable) {
    return <ItemCard className="w-full" variant={variant}>{content}</ItemCard>;
  }
  return (
    <Tooltip delay={400}>
      <ItemCard<"button">
        className="relative w-full cursor-pointer overflow-hidden text-start transition-colors hover:bg-default"
        variant={variant}
        aria-label={t("intelligence.surface.openRecord", { title })}
        render={(props) => (
          <button
            type="button"
            {...props}
            onClick={(event) => {
              props.onClick?.(event);
              appearance.openRecord?.(record);
            }}
          />
        )}
      >
        {content}
      </ItemCard>
      <Tooltip.Content>{t("intelligence.surface.openRecordTooltip")}</Tooltip.Content>
    </Tooltip>
  );
}

const renderers: Record<string, Render> = {
  Column: ({ props, buildChild }) => (
    <div className="flex w-full flex-col gap-3">{stringList(props.children).map((id) => <div key={id}>{buildChild(id)}</div>)}</div>
  ),
  Heading: ({ props }) => <h3 className="text-base font-semibold text-foreground">{String(props.text ?? "")}</h3>,
  Caption: ({ props }) => <p className="text-sm text-muted">{String(props.text ?? "")}</p>,
  RecordCard: function RecordCardView({ props }) {
    const result = asPresentedResult(props.result);
    const index = typeof props.index === "number" ? props.index : 0;
    const record = result?.records[index];
    return record ? <RecordRow record={record} fields={stringList(props.fields)} variant="outline" /> : null;
  },
  RecordList: function RecordListView({ props }) {
    const { t } = useLocalizedText();
    const appearance = usePresentedSurfaceAppearance();
    const result = asPresentedResult(props.result);
    if (!result || result.records.length === 0) return null;
    const shown = result.records.slice(0, presentedListLimit);
    const shortened = result.hasMore || result.records.length > shown.length;
    return (
      <div className="flex w-full flex-col gap-2">
        <ul className="flex flex-col gap-2">
          {shown.map((record) => (
            <li key={record.reference}><RecordRow record={record} fields={stringList(props.fields)} variant="secondary" /></li>
          ))}
        </ul>
        {shortened && appearance.showAll ? (
          <Tooltip delay={400}>
            <Button className="self-start" size="sm" variant="ghost" onPress={() => appearance.showAll?.(result.kind)}>
              <FaIcon icon="list" aria-hidden="true" />
              {t("intelligence.surface.showAll")}
            </Button>
            <Tooltip.Content>{t("intelligence.surface.showAllTooltip")}</Tooltip.Content>
          </Tooltip>
        ) : null}
      </div>
    );
  },
  Metric: function MetricView({ props }) {
    const appearance = usePresentedSurfaceAppearance();
    const result = asPresentedResult(props.result);
    const field = typeof props.field === "string" ? props.field : "";
    const value = result?.records[0]?.fields[field]?.value;
    if (!result || typeof value !== "number") return null;
    return (
      <KPI className="w-full">
        <KPI.Header>
          <KPI.Title>{appearance.fieldLabel(result.kind, field)}</KPI.Title>
        </KPI.Header>
        <KPI.Content>
          <KPI.Value maximumFractionDigits={0} value={value} />
        </KPI.Content>
      </KPI>
    );
  },
  BarChart: function BarChartView({ props }) {
    const { t, locale } = useLocalizedText();
    const appearance = usePresentedSurfaceAppearance();
    const result = asPresentedResult(props.result);
    const field = typeof props.field === "string" ? props.field : "";
    if (!result) return null;
    const series = result.records.flatMap((record) => {
      const value = record.fields[field]?.value;
      return typeof value === "number" ? [{ label: formatPresentedValue(record.title, locale) ?? "", value }] : [];
    });
    if (series.length === 0) return null;
    const metric = appearance.fieldLabel(result.kind, field);
    return (
      <div className="w-full" role="img" aria-label={t("intelligence.surface.chartLabel", { metric })}>
        <BarChart data={series} height={200}>
          <BarChart.Grid vertical={false} />
          <BarChart.XAxis dataKey="label" tickMargin={8} />
          <BarChart.YAxis width={40} allowDecimals={false} />
          <BarChart.Bar dataKey="value" name={metric} fill="var(--accent)" radius={[6, 6, 0, 0]} maxBarSize={40} isAnimationActive={false} />
          <BarChart.Tooltip content={<BarChart.TooltipContent />} />
        </BarChart>
        <dl className="sr-only">
          {series.map((point) => <div key={point.label}><dt>{point.label}</dt><dd>{point.value}</dd></div>)}
        </dl>
      </div>
    );
  },
};

interface BoundViewProps {
  props: Props;
  buildChild: (id: string) => ReactNode;
  context: { dataContext: { resolveSignal<V>(value: unknown): Parameters<typeof useSignalValue<V>>[0] } };
}

/** The catalog accepts `path` or `@path`; the v1.0 resolver reads `@path`. Anything else binds nothing. */
function asBinding(value: unknown): unknown {
  if (typeof value !== "object" || value === null) return null;
  const binding = value as { path?: unknown; "@path"?: unknown };
  const path = binding["@path"] ?? binding.path;
  return typeof path === "string" ? { "@path": path } : null;
}

/**
 * Resolves a component's `result` binding to the server-filled value in the surface's data model. Catalog data
 * props are data bindings only, so the bound value is always what the server read, never model text.
 */
function BoundView({ props, buildChild, context, render }: BoundViewProps & { render: Render }) {
  const signal = useMemo(() => context.dataContext.resolveSignal<unknown>(asBinding(props.result)), [context, props.result]);
  const result = useSignalValue(signal);
  return <>{render({ props: props.result === undefined ? props : { ...props, result }, buildChild })}</>;
}

let reactCatalog: Catalog<ReactComponentImplementation> | null = null;

/**
 * The Vitarish assistant catalog with its React implementations. The component schemas come from the one
 * Intelligence-owned catalog document, so the renderer accepts exactly what the agent runtime validated.
 */
export function presentedSurfaceCatalog(): Catalog<ReactComponentImplementation> {
  if (reactCatalog) return reactCatalog;
  const document = Catalog.fromJson(catalogDocument as Record<string, unknown>);
  const implementations = [...document.components.values()].map((api) => {
    const render = renderers[api.name];
    if (!render) throw new Error(`The assistant catalog component ${api.name} has no renderer.`);
    return createComponentImplementation(api as ComponentApi, (({ props, buildChild, context }: BoundViewProps) =>
      <BoundView props={props} buildChild={buildChild} context={context} render={render} />) as never);
  });
  reactCatalog = new Catalog<ReactComponentImplementation>(document.id, document.protocolVersion, implementations);
  return reactCatalog;
}

export const presentedSurfaceCatalogId: string = catalogDocument.catalogId;
