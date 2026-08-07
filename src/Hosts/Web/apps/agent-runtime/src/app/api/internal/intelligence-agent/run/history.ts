import { protocol, type AgentInputItem } from "@openai/agents";

const maximumHistoryBytes = 1024 * 1024;

export function parseHistory(history: string | null | undefined): AgentInputItem[] {
  if (!history) return [];
  if (Buffer.byteLength(history, "utf8") > maximumHistoryBytes)
    throw new Error("InvalidHistory");
  let parsed: unknown;
  try {
    parsed = JSON.parse(history);
  } catch {
    throw new Error("InvalidHistory");
  }
  if (!Array.isArray(parsed)) throw new Error("InvalidHistory");
  const items: AgentInputItem[] = [];
  for (const item of parsed) {
    const validated = protocol.ModelItem.safeParse(item);
    if (!validated.success) throw new Error("InvalidHistory");
    items.push(validated.data);
  }
  return items;
}
