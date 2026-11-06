"use client";

import { Button, Card, Chip, Spinner } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { LandingAppAvailability, type LandingWorkspacesResponse } from "@vitarish/platform";
import type { HomeHandoffTarget } from "./useHomeHandoff";

interface HomeWorkspacesProps {
  directory: LandingWorkspacesResponse | undefined;
  isLoading: boolean;
  loadFailed: boolean;
  onRetry: () => void;
  pending: HomeHandoffTarget | null;
  failed: HomeHandoffTarget | null;
  onStart: (target: HomeHandoffTarget) => void;
}

/** The greeting's workspace cards: the person's workspaces grouped by app, and one create action per app. */
export function HomeWorkspaces({ directory, isLoading, loadFailed, onRetry, pending, failed, onStart }: HomeWorkspacesProps) {
  const { t } = useLocalizedText();

  if (isLoading) {
    return <Spinner size="sm" aria-label={t("home.loading")} />;
  }
  if (loadFailed || !directory) {
    return (
      <div className="flex flex-wrap items-center gap-3 text-base text-danger" role="alert">
        {t("home.workspaces.loadFailed")}
        <Button size="sm" variant="secondary" onPress={onRetry}>{t("home.workspaces.retry")}</Button>
      </div>
    );
  }

  const busy = pending !== null;
  const appName = (appId: string) => directory.apps.find((app) => app.appId === appId)?.name ?? appId;

  return (
    <Card className="w-full min-w-0" variant="secondary">
      <Card.Header>
        <Card.Title className="text-base">{t("home.workspaces.title")}</Card.Title>
      </Card.Header>
      <Card.Content className="flex flex-col gap-3">
        {directory.workspaces.length === 0 ? (
          <p className="text-base text-muted">{t("home.workspaces.empty")}</p>
        ) : (
          <ul className="flex flex-col gap-2">
            {directory.workspaces.map((workspace) => {
              const isOpening = pending?.kind === "open" && pending.tenantId === workspace.tenantId;
              const openFailed = failed?.kind === "open" && failed.tenantId === workspace.tenantId;
              return (
                <li key={workspace.tenantId} className="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-surface px-4 py-3">
                  <div className="flex min-w-0 flex-col gap-1">
                    <span className="break-words text-base font-medium text-foreground">{workspace.name}</span>
                    <Chip className="self-start" size="sm" variant="soft">{appName(workspace.appId)}</Chip>
                    {openFailed ? <span className="text-sm text-danger" role="alert">{t("home.workspaces.openFailed")}</span> : null}
                  </div>
                  <Button
                    isDisabled={busy && !isOpening}
                    isPending={isOpening}
                    onPress={() => onStart({ kind: "open", tenantId: workspace.tenantId })}
                  >
                    {isOpening ? t("home.workspaces.opening") : t("home.workspaces.open")}
                    <FaIcon icon="arrow-right" aria-hidden="true" className="rtl:-scale-x-100" />
                  </Button>
                </li>
              );
            })}
          </ul>
        )}
      </Card.Content>
      <Card.Footer className="flex flex-wrap gap-2">
        {directory.apps.map((app) =>
          app.canCreate ? (
            <Button
              key={app.appId}
              className="h-auto min-h-10 w-full whitespace-normal py-2 sm:w-auto"
              variant="secondary"
              isDisabled={busy && !(pending?.kind === "create" && pending.appId === app.appId)}
              isPending={pending?.kind === "create" && pending.appId === app.appId}
              onPress={() => onStart({ kind: "create", appId: app.appId })}
            >
              <FaIcon icon="plus" aria-hidden="true" />
              {t("home.workspaces.create", { app: app.name })}
            </Button>
          ) : app.availability === LandingAppAvailability.Soon ? (
            <Chip key={app.appId} variant="soft">{t("home.workspaces.soon", { app: app.name })}</Chip>
          ) : null,
        )}
        {failed?.kind === "create" ? (
          <span className="w-full text-sm text-danger" role="alert">{t("home.workspaces.createFailed")}</span>
        ) : null}
      </Card.Footer>
    </Card>
  );
}
