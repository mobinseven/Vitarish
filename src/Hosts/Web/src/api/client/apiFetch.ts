const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code: string | undefined;
  readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? "Request failed");
    this.name = "ApiError";
    this.status = problem.status ?? 500;
    this.code = problem.code;
    this.problem = problem;
  }
}

let antiforgeryToken: string | null = null;
let antiforgeryHeaderName = "X-XSRF-TOKEN";
let antiforgeryPromise: Promise<void> | null = null;

function isUnsafeMethod(method: string): boolean {
  return method !== "GET" && method !== "HEAD";
}

function antiforgeryBootstrapError(problem: ProblemDetails): ApiError {
  return new ApiError({
    status: problem.status ?? 500,
    code: problem.code ?? "AntiforgeryBootstrapFailed",
    title: problem.title ?? "Antiforgery bootstrap failed",
    detail: problem.detail ?? "The request could not be prepared because antiforgery protection was unavailable.",
  });
}

function readAntiforgeryToken(data: unknown): { headerName: string; requestToken: string } {
  if (typeof data !== "object" || data === null) {
    throw antiforgeryBootstrapError({ status: 500, detail: "The antiforgery response was not a JSON object." });
  }

  const record = data as Record<string, unknown>;
  const requestToken = typeof record.requestToken === "string" ? record.requestToken.trim() : "";
  const headerName = typeof record.headerName === "string" && record.headerName.trim().length > 0
    ? record.headerName.trim()
    : "X-XSRF-TOKEN";

  if (!requestToken) {
    throw antiforgeryBootstrapError({ status: 500, detail: "The antiforgery response did not include a request token." });
  }

  return { headerName, requestToken };
}

async function ensureAntiforgeryToken(headers: Headers): Promise<void> {
  if (antiforgeryToken) {
    headers.set(antiforgeryHeaderName, antiforgeryToken);
    return;
  }

  if (!antiforgeryPromise) {
    antiforgeryPromise = (async () => {
      const res = await fetch(`${BASE_URL}/api/modules/authentication/antiforgery`, {
        credentials: "include",
      });

      if (!res.ok) {
        let problem: ProblemDetails;
        try {
          problem = await res.json();
        } catch {
          problem = { status: res.status, title: res.statusText };
        }
        throw antiforgeryBootstrapError(problem);
      }

      const token = readAntiforgeryToken(await res.json());
      antiforgeryToken = token.requestToken;
      antiforgeryHeaderName = token.headerName;
    })().finally(() => {
      antiforgeryPromise = null;
    });
  }

  await antiforgeryPromise;
  headers.set(antiforgeryHeaderName, antiforgeryToken!);
}

export function resetAntiforgeryToken(): void {
  antiforgeryToken = null;
  antiforgeryHeaderName = "X-XSRF-TOKEN";
}

interface OrvalConfig {
  url: string;
  method?: string;
  params?: Record<string, unknown>;
  data?: unknown;
  headers?: Record<string, string>;
  signal?: AbortSignal;
}

function isOrvalConfig(value: unknown): value is OrvalConfig {
  return typeof value === "object" && value !== null && "url" in value;
}

function appendParams(url: string, params: Record<string, unknown>): string {
  const searchParams = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null) searchParams.set(key, String(value));
  }
  const qs = searchParams.toString();
  if (!qs) return url;
  return url.includes("?") ? `${url}&${qs}` : `${url}?${qs}`;
}

function isAntiforgeryValidationFailure(error: ApiError): boolean {
  return error.code === "AntiforgeryValidationFailed";
}

async function sendRequest<T>(url: string, method: string, headers: Headers, body: BodyInit | undefined, signal: AbortSignal | undefined, options: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${url}`, {
    ...options,
    method,
    headers,
    body,
    signal,
    credentials: "include",
  });

  if (!res.ok) {
    let problem: ProblemDetails;
    try { problem = await res.json(); } catch { problem = { status: res.status, title: res.statusText }; }
    throw new ApiError(problem);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export async function apiFetch<T>(urlOrConfig: string | OrvalConfig, options: RequestInit = {}): Promise<T> {
  let url: string;
  let method: string;
  let body: BodyInit | undefined;
  let signal: AbortSignal | undefined;
  const baseHeaders = new Headers(options.headers);

  if (isOrvalConfig(urlOrConfig)) {
    const config = urlOrConfig;
    url = config.url;
    method = config.method?.toUpperCase() ?? "GET";
    for (const [key, value] of Object.entries(config.headers ?? {})) baseHeaders.set(key, value);
    signal = config.signal ?? (options.signal as AbortSignal | undefined);
    if (config.params) url = appendParams(url, config.params);
    if (config.data !== undefined) body = JSON.stringify(config.data);
  } else {
    url = urlOrConfig;
    method = options.method?.toUpperCase() ?? "GET";
    body = options.body as BodyInit | undefined;
    signal = options.signal as AbortSignal | undefined;
  }

  baseHeaders.set("Accept", "application/json");
  if (!baseHeaders.has("Content-Type") && body) baseHeaders.set("Content-Type", "application/json");

  const buildHeaders = async () => {
    const headers = new Headers(baseHeaders);
    if (isUnsafeMethod(method)) await ensureAntiforgeryToken(headers);
    return headers;
  };

  try {
    return await sendRequest<T>(url, method, await buildHeaders(), body, signal, options);
  } catch (error) {
    if (!isUnsafeMethod(method) || !(error instanceof ApiError) || !isAntiforgeryValidationFailure(error)) {
      throw error;
    }

    resetAntiforgeryToken();
    return sendRequest<T>(url, method, await buildHeaders(), body, signal, options);
  }
}
