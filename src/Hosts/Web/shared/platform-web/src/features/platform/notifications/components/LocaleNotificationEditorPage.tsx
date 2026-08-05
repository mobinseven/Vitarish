"use client";

import { useCallback, useState, useEffect } from "react";
import { Button } from "@heroui/react";
import { useRouter } from "next/navigation";
import {
  useLocaleNotificationDetail,
  useCreateLocaleNotification,
  useUpdateLocaleNotification,
  type LocaleNotificationRecordRequest,
} from "../hooks/useLocaleNotifications";
import { useLocalizedText } from "@vitarish/web-localization";

type LegacyFieldProps = { label: string; value: string; onValueChange: (value: string) => void; errorMessage?: string; isInvalid?: boolean };
function TextField({ label, value, onValueChange, errorMessage, isInvalid }: LegacyFieldProps) {
  return <label className="block"><span>{label}</span><input className="block w-full rounded border px-3 py-2" value={value} onChange={(event) => onValueChange(event.target.value)} aria-invalid={isInvalid} />{errorMessage && <span className="text-danger">{errorMessage}</span>}</label>;
}
function Textarea({ label, value, onValueChange, errorMessage, isInvalid }: LegacyFieldProps) {
  return <label className="block"><span>{label}</span><textarea className="block min-h-24 w-full rounded border px-3 py-2" value={value} onChange={(event) => onValueChange(event.target.value)} aria-invalid={isInvalid} />{errorMessage && <span className="text-danger">{errorMessage}</span>}</label>;
}

interface LocaleNotificationEditorPageProps {
  normalizedLocale: string;
  basePath: string;
  isNew?: boolean;
}

