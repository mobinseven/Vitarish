"use client";

import { useState } from "react";
import { Button, Input, Label, Surface, TextField } from "@heroui/react";
import { useQueryClient } from "@tanstack/react-query";
import { useTenantAuthorization } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";
import {
  getPartiesGetTenantProfileQueryKey,
  usePartiesGetTenantProfile,
  usePartiesSetTenantTimeZone,
} from "../../api/generated/parties/parties";

export function PracticeTimeZoneSettings() {
  const { t } = useLocalizedText();
  const { hasAccess } = useTenantAuthorization("owner");
  const queryClient = useQueryClient();
  const profile = usePartiesGetTenantProfile({ query: { enabled: hasAccess } });
  const [draft, setDraft] = useState<string | null>(null);
  const save = usePartiesSetTenantTimeZone({
    mutation: {
      onSuccess: (_result, { data }) => {
        if (profile.data) {
          queryClient.setQueryData(getPartiesGetTenantProfileQueryKey(), {
            ...profile.data,
            timeZoneId: data.timeZoneId,
          });
        }
        setDraft(null);
      },
    },
  });

  if (!hasAccess) return null;

  const timeZoneId = draft ?? profile.data?.timeZoneId ?? "";
  const submittedTimeZoneId = timeZoneId.trim();
  const canSave = !save.isPending && !!submittedTimeZoneId && submittedTimeZoneId !== profile.data?.timeZoneId;
  return (
    <Surface className="flex flex-col gap-4 rounded-3xl p-6">
      <h2 className="text-lg font-semibold">{t("journey.practiceTimeZone.title")}</h2>
      {profile.isPending && <p role="status">{t("common.states.loading")}</p>}
      {profile.isError && <p role="alert">{t("journey.practiceTimeZone.loadFailed")}</p>}
      {profile.data && (
        <form
          className="flex flex-col items-start gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            if (canSave) save.mutate({ data: { timeZoneId: submittedTimeZoneId } });
          }}
        >
          <TextField
            isRequired
            isDisabled={save.isPending}
            value={timeZoneId}
            onChange={(value) => {
              save.reset();
              setDraft(value);
            }}
          >
            <Label>{t("journey.practiceTimeZone.label")}</Label>
            <Input dir="ltr" spellCheck={false} />
          </TextField>
          <Button type="submit" isDisabled={!canSave}>
            {t("common.actions.save")}
          </Button>
          {save.isSuccess && <p role="status">{t("journey.practiceTimeZone.saved")}</p>}
          {save.isError && <p role="alert">{t("journey.practiceTimeZone.saveFailed")}</p>}
        </form>
      )}
    </Surface>
  );
}
