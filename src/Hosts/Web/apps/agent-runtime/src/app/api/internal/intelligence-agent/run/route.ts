import { NextRequest } from "next/server";
import { executeAgentRun, prepareAgentRun } from "./agent-run";

export async function POST(request: NextRequest) {
  const prepared = await prepareAgentRun(request);
  if (prepared instanceof Response) return prepared;
  return Response.json(await executeAgentRun(prepared, request.signal));
}
