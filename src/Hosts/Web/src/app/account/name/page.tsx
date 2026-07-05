"use client";

import { useCallback } from "react";
import { Card, TextField, Label, Input, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import { useAccountNameUpdate } from "@/features/account/hooks/useAccountNameUpdate";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { useLocalizedText } from "@/localization";

export default function NamePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { t } = useLocalizedText();
  const {
    firstName,
    setFirstName,
    lastName,
    setLastName,
    success,
    isError,
    isPending,
    save,
  } = useAccountNameUpdate(userId);

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      await save(firstName, lastName);
    },
    [firstName, lastName, save]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card>
        <Card.Header>
          <h1 className="text-2xl font-bold">{t("account.name.title")}</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {isError && (
            <StatusMessage message={t("account.name.updateFailed")} />
          )}
          {success && (
            <StatusMessage message={t("account.name.updateSucceeded")} type="success" />
          )}
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <TextField name="firstName" isRequired>
              <Label>{t("account.name.firstName")}</Label>
              <Input value={firstName} onChange={(e) => setFirstName(e.target.value)} />
            </TextField>
            <TextField name="lastName" isRequired>
              <Label>{t("account.name.lastName")}</Label>
              <Input value={lastName} onChange={(e) => setLastName(e.target.value)} />
            </TextField>
            <Button
              type="submit"
              isPending={isPending}
              className="w-full"
            >
              {t("common.actions.save")}
            </Button>
          </form>
          <Link href={Routes.Account.Index} className="text-sm">
            {t("common.actions.backToSettings")}
          </Link>
        </Card.Content>
      </Card>
    </div>
  );
}
