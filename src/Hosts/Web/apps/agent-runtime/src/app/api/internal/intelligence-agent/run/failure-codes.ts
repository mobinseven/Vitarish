/**
 * Single owner of the coded failures of an agent run: the discriminator used inside the runtime, and the numeric
 * code and message Main receives in the failure body (buffered response and streamed result frame alike).
 */
export const AgentRunFailure = {
  ToolCallLimitExceeded: "ToolCallLimitExceeded",
  TokenLimitExceeded: "TokenLimitExceeded",
  Timeout: "Timeout",
  InvalidHistory: "InvalidHistory",
  ProviderFailure: "ProviderFailure",
} as const;
export type AgentRunFailureCode = (typeof AgentRunFailure)[keyof typeof AgentRunFailure];

export const agentRunFailureWire: Record<AgentRunFailureCode, { code: number; message: string }> = {
  [AgentRunFailure.ToolCallLimitExceeded]: { code: 5, message: "The agent tool-call limit was exceeded." },
  [AgentRunFailure.TokenLimitExceeded]: { code: 6, message: "The agent token limit was exceeded." },
  [AgentRunFailure.Timeout]: { code: 7, message: "The agent execution timed out." },
  [AgentRunFailure.InvalidHistory]: { code: 11, message: "The agent history is invalid." },
  [AgentRunFailure.ProviderFailure]: { code: 10, message: "The agent runtime failed." },
};
