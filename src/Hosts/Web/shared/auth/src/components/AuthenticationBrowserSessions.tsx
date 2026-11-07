"use client";

import { useState } from "react";
import { Button, Surface, Tooltip } from "@heroui/react";
import { FaIcon, HeaderIcon, StatusAlert } from "@vitarish/ui";
import { formatDateTime, useLocalizedText } from "@vitarish/localization";
import { useAuthenticationBrowserSessionsGet, authenticationBrowserSessionRevoke } from "../api/generated/authentication/authentication";
import { AuthenticationWebRoutes } from "../routes";

export function AuthenticationBrowserSessions() {
  const { t, locale } = useLocalizedText();
  const query = useAuthenticationBrowserSessionsGet();
  const [ending, setEnding] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);
  async function end(sessionId: string, current: boolean) {
    setEnding(sessionId);
    setFailed(false);
    try {
      await authenticationBrowserSessionRevoke({ sessionId });
      if (current) window.location.assign(AuthenticationWebRoutes.SignIn);
      else await query.refetch();
    } catch { setFailed(true); }
    finally { setEnding(null); }
  }
  return <Surface className="flex flex-col gap-6 rounded-3xl p-6">
    <div className="flex items-center gap-3">
      <HeaderIcon icon="desktop" />
      <h2 className="text-lg font-semibold text-foreground">{t("auth.browserSessions.title")}</h2>
    </div>
    <p className="text-sm text-muted">{t("auth.browserSessions.description")}</p>
    {(failed || query.isError) && <StatusAlert message={t("auth.failures.providerFailure")} />}
    {query.isLoading && <p role="status">{t("auth.guard.loading")}</p>}
    {query.data?.sessions.map(session => <Surface variant="secondary" key={session.sessionId} className="flex flex-wrap items-center justify-between gap-4 rounded-xl p-4">
      <div className="flex flex-col gap-1">
        <p className="font-medium">{t(session.isCurrent ? "auth.browserSessions.current" : "auth.browserSessions.other")}</p>
        <p className="text-sm text-muted">{t("auth.browserSessions.started", { time: formatDateTime(session.createdAt, locale) })}</p>
        <p className="text-sm text-muted">{t("auth.browserSessions.active", { time: formatDateTime(session.lastActivityAt, locale) })}</p>
      </div>
      <Tooltip>
        <Button variant="secondary" isDisabled={ending !== null} isPending={ending === session.sessionId} onPress={() => void end(session.sessionId, session.isCurrent)}>
          <FaIcon icon="arrow-right-from-bracket" aria-hidden="true" />{t("auth.browserSessions.end")}
        </Button>
        <Tooltip.Content>{t("auth.browserSessions.end")}</Tooltip.Content>
      </Tooltip>
    </Surface>)}
  </Surface>;
}
