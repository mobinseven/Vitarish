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

export async function apiFetch<T>(
  url: string,
  options: RequestInit = {}
): Promise<T> {
  const method = options.method?.toUpperCase() ?? "GET";
  const headers = new Headers(options.headers);

  headers.set("Accept", "application/json");

  if (!headers.has("Content-Type") && options.body) {
    headers.set("Content-Type", "application/json");
  }

  if (method !== "GET" && method !== "HEAD") {
    await ensureAntiforgeryToken(headers);
  }

  const res = await fetch(`${BASE_URL}${url}`, {
    ...options,
    headers,
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
