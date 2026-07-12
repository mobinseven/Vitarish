"use client";

import { useCallback } from "react";
import { Button, Table, TableBody, TableCell, TableColumn, TableHeader, TableRow } from "@heroui/react";
import { useRouter } from "next/navigation";
import { useLocaleNotificationList } from "../hooks/useLocaleNotifications";
import { useLocalizedText } from "@vitarish/web-localization";

interface LocaleNotificationListPageProps {
  basePath: string;
}

export function LocaleNotificationListPage({ basePath }: LocaleNotificationListPageProps) {
  const { t } = useLocalizedText();
  const router = useRouter();
  const { data: locales, isLoading, isError } = useLocaleNotificationList();

  const handleEdit = useCallback(
    (normalizedLocale: string) => {
      router.push(`${basePath}/${normalizedLocale}`);
    },
    [router, basePath]
  );

  if (isLoading) {
    return <div>{t("common.loading")}</div>;
  }

  if (isError) {
    return <div>{t("common.error")}</div>;
  }

  if (!locales || locales.length === 0) {
    return <div>{t("platform.notifications.empty")}</div>;
  }

  return (
    <div>
      <h1>{t("platform.notifications.title")}</h1>
      <Table>
        <TableHeader>
          <TableColumn>{t("platform.notifications.locale")}</TableColumn>
          <TableColumn>{t("platform.notifications.normalizedLocale")}</TableColumn>
          <TableColumn>{t("common.actions.edit")}</TableColumn>
        </TableHeader>
        <TableBody>
          {locales.map((locale) => (
            <TableRow key={locale.normalizedLocale}>
              <TableCell>{locale.locale}</TableCell>
              <TableCell>{locale.normalizedLocale}</TableCell>
              <TableCell>
                <Button size="sm" onPress={() => handleEdit(locale.normalizedLocale)}>
                  {t("common.actions.edit")}
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
