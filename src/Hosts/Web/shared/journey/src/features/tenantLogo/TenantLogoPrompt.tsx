"use client";

import { useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Surface, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import {
  getPartiesGetTenantProfileQueryKey,
  partiesSetTenantLogo,
  usePartiesDismissTenantLogoPrompt,
  usePartiesGetTenantProfile,
} from "../../api/generated/parties/parties";
import { displayableImageTypes, maximumLogoBytes, refuseImage, uploadTenantImage, type ImageUploadRefusal } from "./uploadTenantImage";

type LogoFailure = ImageUploadRefusal | "upload";

/**
 * Asks a clinic's owner to add a logo until they upload one or dismiss the request. Both
 * are kept on the server, so the card never comes back on another device. Users who are not
 * the owner cannot read the profile and see nothing.
 */
export function TenantLogoPrompt() {
  const { t } = useLocalizedText();
  const queryClient = useQueryClient();
  const profile = usePartiesGetTenantProfile({ query: { retry: false } });
  const refreshProfile = () => queryClient.invalidateQueries({ queryKey: getPartiesGetTenantProfileQueryKey() });
  const dismiss = usePartiesDismissTenantLogoPrompt({ mutation: { onSuccess: refreshProfile } });
  const input = useRef<HTMLInputElement>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [failure, setFailure] = useState<LogoFailure | null>(null);

  if (!profile.data || profile.data.logoUrl || profile.data.logoPromptDismissed) return null;

  const upload = async (file: File | undefined) => {
    if (!file) return;
    const refusal = refuseImage(file, maximumLogoBytes);
    setFailure(refusal);
    if (refusal) return;
    setIsUploading(true);
    try {
      await partiesSetTenantLogo({ fileId: await uploadTenantImage(file) });
      await refreshProfile();
    } catch {
      setFailure("upload");
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <Surface render={(props) => <section {...props} />} className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="image" />
          <div className="flex flex-col gap-1">
            <h2 className="text-lg font-semibold text-foreground">{t("journey.logoPrompt.title")}</h2>
            <p className="text-sm text-muted">{t("journey.logoPrompt.description")}</p>
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <input
            ref={input}
            type="file"
            accept={displayableImageTypes.join(",")}
            className="sr-only"
            tabIndex={-1}
            aria-hidden="true"
            onChange={(event) => {
              void upload(event.target.files?.[0]);
              event.target.value = "";
            }}
          />
          <Tooltip>
            <Button isPending={isUploading} onPress={() => input.current?.click()}>
              <FaIcon icon="upload" aria-hidden="true" />
              {t("journey.logoPrompt.upload")}
            </Button>
            <Tooltip.Content>{t("journey.logoPrompt.uploadHint")}</Tooltip.Content>
          </Tooltip>
          <Tooltip>
            <Button variant="tertiary" isPending={dismiss.isPending} isDisabled={isUploading} onPress={() => dismiss.mutate()}>
              <FaIcon icon="xmark" aria-hidden="true" />
              {t("journey.logoPrompt.dismiss")}
            </Button>
            <Tooltip.Content>{t("journey.logoPrompt.dismissHint")}</Tooltip.Content>
          </Tooltip>
        </div>
      </div>
      {failure || dismiss.isError ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Description>{t(`journey.logoPrompt.failure.${failure ?? "dismiss"}`)}</Alert.Description>
          </Alert.Content>
        </Alert>
      ) : null}
    </Surface>
  );
}