export function LocaleNotificationEditorPage({
  normalizedLocale,
  basePath,
  isNew = false,
}: LocaleNotificationEditorPageProps) {
  const { t } = useLocalizedText();
  const router = useRouter();
  const { data: existing, isLoading } =
    useLocaleNotificationDetail(normalizedLocale);
  const createMutation = useCreateLocaleNotification();
  const updateMutation = useUpdateLocaleNotification(normalizedLocale);

  const [form, setForm] = useState<LocaleNotificationRecordRequest>({
    locale: normalizedLocale,
    verificationSmsTemplateId: "",
    seaReminderEmailSubject: "",
    seaReminderEmailTextBody: "",
    seaReminderEmailHtmlBody: "",
    seaReminderSmsTemplateId: "",
    appointmentReminderEmailSubject: "",
    appointmentReminderEmailTextBody: "",
    appointmentReminderEmailHtmlBody: "",
    appointmentReminderSmsTemplateId: "",
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (existing && !isNew) {
      setForm({
        locale: existing.locale,
        verificationSmsTemplateId: existing.verificationSmsTemplateId,
        seaReminderEmailSubject: existing.seaReminderEmailSubject,
        seaReminderEmailTextBody: existing.seaReminderEmailTextBody,
        seaReminderEmailHtmlBody: existing.seaReminderEmailHtmlBody,
        seaReminderSmsTemplateId: existing.seaReminderSmsTemplateId,
        appointmentReminderEmailSubject:
          existing.appointmentReminderEmailSubject,
        appointmentReminderEmailTextBody:
          existing.appointmentReminderEmailTextBody,
        appointmentReminderEmailHtmlBody:
          existing.appointmentReminderEmailHtmlBody,
        appointmentReminderSmsTemplateId:
          existing.appointmentReminderSmsTemplateId,
      });
    }
  }, [existing, isNew]);

  const validate = useCallback((): boolean => {
    const newErrors: Record<string, string> = {};

    if (!form.locale)
      newErrors.locale = t("platform.notifications.validation.localeRequired");
    if (!form.verificationSmsTemplateId)
      newErrors.verificationSmsTemplateId = t(
        "platform.notifications.validation.verificationSmsTemplateIdRequired",
      );
    if (!form.seaReminderEmailSubject)
      newErrors.seaReminderEmailSubject = t(
        "platform.notifications.validation.seaReminderEmailSubjectRequired",
      );
    if (!form.seaReminderEmailTextBody)
      newErrors.seaReminderEmailTextBody = t(
        "platform.notifications.validation.seaReminderEmailTextBodyRequired",
      );
    if (!form.seaReminderEmailHtmlBody)
      newErrors.seaReminderEmailHtmlBody = t(
        "platform.notifications.validation.seaReminderEmailHtmlBodyRequired",
      );
    if (!form.seaReminderSmsTemplateId)
      newErrors.seaReminderSmsTemplateId = t(
        "platform.notifications.validation.seaReminderSmsTemplateIdRequired",
      );
    if (!form.appointmentReminderEmailSubject)
      newErrors.appointmentReminderEmailSubject = t(
        "platform.notifications.validation.appointmentReminderEmailSubjectRequired",
      );
    if (!form.appointmentReminderEmailTextBody)
      newErrors.appointmentReminderEmailTextBody = t(
        "platform.notifications.validation.appointmentReminderEmailTextBodyRequired",
      );
    if (!form.appointmentReminderEmailHtmlBody)
      newErrors.appointmentReminderEmailHtmlBody = t(
        "platform.notifications.validation.appointmentReminderEmailHtmlBodyRequired",
      );
    if (!form.appointmentReminderSmsTemplateId)
      newErrors.appointmentReminderSmsTemplateId = t(
        "platform.notifications.validation.appointmentReminderSmsTemplateIdRequired",
      );

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [form, t]);

  const handleSave = useCallback(async () => {
    if (!validate()) return;

    if (isNew) {
      await createMutation.mutateAsync(form);
    } else {
      await updateMutation.mutateAsync(form);
    }
    router.push(basePath);
  }, [form, validate, isNew, createMutation, updateMutation, router, basePath]);

  if (isLoading) {
    return <div>{t("common.loading")}</div>;
  }

  return (
    <div>
      <h1>
        {isNew
          ? t("platform.notifications.editor.createNew")
          : t("platform.notifications.editor.edit", {
              locale: normalizedLocale,
            })}
      </h1>

      <div>
        <h2>{t("platform.notifications.verificationCode")}</h2>
        <TextField
          label={t("platform.notifications.fields.smsTemplateId")}
          value={form.verificationSmsTemplateId}
          onValueChange={(value) =>
            setForm((prev) => ({ ...prev, verificationSmsTemplateId: value }))
          }
          errorMessage={errors.verificationSmsTemplateId}
          isInvalid={!!errors.verificationSmsTemplateId}
        />
      </div>

      <div>
        <h2>{t("platform.notifications.seaReminder")}</h2>
        <TextField
          label={t("platform.notifications.fields.emailSubject")}
          value={form.seaReminderEmailSubject}
          onValueChange={(value) =>
            setForm((prev) => ({ ...prev, seaReminderEmailSubject: value }))
          }
          errorMessage={errors.seaReminderEmailSubject}
          isInvalid={!!errors.seaReminderEmailSubject}
        />
        <Textarea
          label={t("platform.notifications.fields.emailTextBody")}
          value={form.seaReminderEmailTextBody}
          onValueChange={(value) =>
            setForm((prev) => ({ ...prev, seaReminderEmailTextBody: value }))
          }
          errorMessage={errors.seaReminderEmailTextBody}
          isInvalid={!!errors.seaReminderEmailTextBody}
        />
        <p>{t("platform.notifications.placeholders.seaReminder")}</p>
        <Textarea
          label={t("platform.notifications.fields.emailHtmlBody")}
          value={form.seaReminderEmailHtmlBody}
          onValueChange={(value) =>
            setForm((prev) => ({ ...prev, seaReminderEmailHtmlBody: value }))
          }
          errorMessage={errors.seaReminderEmailHtmlBody}
          isInvalid={!!errors.seaReminderEmailHtmlBody}
        />
        <TextField
          label={t("platform.notifications.fields.smsTemplateId")}
          value={form.seaReminderSmsTemplateId}
          onValueChange={(value) =>
            setForm((prev) => ({ ...prev, seaReminderSmsTemplateId: value }))
          }
          errorMessage={errors.seaReminderSmsTemplateId}
          isInvalid={!!errors.seaReminderSmsTemplateId}
        />
      </div>

      <div>
        <h2>{t("platform.notifications.appointmentReminder")}</h2>
        <TextField
          label={t("platform.notifications.fields.emailSubject")}
          value={form.appointmentReminderEmailSubject}
          onValueChange={(value) =>
            setForm((prev) => ({
              ...prev,
              appointmentReminderEmailSubject: value,
            }))
          }
          errorMessage={errors.appointmentReminderEmailSubject}
          isInvalid={!!errors.appointmentReminderEmailSubject}
        />
        <Textarea
          label={t("platform.notifications.fields.emailTextBody")}
          value={form.appointmentReminderEmailTextBody}
          onValueChange={(value) =>
            setForm((prev) => ({
              ...prev,
              appointmentReminderEmailTextBody: value,
            }))
          }
          errorMessage={errors.appointmentReminderEmailTextBody}
          isInvalid={!!errors.appointmentReminderEmailTextBody}
        />
        <p>{t("platform.notifications.placeholders.appointmentReminder")}</p>
        <Textarea
          label={t("platform.notifications.fields.emailHtmlBody")}
          value={form.appointmentReminderEmailHtmlBody}
          onValueChange={(value) =>
            setForm((prev) => ({
              ...prev,
              appointmentReminderEmailHtmlBody: value,
            }))
          }
          errorMessage={errors.appointmentReminderEmailHtmlBody}
          isInvalid={!!errors.appointmentReminderEmailHtmlBody}
        />
        <TextField
          label={t("platform.notifications.fields.smsTemplateId")}
          value={form.appointmentReminderSmsTemplateId}
          onValueChange={(value) =>
            setForm((prev) => ({
              ...prev,
              appointmentReminderSmsTemplateId: value,
            }))
          }
          errorMessage={errors.appointmentReminderSmsTemplateId}
          isInvalid={!!errors.appointmentReminderSmsTemplateId}
        />
      </div>

      <div>
        <Button
          onPress={handleSave}
          isPending={createMutation.isPending || updateMutation.isPending}
        >
          {t("common.actions.save")}
        </Button>
        <Button variant="outline" onPress={() => router.push(basePath)}>
          {t("common.actions.cancel")}
        </Button>
      </div>
    </div>
  );
}
