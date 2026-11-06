"use client";

import { useId, useState, type FormEvent } from "react";
import {
  Button,
  Input,
  Label,
  Surface,
  Switch,
  TextField,
  Tooltip,
} from "@heroui/react";
import { ApiError } from "@vitarish/api-client";
import {
  isInvalidCredentialsError,
  TwoStepSignInChannelDto,
  useAuthSession,
  type CurrentAuthenticationUserDto,
} from "@vitarish/auth";
import { HeaderIcon, StatusAlert } from "@vitarish/ui";
import { useLocalizedText } from "@vitarish/localization";

export interface AccountSignInSettingsProps {
  setFirstPassword: (newPassword: string) => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  setTwoFactorEnabled: (enabled: boolean) => Promise<void>;
}

export function AccountSignInSettings({
  setFirstPassword,
  changePassword,
  setTwoFactorEnabled,
}: AccountSignInSettingsProps) {
  const { isAuthenticated, isError, isLoading, user, refetch } = useAuthSession();
  const { t } = useLocalizedText();
  const passwordHeadingId = useId();
  const twoStepHeadingId = useId();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isSavingPassword, setIsSavingPassword] = useState(false);
  const [passwordError, setPasswordError] = useState("");
  const [passwordNotice, setPasswordNotice] = useState("");
  const [passwordConflict, setPasswordConflict] = useState(false);
  const [isSavingTwoStep, setIsSavingTwoStep] = useState(false);
  const [twoStepError, setTwoStepError] = useState("");
  const [twoStepNotice, setTwoStepNotice] = useState("");
  const [twoStepConflict, setTwoStepConflict] = useState(false);

  if (isLoading) {
    return <p aria-live="polite">{t("common.states.loading")}</p>;
  }

  if (isError) {
    return (
      <AccountSignInSettingsUnavailable
        message={t("account.signInSecurity.unavailable")}
        onRetry={refetch}
        retryLabel={t("account.signInSecurity.reload")}
      />
    );
  }

  if (!isAuthenticated || !user) {
    return <StatusAlert message={t("account.signInSecurity.unauthorized")} />;
  }

  if (!hasSignInSecurityState(user)) {
    return (
      <AccountSignInSettingsUnavailable
        message={t("account.signInSecurity.unavailable")}
        onRetry={refetch}
        retryLabel={t("account.signInSecurity.reload")}
      />
    );
  }

  const passwordHasBeenSet = user.hasPassword;
  const twoFactorIsEnabled = user.twoFactorEnabled;
  const channel = user.twoFactorChannel;
  const canEnableTwoStep =
    passwordHasBeenSet &&
    (channel === TwoStepSignInChannelDto.Phone ||
      channel === TwoStepSignInChannelDto.Email);
  const channelLabel =
    channel === TwoStepSignInChannelDto.Phone
      ? t("account.signInSecurity.channelPhone")
      : channel === TwoStepSignInChannelDto.Email
        ? t("account.signInSecurity.channelEmail")
        : t("account.signInSecurity.noChannel");

  const submitPassword = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPasswordError("");
    setPasswordNotice("");
    setPasswordConflict(false);

    if (newPassword !== confirmPassword) {
      setPasswordError(t("account.password.mismatch"));
      return;
    }

    setIsSavingPassword(true);
    let saved = false;
    try {
      if (passwordHasBeenSet) {
        await changePassword(currentPassword, newPassword);
      } else {
        await setFirstPassword(newPassword);
      }

      saved = true;
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      const refreshed = await refetch();
      if (refreshed.isError) {
        setPasswordError(t("account.signInSecurity.unavailable"));
        return;
      }

      setPasswordNotice(
        t(
          passwordHasBeenSet
            ? "account.password.changeSucceeded"
            : "account.password.firstSetSucceeded",
        ),
      );
    } catch (error: unknown) {
      setPasswordNotice("");
      setPasswordError(
        saved
          ? t("account.signInSecurity.unavailable")
          : passwordErrorMessage(error, t, passwordHasBeenSet),
      );
      setPasswordConflict(
        !saved && error instanceof ApiError && error.status === 409,
      );
    } finally {
      setIsSavingPassword(false);
    }
  };

  const updateTwoStep = async (enabled: boolean) => {
    setTwoStepError("");
    setTwoStepNotice("");
    setTwoStepConflict(false);
    setIsSavingTwoStep(true);
    let saved = false;
    try {
      await setTwoFactorEnabled(enabled);
      saved = true;
      const refreshed = await refetch();
      if (refreshed.isError) {
        setTwoStepError(t("account.signInSecurity.unavailable"));
        return;
      }

      setTwoStepNotice(t("account.signInSecurity.updated"));
    } catch (error: unknown) {
      setTwoStepNotice("");
      setTwoStepError(
        saved
          ? t("account.signInSecurity.unavailable")
          : twoStepErrorMessage(error, t),
      );
      setTwoStepConflict(
        !saved && error instanceof ApiError && error.status === 409,
      );
    } finally {
      setIsSavingTwoStep(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <Surface
        className="flex flex-col gap-6 rounded-3xl p-6"
        role="region"
        aria-labelledby={passwordHeadingId}
      >
        <div className="flex flex-col gap-2">
          <div className="flex items-center gap-3">
            <HeaderIcon icon="key" />
            <h2
              id={passwordHeadingId}
              className="text-lg font-semibold text-foreground"
            >
              {t("account.password.title")}
            </h2>
          </div>
          <p className="text-sm text-muted">
            {passwordHasBeenSet
              ? t("account.password.changeDescription")
              : t("account.password.setFirstDescription")}
          </p>
        </div>

        {passwordError && (
          <StatusAlert className="-mt-2" message={passwordError} />
        )}
        {passwordNotice && (
          <StatusAlert
            className="-mt-2"
            message={passwordNotice}
            type="success"
          />
        )}
        {passwordConflict && (
          <Tooltip delay={0}>
            <Button
              type="button"
              variant="secondary"
              onPress={() => void refetch()}
            >
              <i aria-hidden="true" className="fa-light fa-rotate-right" />
              {t("account.signInSecurity.reload")}
            </Button>
            <Tooltip.Content>
              <p>{t("account.signInSecurity.reload")}</p>
            </Tooltip.Content>
          </Tooltip>
        )}

        <form onSubmit={submitPassword} className="flex flex-col gap-4">
          {passwordHasBeenSet && (
            <TextField name="currentPassword" type="password" isRequired>
              <Label>{t("account.password.currentPassword")}</Label>
              <Input
                value={currentPassword}
                onChange={(event) => setCurrentPassword(event.target.value)}
                autoComplete="current-password"
              />
            </TextField>
          )}
          <TextField name="newPassword" type="password" isRequired>
            <Label>{t("account.password.newPassword")}</Label>
            <Input
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              autoComplete="new-password"
            />
          </TextField>
          <TextField name="confirmPassword" type="password" isRequired>
            <Label>{t("account.password.confirmNewPassword")}</Label>
            <Input
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              autoComplete="new-password"
            />
          </TextField>
          <Tooltip delay={0}>
            <Button
              type="submit"
              isPending={isSavingPassword}
              className="w-full"
            >
              <i aria-hidden="true" className="fa-light fa-key" />
              {t(
                passwordHasBeenSet
                  ? "account.password.changeAction"
                  : "account.password.setFirstAction",
              )}
            </Button>
            <Tooltip.Content>
              <p>
                {t(
                  passwordHasBeenSet
                    ? "account.password.changeAction"
                    : "account.password.setFirstAction",
                )}
              </p>
            </Tooltip.Content>
          </Tooltip>
        </form>
      </Surface>

      <Surface
        className="flex flex-col gap-6 rounded-3xl p-6"
        role="region"
        aria-labelledby={twoStepHeadingId}
      >
        <div className="flex flex-col gap-2">
          <div className="flex items-center gap-3">
            <HeaderIcon icon="shield-halved" />
            <h2
              id={twoStepHeadingId}
              className="text-lg font-semibold text-foreground"
            >
              {t("account.signInSecurity.title")}
            </h2>
          </div>
          <p className="text-sm text-muted">
            {t("account.signInSecurity.description")}
          </p>
        </div>

        {twoStepError && (
          <StatusAlert className="-mt-2" message={twoStepError} />
        )}
        {twoStepNotice && (
          <StatusAlert
            className="-mt-2"
            message={twoStepNotice}
            type="success"
          />
        )}
        {twoStepConflict && (
          <Tooltip delay={0}>
            <Button
              type="button"
              variant="secondary"
              onPress={() => void refetch()}
            >
              <i aria-hidden="true" className="fa-light fa-rotate-right" />
              {t("account.signInSecurity.reload")}
            </Button>
            <Tooltip.Content>
              <p>{t("account.signInSecurity.reload")}</p>
            </Tooltip.Content>
          </Tooltip>
        )}

        <div className="flex flex-col gap-3">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-col gap-1">
              <p className="font-medium text-foreground">
                {t("account.signInSecurity.toggleLabel")}
              </p>
              <p aria-live="polite" className="text-sm text-muted">
                {t(
                  twoFactorIsEnabled
                    ? "account.signInSecurity.enabled"
                    : "account.signInSecurity.disabled",
                )}
              </p>
            </div>
            <Tooltip delay={0}>
              <Switch
                isSelected={twoFactorIsEnabled}
                isDisabled={isSavingTwoStep || (!twoFactorIsEnabled && !canEnableTwoStep)}
                onChange={(enabled) => void updateTwoStep(enabled)}
                aria-label={t("account.signInSecurity.toggleLabel")}
              >
                <Switch.Content className="flex items-center justify-between gap-3">
                  <i aria-hidden="true" className="fa-light fa-shield-halved" />
                  <Switch.Control>
                    <Switch.Thumb />
                  </Switch.Control>
                </Switch.Content>
              </Switch>
              <Tooltip.Content>
                <p>
                  {t(
                    twoFactorIsEnabled
                      ? "account.signInSecurity.disableAction"
                      : "account.signInSecurity.enableAction",
                  )}
                </p>
              </Tooltip.Content>
            </Tooltip>
          </div>
          <p className="text-sm text-muted">
            <span className="font-medium text-foreground">
              {t("account.signInSecurity.channel")}:
            </span>
            {" "}
            {channelLabel}
          </p>
          {!passwordHasBeenSet && (
            <p className="text-sm text-muted">
              {t("account.signInSecurity.needsPassword")}
            </p>
          )}
        </div>
      </Surface>
    </div>
  );
}

function passwordErrorMessage(
  error: unknown,
  t: ReturnType<typeof useLocalizedText>["t"],
  hasPassword: boolean,
) {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      if (hasPassword && isInvalidCredentialsError(error)) {
        return t("account.password.currentPasswordRejected");
      }
      return t("account.signInSecurity.freshAuthenticationRequired");
    }
    if (error.status === 403) {
      return t("account.signInSecurity.forbidden");
    }
    if (error.status === 409) {
      return t("account.signInSecurity.conflict");
    }
    if (error.status === 503) {
      return t("account.signInSecurity.unavailable");
    }
    if (error.status === 429) {
      return t("account.signInSecurity.rateLimited");
    }
    if (error.status === 400) {
      return t("account.password.policyRejected");
    }
  }
  return t("account.password.changeFailed");
}

