// Re-export infrastructure from @vitarish/web-localization
export {
  LocalizationContext,
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
  formatList,
  formatRelativeTime,
  getReactAriaLocale,
  getDateCalendar,
  getApiErrorMessageKey,
  localizeApiError,
} from "@vitarish/web-localization";
export type {
  LocalizationContextValue,
  SupportedLocale,
  ResolveLocaleInput,
  ResolvedLocale,
  LocaleResolutionSource,
  SupportedDateCalendar,
  CodeBearingError,
  MessageCatalog,
  MessageKey,
  MessageParameters,
  Translate,
} from "@vitarish/web-localization";

// App-specific: LocalizationProvider (auto-loads app message catalogs), message catalogs, and metadata
export { LocalizationProvider } from "./LocalizationProvider";
export { getMessages } from "./messages";
export type { MessageCatalog as AppMessageCatalog } from "./messages";
export { localizedMetadata } from "./metadata";
