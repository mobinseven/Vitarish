"use client";

import type { ReactNode } from "react";
import { Spinner } from "@heroui/react";
import { EmptyState } from "@heroui-pro/react";

interface ClinicLoadingStateProps {
  label: string;
}

interface ClinicMessageStateProps {
  title: string;
  description?: string;
  children?: ReactNode;
}

export function ClinicLoadingState({ label }: ClinicLoadingStateProps) {
  return (
    <EmptyState className="rounded-2xl border border-dashed border-border bg-surface-secondary" size="sm">
      <EmptyState.Header>
        <EmptyState.Title>{label}</EmptyState.Title>
      </EmptyState.Header>
      <EmptyState.Content>
        <Spinner size="sm" />
      </EmptyState.Content>
    </EmptyState>
  );
}

export function ClinicErrorState({ title, description, children }: ClinicMessageStateProps) {
  return (
    <EmptyState className="rounded-2xl border border-danger/20 bg-danger/5" size="sm">
      <EmptyState.Header>
        <EmptyState.Title className="text-danger">{title}</EmptyState.Title>
        {description && <EmptyState.Description>{description}</EmptyState.Description>}
      </EmptyState.Header>
      {children && <EmptyState.Content>{children}</EmptyState.Content>}
    </EmptyState>
  );
}

export function ClinicEmptyState({ title, description, children }: ClinicMessageStateProps) {
  return (
    <EmptyState className="rounded-2xl border border-dashed border-border bg-surface-secondary" size="sm">
      <EmptyState.Header>
        <EmptyState.Title>{title}</EmptyState.Title>
        {description && <EmptyState.Description>{description}</EmptyState.Description>}
      </EmptyState.Header>
      {children && <EmptyState.Content>{children}</EmptyState.Content>}
    </EmptyState>
  );
}