function twoStepErrorMessage(
  error: unknown,
  t: ReturnType<typeof useLocalizedText>["t"],
) {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return t("account.signInSecurity.freshAuthenticationRequired");
    }
    if (error.status === 403 || error.status === 400) {
      return t("account.signInSecurity.preconditionRejected");
    }
    if (error.status === 409) {
      return t("account.signInSecurity.conflict");
    }
    if (error.status === 503) {
      return t("account.signInSecurity.unavailable");
    }
    if (error.status === 429) {
      return t("account.signInSecurity.rateLimited");
    }
  }
  return t("account.signInSecurity.updateFailed");
}

function hasSignInSecurityState(
  user: CurrentAuthenticationUserDto | null,
): user is CurrentAuthenticationUserDto &
  Required<
    Pick<
      CurrentAuthenticationUserDto,
      "hasPassword" | "twoFactorEnabled" | "twoFactorChannel"
    >
  > {
  return (
    typeof user?.hasPassword === "boolean" &&
    typeof user.twoFactorEnabled === "boolean" &&
    user.twoFactorChannel !== undefined
  );
}

function AccountSignInSettingsUnavailable({
  message,
  onRetry,
  retryLabel,
}: {
  message: string;
  onRetry: () => unknown;
  retryLabel: string;
}) {
  return (
    <div className="flex flex-col items-start gap-3">
      <StatusAlert message={message} />
      <Tooltip delay={0}>
        <Button
          type="button"
          variant="secondary"
          onPress={() => void onRetry()}
        >
          <i aria-hidden="true" className="fa-light fa-rotate-right" />
          {retryLabel}
        </Button>
        <Tooltip.Content>
          <p>{retryLabel}</p>
        </Tooltip.Content>
      </Tooltip>
    </div>
  );
}
