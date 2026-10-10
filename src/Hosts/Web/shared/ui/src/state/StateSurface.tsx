"use client";

import React, { type ReactNode } from "react";
import { Alert, Spinner } from "@heroui/react";
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

const neutralStateClassName =
  "rounded-lg border border-dashed border-border bg-surface-secondary";

export function LoadingStateSurface({
  className = "",
  label,
}: LoadingStateSurfaceProps) {
  return (
    <EmptyState
      className={`${neutralStateClassName} ${className}`.trim()}
      size="sm"
    >
      <EmptyState.Header>
        <EmptyState.Title>{label}</EmptyState.Title>
      </EmptyState.Header>
      <EmptyState.Content>
        <Spinner size="sm" />
      </EmptyState.Content>
    </EmptyState>
  );
}

export function ErrorStateSurface({
  children,
  className = "",
  description,
  title,
}: MessageStateSurfaceProps) {
  return (
    <Alert className={className || undefined} status="danger">
      <Alert.Indicator />
      <Alert.Content>
        <Alert.Title>{title}</Alert.Title>
        {description ? <Alert.Description>{description}</Alert.Description> : null}
        {children}
      </Alert.Content>
    </Alert>
  );
}

export function EmptyStateSurface({
  children,
  className = "",
  description,
  title,
}: MessageStateSurfaceProps) {
  return (
    <EmptyState
      className={`${neutralStateClassName} ${className}`.trim()}
      size="sm"
    >
      <EmptyState.Header>
        <EmptyState.Title>{title}</EmptyState.Title>
        {description ? (
          <EmptyState.Description>{description}</EmptyState.Description>
        ) : null}
      </EmptyState.Header>
      {children ? <EmptyState.Content>{children}</EmptyState.Content> : null}
    </EmptyState>
  );
}
