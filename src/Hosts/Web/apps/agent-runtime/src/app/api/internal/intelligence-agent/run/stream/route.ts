import { NextRequest } from "next/server";
import { prepareAgentRun } from "../agent-run";
import { streamAgentRun } from "../stream-response";

export async function POST(request: NextRequest) {
  const prepared = await prepareAgentRun(request);
  if (prepared instanceof Response) return prepared;
  return streamAgentRun(prepared, request.signal);
}
