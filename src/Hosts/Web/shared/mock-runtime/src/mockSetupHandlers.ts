import { HttpResponse, http, type RequestHandler } from "msw";
import type { MockScenario } from "./mockHandlers";
import { mockFixtureIds } from "./mockStore";

type MockSetupHandlerContext = {
  scenario: MockScenario;
  isPersianRequest: (request: Request) => boolean;
  withDelay: () => Promise<void>;
  unavailable: () => Response;
};

/** The clinic module's setup goals, in the order the guide walks through them (`ClinicSetupGoal`). */
const setupGoals = ["ServicesAndProviders", "ShiftsAndBookingRules", "StaffUsers", "ClinicProfile"] as const;
type SetupGoal = (typeof setupGoals)[number];
const shiftsGoal: SetupGoal = "ShiftsAndBookingRules";

/** The operations assistant's agent key and the action type of a scheduling-management proposal. */
const operationsAgentKey = "clinic.assistant.operations";
const manageActionType = "appointment_manage";
const manageCapability = "Operations";
const createShiftOperation = "create_shift";

const actionStatus = { proposed: "Proposed", completed: "Completed", rejected: "Rejected" } as const;
type ActionStatus = (typeof actionStatus)[keyof typeof actionStatus];

const blockKind = { question: "Question", nextAction: "NextAction", actionGroup: "ActionGroup" } as const;

/** The two shift proposals the mock operations assistant prepares when asked about shifts. */
export const mockShiftAssistantActionIds = ["mock-shift-assistant-action-1", "mock-shift-assistant-action-2"] as const;
const mockOperationsRunId = "mock-operations-run";
const mockOperationsConversationId = "mock-operations-conversation";
const shiftMessagePattern = /shift|شیفت/i;
const minuteMilliseconds = 60_000;

type MockOperationsAction = {
  actionId: string;
  status: ActionStatus;
  reviewPayload: { operation: string; payload: Record<string, unknown> };
};

type ActionRoute = { id: string; verb: string | null };

const actionRoutePattern = /\/api\/modules\/clinic\/assistant\/actions\/([^/?]+)(?:\/(confirm|reject|revise))?(?:\?|$)/;

function readActionRoute(request: Request): ActionRoute | null {
  const match = actionRoutePattern.exec(new URL(request.url).pathname + new URL(request.url).search);
  return match ? { id: decodeURIComponent(match[1]!), verb: match[2] ?? null } : null;
}

function shiftProposal(actionId: string, dayOfWeek: number): MockOperationsAction {
  return {
    actionId,
    status: actionStatus.proposed,
    reviewPayload: {
      operation: createShiftOperation,
      payload: {
        serviceId: mockFixtureIds.service,
        serviceProviderId: mockFixtureIds.serviceProvider,
        dayOfWeek,
        startTime: "09:00",
        endTime: "13:00",
        totalSeats: 6,
        timeZone: "Asia/Tehran",
      },
    },
  };
}

/**
 * Mock of the clinic setup guide (`/api/modules/clinic/setup`) and of the operations assistant's guided reply: two
 * prepared shift proposals the owner can review, edit and confirm. The progress, the dismissal and the proposals are
 * kept for the life of the mock, so confirming a shift completes its goal like the real module.
 */
