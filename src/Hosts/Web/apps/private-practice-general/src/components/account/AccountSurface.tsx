"use client";

import {
  SettingsBackLink,
  SettingsSection,
  SettingsSurface,
  type SettingsBackLinkProps,
  type SettingsSectionProps,
  type SettingsSurfaceProps,
} from "@vitarish/web-ui";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

type AccountSurfaceProps = SettingsSurfaceProps;
type AccountWidgetProps = SettingsSectionProps;

interface AccountBackLinkProps extends Omit<SettingsBackLinkProps, "label"> {
  href?: string;
}

export function AccountSurface(props: AccountSurfaceProps) {
  return <SettingsSurface {...props} />;
}

export function AccountWidget(props: AccountWidgetProps) {
  return <SettingsSection {...props} />;
}

export function AccountBackLink({ href = Routes.Account.Index, className = "", ...props }: AccountBackLinkProps) {
  const { t } = useLocalizedText();

  return (
    <SettingsBackLink
      href={href}
      label={t("common.actions.backToSettings")}
      className={className}
      {...props}
    />
  );
}
