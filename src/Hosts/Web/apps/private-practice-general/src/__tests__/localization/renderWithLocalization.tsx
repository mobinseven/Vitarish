import { render, type RenderOptions } from "@testing-library/react";
import type { ReactElement } from "react";
import { defaultLocale, I18nProvider, type SupportedLocale } from "@/localization";
import { getMessages, type MessageCatalog } from "@/localization/messages";

export function renderWithLocalization(ui: ReactElement, options?: RenderOptions & { locale?: SupportedLocale; messages?: MessageCatalog }) {
  const locale = options?.locale ?? defaultLocale;
  const messages = options?.messages ?? getMessages(locale);
  return render(ui, {
    ...options,
    wrapper: ({ children }) => (
      <I18nProvider locale={locale} messages={messages}>{children}</I18nProvider>
    ),
  });
}
