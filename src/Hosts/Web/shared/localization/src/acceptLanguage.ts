export function parseAcceptLanguage(acceptLanguage: string | null | undefined): string[] {
  if (!acceptLanguage) return [];

  return acceptLanguage
    .split(",")
    .map((entry, index) => {
      const [locale = "", ...parameters] = entry.trim().split(";");
      const qualityParameter = parameters.find((parameter) => parameter.trim().toLowerCase().startsWith("q="));
      const parsedQuality = qualityParameter ? Number.parseFloat(qualityParameter.trim().slice(2)) : 1;
      const quality = Number.isFinite(parsedQuality) ? Math.min(Math.max(parsedQuality, 0), 1) : 0;
      return { locale: locale.trim(), quality, index };
    })
    .filter(({ locale, quality }) => locale.length > 0 && locale !== "*" && quality > 0)
    .sort((left, right) => right.quality - left.quality || left.index - right.index)
    .map(({ locale }) => locale);
}
