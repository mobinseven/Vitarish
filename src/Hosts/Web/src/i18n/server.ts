import { cookies, headers } from "next/headers";
import {
  cultureCookieName,
  defaultCulture,
  tryNormalizeCulture,
  type SupportedCulture,
} from "./cultures";

export async function resolveRequestCulture(): Promise<SupportedCulture> {
  const cookieStore = await cookies();
  const cookieCulture = tryNormalizeCulture(
    cookieStore.get(cultureCookieName)?.value,
  );

  if (cookieCulture) {
    return cookieCulture;
  }

  const requestHeaders = await headers();
  const acceptedCulture = tryResolveAcceptLanguage(
    requestHeaders.get("accept-language"),
  );

  return acceptedCulture ?? defaultCulture;
}

function tryResolveAcceptLanguage(
  headerValue: string | null,
): SupportedCulture | undefined {
  if (!headerValue) {
    return undefined;
  }

  return headerValue
    .split(",")
    .map((part) => parseAcceptedLanguage(part))
    .filter((candidate): candidate is AcceptedLanguage => candidate !== undefined)
    .sort((left, right) => right.quality - left.quality)
    .map((candidate) => tryNormalizeCulture(candidate.culture))
    .find((culture): culture is SupportedCulture => culture !== undefined);
}

interface AcceptedLanguage {
  readonly culture: string;
  readonly quality: number;
}

function parseAcceptedLanguage(value: string): AcceptedLanguage | undefined {
  const [culturePart, ...parameters] = value.trim().split(";");

  if (!culturePart) {
    return undefined;
  }

  const qualityParameter = parameters.find((parameter) =>
    parameter.trim().startsWith("q="),
  );
  const quality = qualityParameter
    ? Number.parseFloat(qualityParameter.split("=")[1] ?? "1")
    : 1;

  return {
    culture: culturePart,
    quality: Number.isFinite(quality) ? quality : 1,
  };
}
