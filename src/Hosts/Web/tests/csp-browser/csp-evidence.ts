import type { Page, Response } from "@playwright/test";
import { cspNonceMetaProperty } from "../../shared/ui/src/ContentSecurityPolicyNonceMeta";

function sanitizeResourceUrl(value: string): string {
  try {
    const url = new URL(value);
    if (url.protocol === "data:") return "data:";
    if (url.protocol === "blob:") return `blob:${url.origin}`;
    return `${url.origin}${url.pathname}`;
  } catch {
    return "invalid-or-inline-resource";
  }
}

function sanitizeConsoleMessage(value: string): string {
  return value
    .replace(/nonce-[^'"\s;]+/giu, "nonce-[redacted]")
    .replace(/https?:\/\/[^\s'"<>]+/giu, (url) => sanitizeResourceUrl(url))
    .slice(0, 300);
}

export interface CspViolationEvidence {
  page: string;
  blockedURI: string;
  effectiveDirective: string;
  violatedDirective: string;
  disposition: string;
  sourceFile: string;
  lineNumber: number;
}

export interface CspDocumentEvidence {
  reportOnlyPresent: boolean;
  enforcingHeaderPresent: boolean;
  responseNoncePresent: boolean;
  requestResponseNonceMatches: boolean;
  nonceRotatedFromPreviousDocument: boolean;
  metaNonceMatches: boolean;
  inlineStyleNonceCount: number;
  inlineStyleNoncesMatch: boolean;
  inlineScriptNonceCount: number;
  inlineScriptNoncesMatch: boolean;
  visibleTextLength: number;
  visibleContentPresent: boolean;
  stylesheetCount: number;
  loadedImages: number;
  brokenImages: number;
  violations: CspViolationEvidence[];
  consoleSignals: Array<{ page: string; directive: string; disposition: string; blockedURI: string; message: string }>;
  resources: Array<{ type: string; url: string; status: number | null; failure?: string }>;
}

export function observeCsp(page: Page) {
  const consoleSignals = new Map<string, CspDocumentEvidence["consoleSignals"][number]>();
  const resources = new Map<string, CspDocumentEvidence["resources"][number]>();
  let previousResponseNonce: string | null = null;

  page.on("console", (message) => {
    const raw = message.text();
    if (!/content security policy/i.test(raw)) return;
    if (!/violat|refused|blocked/i.test(raw)) return;
    const directive = /directive\s*(?::|\s)\s*['"]?([a-z-]+)/i.exec(raw)?.[1] ?? "unknown-directive";
    const blockedURI = /inline/i.test(raw) ? "inline" : /eval/i.test(raw) ? "eval" : "unknown-resource";
    const disposition = /report.only/i.test(raw) ? "report" : "enforce";
    const signal = { page: new URL(page.url()).pathname, directive, disposition, blockedURI, message: sanitizeConsoleMessage(raw) };
    consoleSignals.set(JSON.stringify(signal), signal);
  });

  page.on("response", (response) => {
    const request = response.request();
    if (!["stylesheet", "font", "script", "image", "media"].includes(request.resourceType())) return;
    if (request.resourceType() === "image" && new URL(request.url()).pathname.endsWith("/favicon.ico")) return;
    const resource = { type: request.resourceType(), url: sanitizeResourceUrl(request.url()), status: response.status() };
    resources.set(`${resource.type}:${resource.url}`, resource);
  });
  page.on("requestfailed", (request) => {
    if (!["stylesheet", "font", "script", "image", "media"].includes(request.resourceType())) return;
    if (request.resourceType() === "image" && new URL(request.url()).pathname.endsWith("/favicon.ico")) return;
    const failure = request.failure()?.errorText ?? "request-failed";
    if (/abort|cancel/iu.test(failure)) return;
    const resource = { type: request.resourceType(), url: sanitizeResourceUrl(request.url()), status: null, failure };
    resources.set(`${resource.type}:${resource.url}`, resource);
  });

  return {
    async document(response: Response | null): Promise<CspDocumentEvidence> {
      await page.evaluate(() => document.fonts.ready.then(() => undefined));
      const reportOnly = response?.headers()["content-security-policy-report-only"] ?? null;
      const scriptNonce = reportOnly ? /(?:^|;)\s*script-src\s+[^;]*?'nonce-([^']+)'/iu.exec(reportOnly)?.[1] ?? null : null;
      const styleNonce = reportOnly ? /(?:^|;)\s*style-src-elem\s+[^;]*?'nonce-([^']+)'/iu.exec(reportOnly)?.[1] ?? null : null;
      const nonceRotatedFromPreviousDocument = Boolean(previousResponseNonce && scriptNonce && previousResponseNonce !== scriptNonce);
      previousResponseNonce = scriptNonce;
      const dom = await page.evaluate((nonceProperty) => {
        const nonce = document.querySelector(`meta[property="${nonceProperty}"]`)?.getAttribute("content") ?? null;
        const visible = [...document.querySelectorAll("body *")].some((node) => {
          if (!(node instanceof HTMLElement)) return false;
          const element = node;
          if (["SCRIPT", "STYLE", "NOSCRIPT"].includes(element.tagName)) return false;
          const bounds = element.getBoundingClientRect();
          return bounds.width > 0 && bounds.height > 0 && element.innerText.trim().length >= 12;
        });
        const images = [...document.images].filter((image) => image.loading !== "lazy" || image.complete);
        return {
          metaNonce: nonce,
          inlineStyleNonces: [...document.querySelectorAll("style")].map((style) => style.nonce),
          inlineScriptNonces: [...document.querySelectorAll("script:not([src])")].map((script) => script.nonce),
          visibleTextLength: document.body.innerText.trim().length,
          visibleContentPresent: visible,
          stylesheetCount: document.styleSheets.length,
          loadedImages: images.filter((image) => image.complete && image.naturalWidth > 0).length,
          brokenImages: images.filter((image) => image.complete && image.naturalWidth === 0 && Boolean(image.currentSrc)).length,
          violations: window.__cspViolations ?? [],
        };
      }, cspNonceMetaProperty);
      const uniqueViolations = new Map(dom.violations.map((violation: CspViolationEvidence) => [JSON.stringify(violation), violation]));
      for (const signal of consoleSignals.values()) {
        const duplicate = [...uniqueViolations.values()].some((violation) =>
          violation.page === signal.page && violation.disposition === signal.disposition && violation.effectiveDirective === signal.directive,
        );
        if (!duplicate) {
          uniqueViolations.set(JSON.stringify(signal), {
            ...signal,
            effectiveDirective: signal.directive,
            violatedDirective: signal.directive,
            sourceFile: "",
            lineNumber: 0,
          });
        }
      }
      return {
        reportOnlyPresent: Boolean(reportOnly),
        enforcingHeaderPresent: Boolean(response?.headers()["content-security-policy"]),
        responseNoncePresent: Boolean(scriptNonce && styleNonce),
        requestResponseNonceMatches: Boolean(scriptNonce && scriptNonce === styleNonce),
        nonceRotatedFromPreviousDocument,
        metaNonceMatches: Boolean(dom.metaNonce && dom.metaNonce === scriptNonce && dom.metaNonce === styleNonce),
        inlineStyleNonceCount: dom.inlineStyleNonces.length,
        inlineStyleNoncesMatch: dom.inlineStyleNonces.every((nonce) => Boolean(scriptNonce && nonce === scriptNonce)),
        inlineScriptNonceCount: dom.inlineScriptNonces.length,
        inlineScriptNoncesMatch: dom.inlineScriptNonces.every((nonce) => Boolean(scriptNonce && nonce === scriptNonce)),
        visibleTextLength: dom.visibleTextLength,
        visibleContentPresent: dom.visibleContentPresent,
        stylesheetCount: dom.stylesheetCount,
        loadedImages: dom.loadedImages,
        brokenImages: dom.brokenImages,
        violations: [...uniqueViolations.values()],
        consoleSignals: [...consoleSignals.values()],
        resources: [...resources.values()],
      };
    },
  };
}

declare global {
  interface Window {
    __cspViolations?: CspViolationEvidence[];
  }
}

export async function installCspObserver(page: Page): Promise<void> {
  await page.addInitScript(() => {
    window.__cspViolations = [];
    window.addEventListener("securitypolicyviolation", (event) => {
      const sanitize = (raw: string): string => {
        if (["inline", "eval", "self", "none"].includes(raw)) return raw;
        try {
          const resource = new URL(raw, window.location.href);
          if (resource.protocol === "data:") return "data:";
          if (resource.protocol === "blob:") return `blob:${resource.origin}`;
          return `${resource.origin}${resource.pathname}`;
        } catch {
          return "invalid-or-inline-resource";
        }
      };
      let sourceFile = "";
      try {
        const source = new URL(event.sourceFile);
        sourceFile = `${source.origin}${source.pathname}`;
      } catch {
        sourceFile = event.sourceFile ? "invalid-or-inline-source" : "";
      }
      window.__cspViolations.push({
        page: window.location.pathname,
        blockedURI: sanitize(event.blockedURI),
        effectiveDirective: event.effectiveDirective,
        violatedDirective: event.violatedDirective,
        disposition: event.disposition,
        sourceFile,
        lineNumber: event.lineNumber,
      });
    });
  });
}

export async function waitForMeaningfulContent(page: Page): Promise<void> {
  await page.waitForFunction(() => [...document.querySelectorAll("body *")].some((node) => {
    if (!(node instanceof HTMLElement)) return false;
    const element = node;
    if (["SCRIPT", "STYLE", "NOSCRIPT"].includes(element.tagName)) return false;
    const bounds = element.getBoundingClientRect();
    return bounds.width > 0 && bounds.height > 0 && element.innerText.trim().length >= 12;
  }), undefined, { timeout: 20_000 });
}

export function cspPolicyProblems(evidence: CspDocumentEvidence): string[] {
  const problems: string[] = [];
  if (!evidence.reportOnlyPresent) problems.push("missing Report-Only response header");
  if (evidence.enforcingHeaderPresent) problems.push("unexpected enforcing response header");
  if (!evidence.responseNoncePresent || !evidence.requestResponseNonceMatches) problems.push("request and response nonce directives do not match");
  if (!evidence.metaNonceMatches) problems.push("browser nonce meta does not match the response policy");
  if (!evidence.inlineStyleNoncesMatch) problems.push("inline style nonce does not match the response policy");
  if (!evidence.inlineScriptNoncesMatch) problems.push("inline script nonce does not match the response policy");
  if (evidence.violations.length > 0) {
    const violations = evidence.violations.map((violation) =>
      `${violation.effectiveDirective}:${violation.blockedURI} at ${violation.sourceFile || "unknown source"}:${violation.lineNumber}`,
    );
    problems.push(`${evidence.violations.length} unique CSP violation(s): ${violations.join("; ")}`);
  }
  return problems;
}

export function renderingProblems(evidence: CspDocumentEvidence): string[] {
  const problems: string[] = [];
  if (evidence.visibleTextLength < 12 || !evidence.visibleContentPresent) problems.push("page did not render meaningful visible content");
  if (evidence.stylesheetCount === 0) problems.push("page has no loaded stylesheet");
  if (evidence.brokenImages > 0) problems.push(`${evidence.brokenImages} visible image(s) did not load`);
  for (const resource of evidence.resources) {
    if ((resource.status !== null && resource.status >= 400) || resource.failure) {
      problems.push(`${resource.type} resource failed at ${resource.url}${resource.status === null ? ` (${resource.failure})` : ` with HTTP ${resource.status}`}`);
    }
  }
  return problems;
}
