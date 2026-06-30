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

async function ensureAntiforgeryToken(headers: Headers): Promise<void> {
  if (antiforgeryToken) {
    headers.set(antiforgeryHeaderName, antiforgeryToken);
    return;
  }

  if (!antiforgeryPromise) {
    antiforgeryPromise = (async () => {
      try {
        const res = await fetch(
          `${BASE_URL}/api/modules/authentication/antiforgery`,
          { credentials: "include" }
        );

        if (res.ok) {
          const data = await res.json();
          antiforgeryToken = data.requestToken;
          antiforgeryHeaderName = data.headerName ?? "X-XSRF-TOKEN";
        }
      } finally {
        antiforgeryPromise = null;
      }
    })();
  }

  await antiforgeryPromise;

  if (antiforgeryToken) {
    headers.set(antiforgeryHeaderName, antiforgeryToken);
  }
}

export function resetAntiforgeryToken(): void {
  antiforgeryToken = null;
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
    if (value !== undefined && value !== null) {
      searchParams.set(key, String(value));
    }
  }
  const qs = searchParams.toString();
  if (!qs) return url;
  return url.includes("?") ? `${url}&${qs}` : `${url}?${qs}`;
}

export async function apiFetch<T>(
  urlOrConfig: string | OrvalConfig,
  options: RequestInit = {}
): Promise<T> {
  let url: string;
  let method: string;
  let headers: Headers;
  let body: BodyInit | undefined;
  let signal: AbortSignal | undefined;

  if (isOrvalConfig(urlOrConfig)) {
    const config = urlOrConfig;
    url = config.url;
    method = config.method?.toUpperCase() ?? "GET";
    headers = new Headers(config.headers);
    signal = config.signal;

    if (config.params) {
      url = appendParams(url, config.params);
    }

    if (config.data !== undefined) {
      body = JSON.stringify(config.data);
    }
  } else {
    url = urlOrConfig;
    method = options.method?.toUpperCase() ?? "GET";
    headers = new Headers(options.headers);
    body = options.body as BodyInit | undefined;
    signal = options.signal as AbortSignal | undefined;
  }

  headers.set("Accept", "application/json");

  if (!headers.has("Content-Type") && body) {
    headers.set("Content-Type", "application/json");
  }

  if (method !== "GET" && method !== "HEAD") {
    await ensureAntiforgeryToken(headers);
  }

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
    try {
      problem = await res.json();
    } catch {
      problem = { status: res.status, title: res.statusText };
    }
    throw new ApiError(problem);
  }

  if (res.status === 204) {
    return undefined as T;
  }

  return res.json() as Promise<T>;
}
