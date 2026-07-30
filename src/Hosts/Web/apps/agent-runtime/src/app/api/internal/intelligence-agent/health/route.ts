import { NextRequest } from "next/server";

function authorized(request: NextRequest): boolean {
  const expected = process.env.AGENT_RUNTIME_SHARED_SECRET;
  return Boolean(expected) && request.headers.get("authorization") === `Bearer ${expected}`;
}

export async function GET(request: NextRequest) {
  if (!authorized(request)) return Response.json({ status: "unauthorized" }, { status: 401 });
  const url = process.env.AGENT_RUNTIME_INTELLIGENCE_HEALTH_URL;
  const secret = process.env.AGENT_RUNTIME_SHARED_SECRET;
  if (!url || !secret) return Response.json({ status: "not_configured" }, { status: 503 });
  try {
    const response = await fetch(url, {
      headers: { authorization: `Bearer ${secret}` },
      cache: "no-store",
      signal: AbortSignal.timeout(5000),
    });
    return Response.json({ status: response.ok ? "ok" : "intelligence_unhealthy", intelligenceStatus: response.status }, { status: response.ok ? 200 : 503 });
  } catch {
    return Response.json({ status: "intelligence_unreachable" }, { status: 503 });
  }
}