export function createSetupMockHandlers({ scenario, isPersianRequest, withDelay, unavailable }: MockSetupHandlerContext): RequestHandler[] {
  const done = new Set<SetupGoal>(scenario === "empty" ? [] : [setupGoals[0]]);
  let dismissed = false;
  let revisionCount = 0;
  const actions = new Map<string, MockOperationsAction>();
  const actionDetails = (action: MockOperationsAction) => ({
    actionId: action.actionId,
    agentRunId: mockOperationsRunId,
    conversationId: mockOperationsConversationId,
    capability: manageCapability,
    actionType: manageActionType,
    status: action.status,
    reviewPayload: action.reviewPayload,
    expiresAt: new Date(Date.now() + 30 * minuteMilliseconds).toISOString(),
    completionPayload: action.status === actionStatus.completed ? {} : null,
    failure: null,
  });
  const problem = (status: number, code: string) => HttpResponse.json({ status, code, title: code }, { status });

  return [
    http.get(/\/api\/modules\/clinic\/setup\/progress(?:\?|$)/, async () => {
      await withDelay();
      if (scenario === "error") return unavailable();
      const nextGoal = setupGoals.find((goal) => !done.has(goal)) ?? null;
      return HttpResponse.json({
        applies: !dismissed,
        goals: setupGoals.map((goal) => ({ goal, done: done.has(goal) })),
        nextGoal,
        dismissed,
      });
    }),
    http.post(/\/api\/modules\/clinic\/setup\/dismissal(?:\?|$)/, async () => {
      await withDelay();
      if (scenario === "error") return unavailable();
      dismissed = true;
      return new HttpResponse(null, { status: 204 });
    }),
    // A shift question gets two prepared proposals plus a follow-up question and a next step; anything else falls through.
    http.post(/\/api\/modules\/intelligence\/agent-conversations(?:\?|$)/, async ({ request }) => {
      const body = await request.clone().json().catch(() => null) as { agentKey?: unknown; message?: unknown } | null;
      if (body?.agentKey !== operationsAgentKey || typeof body.message !== "string" || !shiftMessagePattern.test(body.message)) return undefined;
      await withDelay();
      if (scenario === "error") return unavailable();
      const isPersian = isPersianRequest(request);
      const [mondayId, tuesdayId] = mockShiftAssistantActionIds;
      actions.set(mondayId, shiftProposal(mondayId, 1));
      actions.set(tuesdayId, shiftProposal(tuesdayId, 2));
      const summary = isPersian ? "ایجاد شیفت" : "Create a shift";
      return HttpResponse.json({
        agentRunId: mockOperationsRunId,
        conversationId: mockOperationsConversationId,
        result: {
          agentRunId: mockOperationsRunId,
          conversationId: mockOperationsConversationId,
          previousResponseId: null,
          messages: [{
            kind: "AssistantMessage",
            content: isPersian
              ? "دو شیفت صبح برای دوشنبه و سه‌شنبه آماده کردم. پیش از تأیید بررسی کنید."
              : "I prepared two morning shifts for Monday and Tuesday. Review them before confirming.",
          }],
          actions: mockShiftAssistantActionIds.map((id) => ({
            id,
            capability: manageCapability,
            actionType: manageActionType,
            status: actionStatus.proposed,
            arguments: actions.get(id)!.reviewPayload,
            summary,
          })),
        },
        blocks: [
          { kind: blockKind.actionGroup, actionGroup: { agentRunId: mockOperationsRunId, actionIds: [...mockShiftAssistantActionIds] } },
          {
            kind: blockKind.question,
            question: {
              prompt: isPersian ? "شیفت‌ها چطور رزرو شوند؟" : "How should patients book these shifts?",
              options: [
                { key: "online", label: isPersian ? "رزرو آنلاین" : "Online booking" },
                { key: "front-desk", label: isPersian ? "فقط از طریق پذیرش" : "Front desk only" },
              ],
              maxSelections: "1",
            },
          },
          { kind: blockKind.nextAction, nextAction: shiftsGoal },
        ],
      });
    }),
    http.get(actionRoutePattern, async ({ request }) => {
      const route = readActionRoute(request);
      const action = route && actions.get(route.id);
      if (!action) return undefined;
      await withDelay();
      if (scenario === "error") return unavailable();
      return HttpResponse.json(actionDetails(action));
    }),
    http.post(actionRoutePattern, async ({ request }) => {
      const route = readActionRoute(request);
      const action = route && actions.get(route.id);
      if (!route || !action) return undefined;
      await withDelay();
      if (scenario === "error") return unavailable();
      if (action.status !== actionStatus.proposed) return problem(409, "ActionNotProposed");
      if (route.verb === "confirm") {
        action.status = actionStatus.completed;
        done.add(shiftsGoal);
        return HttpResponse.json({ actionId: action.actionId, status: action.status, completionPayload: {}, failure: null });
      }
      if (route.verb === "reject") {
        action.status = actionStatus.rejected;
        return HttpResponse.json({ actionId: action.actionId, status: action.status, failure: null });
      }
      const body = await request.json().catch(() => null) as { arguments?: { operation?: unknown; payload?: unknown } } | null;
      const revised = body?.arguments;
      if (!revised || typeof revised.operation !== "string" || typeof revised.payload !== "object" || revised.payload === null) {
        return problem(400, "InvalidRequest");
      }
      // The edit replaces the proposal: the old one is closed and a new one takes its place.
      revisionCount += 1;
      action.status = actionStatus.rejected;
      const replacement: MockOperationsAction = {
        actionId: `${action.actionId}-revision-${revisionCount}`,
        status: actionStatus.proposed,
        reviewPayload: { operation: revised.operation, payload: revised.payload as Record<string, unknown> },
      };
      actions.set(replacement.actionId, replacement);
      return HttpResponse.json(actionDetails(replacement));
    }),
  ];
}
