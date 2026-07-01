"use client";

import type { FormEvent, ReactNode } from "react";
import { Button, Card, Input, Label, TextField } from "@heroui/react";
import { SurfaceCard } from "@/design-system";

export type KnowledgeChatItemKind = "question" | "answer" | "notice";
export type KnowledgeChatTone = "default" | "success" | "warning" | "danger";

export function KnowledgeChatConversation({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return <section className={joinClasses("flex flex-col gap-4", className)}>{children}</section>;
}

export function KnowledgeChatMessage({
  kind,
  title,
  subtitle,
  status,
  tone = "default",
  actions,
  children,
}: {
  kind: KnowledgeChatItemKind;
  title?: string;
  subtitle?: string;
  status?: string;
  tone?: KnowledgeChatTone;
  actions?: ReactNode;
  children: ReactNode;
}) {
  return (
    <SurfaceCard className={joinClasses(kind === "question" ? "ms-auto max-w-3xl" : "")}>
      <Card.Header>
        <div className="flex w-full flex-col gap-2 md:flex-row md:items-start md:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.25em] text-muted">{kind}</p>
            {title && <h2 className="text-xl font-semibold">{title}</h2>}
            {subtitle && <p className="mt-1 text-sm text-muted">{subtitle}</p>}
          </div>
          {status && <KnowledgeChatStatus label={status} tone={tone} />}
        </div>
      </Card.Header>
      <Card.Content className="flex flex-col gap-5">
        {children}
        {actions && <KnowledgeChatMessageActions>{actions}</KnowledgeChatMessageActions>}
      </Card.Content>
    </SurfaceCard>
  );
}

export function KnowledgePromptInput({
  value,
  label = "Question",
  placeholder = "Ask anything",
  isPending = false,
  submitLabel = "Ask",
  helperText,
  actions,
  onChange,
  onSubmit,
}: {
  value: string;
  label?: string;
  placeholder?: string;
  isPending?: boolean;
  submitLabel?: string;
  helperText?: string;
  actions?: ReactNode;
  onChange: (value: string) => void;
  onSubmit: (value: string) => void;
}) {
  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSubmit(value);
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="flex flex-col gap-3 md:flex-row md:items-end">
        <TextField name="knowledgePrompt" isRequired isDisabled={isPending} className="flex-1">
          <Label>{label}</Label>
          <Input
            placeholder={placeholder}
            value={value}
            onChange={(event) => onChange(event.target.value)}
            autoComplete="off"
          />
        </TextField>
        <Button type="submit" isPending={isPending} className="md:min-w-32">
          {submitLabel}
        </Button>
      </div>

      {(helperText || actions) && (
        <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
          {helperText && <p className="text-sm text-muted">{helperText}</p>}
          {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
        </div>
      )}
    </form>
  );
}

export function KnowledgeChatSourceList({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <section className="flex flex-col gap-2">
      <h3 className="text-sm font-semibold">{title}</h3>
      <div className="grid gap-2">{children}</div>
    </section>
  );
}

export function KnowledgeChatSource({
  title,
  body,
  meta,
}: {
  title: string;
  body?: string | null;
  meta?: string;
}) {
  return (
    <article className="ds-inline-panel text-sm">
      <div className="flex items-center justify-between gap-3">
        <div className="font-medium">{title}</div>
        {meta && <div className="shrink-0 text-xs text-muted">{meta}</div>}
      </div>
      {body && <p className="mt-1 text-muted">{body}</p>}
    </article>
  );
}

export function KnowledgeChatLoader({ message }: { message: string }) {
  return (
    <SurfaceCard>
      <Card.Content className="flex flex-col gap-4 p-6">
        <p className="text-xs uppercase tracking-[0.25em] text-muted">Thinking</p>
        <h2 className="text-xl font-semibold">Searching grounded context...</h2>
        <p className="text-sm text-muted">{message}</p>
      </Card.Content>
    </SurfaceCard>
  );
}

export function KnowledgeChatEmptyState({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  return (
    <SurfaceCard>
      <Card.Content className="flex flex-col gap-4 p-6">
        <p className="text-xs uppercase tracking-[0.25em] text-muted">Start</p>
        <h2 className="text-xl font-semibold">{title}</h2>
        <p className="text-sm text-muted">{description}</p>
      </Card.Content>
    </SurfaceCard>
  );
}

export function KnowledgeChatMessageActions({ children }: { children: ReactNode }) {
  return <div className="flex flex-wrap gap-2">{children}</div>;
}

export function KnowledgeChatBody({ children }: { children: ReactNode }) {
  return <section className="ds-chat-body">{children}</section>;
}

export function KnowledgeChatCallout({
  tone = "default",
  children,
}: {
  tone?: KnowledgeChatTone;
  children: ReactNode;
}) {
  const toneClass = {
    default: "ds-callout",
    success: "ds-callout ds-callout-success",
    warning: "ds-callout ds-callout-warning",
    danger: "ds-callout ds-callout-danger",
  }[tone];

  return <section className={`text-sm ${toneClass}`}>{children}</section>;
}

function KnowledgeChatStatus({ label, tone }: { label: string; tone: KnowledgeChatTone }) {
  const toneClass = {
    default: "bg-[var(--surface-muted)] text-foreground",
    success: "bg-[var(--success-soft)] text-[var(--success)]",
    warning: "bg-[color-mix(in_oklch,var(--warning)_18%,transparent)] text-[var(--warning)]",
    danger: "bg-[color-mix(in_oklch,var(--danger)_16%,transparent)] text-[var(--danger)]",
  }[tone];

  return <span className={`rounded-full px-3 py-1 text-xs font-medium ${toneClass}`}>{label}</span>;
}

function joinClasses(...classes: Array<string | undefined>): string {
  return classes.filter(Boolean).join(" ");
}
