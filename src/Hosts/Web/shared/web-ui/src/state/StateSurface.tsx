"use client";

import type { ReactNode } from "react";
import { Spinner } from "@heroui/react";
import { EmptyState } from "@heroui-pro/react/empty-state";

export interface LoadingStateSurfaceProps {
  label: string;
  className?: string;
}

export interface MessageStateSurfaceProps {
  title: string;
  description?: string;
  children?: ReactNode;
  className?: string;
}

const neutralStateClassName = "rounded-lg border border-dashed border-border bg-surface-secondary";
const dangerStateClassName = "rounded-lg border border-danger/20 bg-danger/5";

export function LoadingStateSurface({ className = "", label }: LoadingStateSurfaceProps) {
  return (
    <EmptyState className={`${neutralStateClassName} ${className}`.trim()} size="sm">
      <EmptyState.Header>
        <EmptyState.Title>{label}</EmptyState.Title>
      </EmptyState.Header>
      <EmptyState.Content>
        <Spinner size="sm" />
      </EmptyState.Content>
    </EmptyState>
  );
}

export function ErrorStateSurface({ children, className = "", description, title }: MessageStateSurfaceProps) {
  return (
    <EmptyState className={`${dangerStateClassName} ${className}`.trim()} size="sm">
      <EmptyState.Header>
        <EmptyState.Title className="text-danger">{title}</EmptyState.Title>
        {description ? <EmptyState.Description>{description}</EmptyState.Description> : null}
      </EmptyState.Header>
      {children ? <EmptyState.Content>{children}</EmptyState.Content> : null}
    </EmptyState>
  );
}

export function EmptyStateSurface({ children, className = "", description, title }: MessageStateSurfaceProps) {
  return (
    <EmptyState className={`${neutralStateClassName} ${className}`.trim()} size="sm">
      <EmptyState.Header>
        <EmptyState.Title>{title}</EmptyState.Title>
        {description ? <EmptyState.Description>{description}</EmptyState.Description> : null}
      </EmptyState.Header>
      {children ? <EmptyState.Content>{children}</EmptyState.Content> : null}
    </EmptyState>
  );
}
