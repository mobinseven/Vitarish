"use client";

import { AuthMethodList, type AuthMethodListItem } from "@vitarish/web-ui";
import { FaIcon } from "@vitarish/web-ui";
import { useLocalizedText, type MessageKey } from "@vitarish/web-localization";

export interface AuthMethodRoute {
  id: string;
  labelKey: MessageKey;
  href: string;
  icon: string;
}

interface AuthMethodSelectorProps {
  methods: AuthMethodRoute[];
}

export function AuthMethodSelector({ methods }: AuthMethodSelectorProps) {
  const { t } = useLocalizedText();
  const localizedMethods: AuthMethodListItem[] = methods.map((method) => ({
    id: method.id,
    href: method.href,
    label: t(method.labelKey),
    icon: <FaIcon icon={method.icon} aria-hidden="true" className="size-5" />,
  }));

  return <AuthMethodList methods={localizedMethods} />;
}
