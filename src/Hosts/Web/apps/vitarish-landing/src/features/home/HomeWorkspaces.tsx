"use client";

import { Alert, Button, Card, Chip, Spinner, Tooltip } from "@heroui/react";
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
      <Alert className="flex flex-wrap items-center gap-3" status="danger">
        <Alert.Indicator><FaIcon icon="triangle-exclamation" aria-hidden="true" /></Alert.Indicator>
        <Alert.Content><Alert.Title>{t("home.workspaces.loadFailed")}</Alert.Title></Alert.Content>
        <Tooltip>
          <Button size="sm" variant="secondary" onPress={onRetry}>
            <FaIcon icon="rotate-right" aria-hidden="true" />
            {t("home.workspaces.retry")}
          </Button>
          <Tooltip.Content>{t("home.workspaces.retry")}</Tooltip.Content>
        </Tooltip>
      </Alert>
    );
  }

  const busy = pending !== null;
  const appName = (appId: string) => directory.apps.find((app) => app.appId === appId)?.name ?? appId;
  const appIds = [...new Set(directory.workspaces.map((workspace) => workspace.appId))];

  return (
    <Card className="w-full min-w-0" variant="secondary">
      <Card.Header>
        <Card.Title className="text-base">{t("home.workspaces.title")}</Card.Title>
      </Card.Header>
      <Card.Content className="flex flex-col gap-3">
        {directory.workspaces.length === 0 ? (
          <p className="text-base text-muted">{t("home.workspaces.empty")}</p>
        ) : (
          <ul className="flex flex-col gap-4">
            {appIds.map((appId) => (
              <li key={appId} className="flex flex-col gap-2">
                <h3 className="text-sm font-semibold text-muted"><bdi>{appName(appId)}</bdi></h3>
                <ul className="flex flex-col gap-2">
                  {directory.workspaces.filter((workspace) => workspace.appId === appId).map((workspace) => {
                    const isOpening = pending?.kind === "open" && pending.tenantId === workspace.tenantId;
                    const openFailed = failed?.kind === "open" && failed.tenantId === workspace.tenantId;
                    const namedOpen = t("home.workspaces.openNamed", { name: workspace.name });
                    return (
                      <li key={workspace.tenantId} className="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-surface px-4 py-3">
                        <div className="flex min-w-0 flex-col gap-1">
                          <span className="break-words text-base font-medium text-foreground"><bdi>{workspace.name}</bdi></span>
                          {openFailed ? <span className="text-sm text-danger" role="alert">{t("home.workspaces.openFailed")}</span> : null}
                        </div>
                        <Tooltip>
                          <Button
                            aria-label={namedOpen}
                            isDisabled={busy && !isOpening}
                            isPending={isOpening}
                            onPress={() => onStart({ kind: "open", tenantId: workspace.tenantId })}
                          >
                            {isOpening ? t("home.workspaces.opening") : t("home.workspaces.open")}
                            <FaIcon icon="arrow-right" aria-hidden="true" className="rtl:-scale-x-100" />
                          </Button>
                          <Tooltip.Content>
                            {t("home.workspaces.openNamedBeforeName")}<bdi>{workspace.name}</bdi>{t("home.workspaces.openNamedAfterName")}
                          </Tooltip.Content>
                        </Tooltip>
                      </li>
                    );
                  })}
                </ul>
              </li>
            ))}
          </ul>
        )}
      </Card.Content>
      <Card.Footer className="flex flex-wrap gap-2">
        {directory.apps.map((app) =>
          app.canCreate ? (
            <Tooltip key={app.appId}>
              <Button
                className="h-auto min-h-10 w-full whitespace-normal py-2 sm:w-auto"
                variant="secondary"
                isDisabled={busy && !(pending?.kind === "create" && pending.appId === app.appId)}
                isPending={pending?.kind === "create" && pending.appId === app.appId}
                onPress={() => onStart({ kind: "create", appId: app.appId })}
              >
                <FaIcon icon="plus" aria-hidden="true" />
                <span className="min-w-0">
                  {t("home.workspaces.createBeforeApp")}<bdi>{app.name}</bdi>{t("home.workspaces.createAfterApp")}
                </span>
              </Button>
              <Tooltip.Content>
                {t("home.workspaces.createBeforeApp")}<bdi>{app.name}</bdi>{t("home.workspaces.createAfterApp")}
              </Tooltip.Content>
            </Tooltip>
          ) : app.availability === LandingAppAvailability.Soon ? (
            <Chip key={app.appId} variant="soft">
              {t("home.workspaces.soonBeforeApp")}<bdi>{app.name}</bdi>{t("home.workspaces.soonAfterApp")}
            </Chip>
          ) : null,
        )}
        {failed?.kind === "create" ? (
          <span className="w-full text-sm text-danger" role="alert">{t("home.workspaces.createFailed")}</span>
        ) : null}
      </Card.Footer>
    </Card>
  );
}
