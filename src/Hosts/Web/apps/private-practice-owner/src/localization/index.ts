// Re-export infrastructure from @vitarish/localization
export {
  LocalizationContext,
  I18nProvider,
  I18nAppLayout,
  resolveI18nAppLocale,
  useLocalizedText,
  defaultLocale,
  supportedLocales,
  getLocaleDirection,
  isSupportedLocale,
  normalizeLocale,
  toSupportedLocale,
  resolveLocale,
  parseLocaleCookie,
  serializeLocaleCookie,
  setLocaleCookie,
  localeCookieName,
  formatDate,
  formatDateTime,
  formatNumber,
  formatPercent,
  formatCurrency,
  formatList,
  formatRelativeTime,
  getReactAriaLocale,
  getDateCalendar,
  parseIsoDateValue,
  dateValueToIsoDate,
  getApiErrorMessageKey,
  localizeApiError,
} from "@vitarish/localization";
export type {
  LocalizationContextValue,
  SupportedLocale,
  ResolveLocaleInput,
  ResolvedLocale,
  LocaleResolutionSource,
  I18nProviderProps,
  I18nAppLayoutProps,
  ResolveI18nAppLocaleInput,
  SupportedDateCalendar,
  CodeBearingError,
  MessageCatalog,
  MessageKey,
  MessageParameters,
  Translate,
} from "@vitarish/localization";

// App-specific catalog-loading provider, message catalogs, and metadata
export { LocalizationProvider } from "./LocalizationProvider";
export { getMessages } from "./messages";
export type { MessageCatalog as AppMessageCatalog } from "./messages";
export { localizedMetadata } from "./metadata";
