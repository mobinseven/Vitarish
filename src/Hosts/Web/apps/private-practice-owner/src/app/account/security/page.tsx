"use client";

import { AuthenticationSessionNoticeType, useAuthSession, useAuthenticationAccessHistory, useAuthenticationSessionNotices } from "@vitarish/auth";
import { formatDateTime, useLocalizedText } from "@/localization";
import {
  AccountBackLink,
  AccountSurface,
} from "@/components/account/AccountSurface";

function SecurityValue({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-border-subtle bg-surface-secondary/50 p-4">
      <dt className="text-sm text-foreground-muted">{label}</dt>
      <dd className="mt-1 text-sm font-medium text-foreground">{value}</dd>
    </div>
  );
}

export default function AccountSecurityPage() {
  const { isAuthenticated } = useAuthSession();
  const { t, locale } = useLocalizedText();
  const historyQuery = useAuthenticationAccessHistory({ query: { enabled: isAuthenticated } });
  const noticesQuery = useAuthenticationSessionNotices({ query: { enabled: isAuthenticated } });
  const history = historyQuery.data;
  const date = (value: string | null | undefined) =>
    value ? formatDateTime(value, locale) : t("account.security.notRecorded");

  return (
    <AccountSurface>
      <div className="space-y-6">
        <div>
          <h1 className="text-xl font-semibold text-foreground">{t("account.security.title")}</h1>
          <p className="mt-2 text-sm text-foreground-muted">{t("account.security.description")}</p>
        </div>

        <section aria-labelledby="access-history-heading" className="space-y-3">
          <h2 id="access-history-heading" className="text-base font-semibold text-foreground">
            {t("account.security.accessHistory")}
          </h2>
          {historyQuery.isLoading && <p className="text-sm text-foreground-muted">{t("common.states.loading")}</p>}
          {historyQuery.isError && <p className="text-sm text-danger">{t("account.security.unavailable")}</p>}
          {!historyQuery.isLoading && !historyQuery.isError && !history && (
            <p className="text-sm text-foreground-muted">{t("account.security.noHistory")}</p>
          )}
          {history && (
            <dl className="grid gap-3 sm:grid-cols-2">
              <SecurityValue label={t("account.security.previousSuccessful")} value={date(history.previousSuccessfulAt)} />
              <SecurityValue label={t("account.security.lastSuccessful")} value={date(history.lastSuccessfulAt)} />
              <SecurityValue label={t("account.security.lastUnsuccessful")} value={date(history.lastUnsuccessfulAt)} />
              <SecurityValue label={t("account.security.failedAttempts")} value={String(history.failedAttemptsSincePreviousSuccessful)} />
              <SecurityValue label={t("account.security.currentFailedAttempts")} value={String(history.currentFailedAttempts)} />
            </dl>
          )}
        </section>

        <section aria-labelledby="session-notices-heading" className="space-y-3">
          <h2 id="session-notices-heading" className="text-base font-semibold text-foreground">
            {t("account.security.notices")}
          </h2>
          {noticesQuery.isError && <p className="text-sm text-danger">{t("account.security.unavailable")}</p>}
          {!noticesQuery.isLoading && !noticesQuery.isError && (noticesQuery.data?.length ?? 0) === 0 && (
            <p className="text-sm text-foreground-muted">{t("account.security.noNotices")}</p>
          )}
          <ul className="space-y-2">
            {noticesQuery.data?.map((notice) => (
              <li key={notice.id} className="rounded-xl border border-border-subtle bg-surface-secondary/50 p-4 text-sm text-foreground">
                <span>{notice.noticeType === AuthenticationSessionNoticeType.parallel_session_established ? t("account.security.parallelNotice") : notice.noticeType}</span>
                <time className="mt-1 block text-foreground-muted" dateTime={notice.createdAt}>
                  {formatDateTime(notice.createdAt, locale)}
                </time>
              </li>
            ))}
          </ul>
        </section>

        <AccountBackLink />
      </div>
    </AccountSurface>
  );
}
