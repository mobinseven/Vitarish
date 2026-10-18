import { HttpResponse, http, type RequestHandler } from "msw";
import { mockFixtureIds, type MockStore } from "./mockStore";

type MockSurgeryHandlerContext = {
  store: MockStore;
  isPersianRequest: (request: Request) => boolean;
  isUnavailable: boolean;
  withDelay: () => Promise<void>;
  unavailable: () => Response;
  notFound: (detail: string) => Response;
};

/** Mirrors the clinic module's SurgeryCatalogDto. */
type MockSurgeryCatalog = {
  id: string;
  name: string;
  tools: string[];
  defaultTeamRoles: string[];
  reminderPolicies: Array<{ name: string; minutesBefore: number | string; subject: string; textBody: string }>;
  concurrencyToken: string;
};

/** Mirrors the clinic module's SurgerySettingsDto. */
type MockSurgerySettings = {
  cancellationSubject: string;
  cancellationTextBody: string;
  concurrencyToken: string;
};

/** Mirrors the clinic module's SurgeryResponse. */
type MockSurgery = {
  id: string;
  patientId: string;
  catalogId: string;
  name: string;
  idempotencyKey: string | null;
  status: string;
  scheduledAt: string;
  tools: string[];
  personsToNotify: string[];
  contacts: unknown[];
  reminderPolicies: MockSurgeryCatalog["reminderPolicies"];
  concurrencyToken: string;
  createdAt: string;
  updatedAt: string | null;
  durationMinutes: number | null;
  location: string | null;
  team: Array<{ userId: string; displayName: string; role: string }>;
};

type Body = Record<string, unknown>;

/** The clinic module's surgery status and conflict reason wire values. */
const surgeryStatus = { planned: "Planned", completed: "Completed", cancelled: "Cancelled" } as const;
const conflictReason = { sameLocation: "SameLocation", sharedTeamMember: "SharedTeamMember" } as const;

const surgeriesPath = "\\/api\\/modules\\/clinic\\/surgeries\\/";
const route = (path: string) => new RegExp(`${surgeriesPath}${path}(?:\\?|$)`);
const patientSurgeriesRoute = /\/api\/modules\/clinic\/patients\/([^/]+)\/surgeries(?:\?|$)/;
const catalogIdPattern = /\/surgeries\/catalogs\/([^/?]+)/;
const surgeryIdPattern = /\/surgeries\/([^/?]+)/;
const minuteMilliseconds = 60_000;

// Seeded surgeries for the first patients, in days and hours from the viewer's today, so the dashboard
// always shows today's surgery and the next one, and yesterday's can be completed; Persian requests read the Persian names.
const seededSurgeries = [
  { id: mockFixtureIds.surgery, dayOffset: 0, hour: 13, name: "Knee arthroscopy", persianName: "آرتروسکوپی زانو", persianPatientName: "آدا لاولیس", location: "OR 1" },
  { dayOffset: 3, hour: 9, name: "Ligament repair", persianName: "ترمیم رباط", persianPatientName: "گریس هاپر", location: "OR 2" },
  { dayOffset: 10, hour: 8, name: "Cyst removal", persianName: "برداشتن کیست", persianPatientName: "کاترین جانسون", location: "OR 1" },
  { id: mockFixtureIds.pastSurgery, dayOffset: -1, hour: 10, name: "Tonsillectomy", persianName: "برداشتن لوزه", persianPatientName: "ماری کوری", location: "OR 2" },
];

const strings = (value: unknown) => (Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : []);
const text = (body: Body | null, key: string) => (typeof body?.[key] === "string" ? (body[key] as string) : "");
const optionalNumber = (value: unknown) => (value === undefined || value === null || value === "" ? null : Number(value));
const optionalText = (value: unknown) => (typeof value === "string" && value.trim() ? value : null);

/**
 * Surgery types, the cancellation message, and planned surgeries. Types and the message start empty
 * like a new practice, so the app creates its default type and message in the viewer's language.
 */
export function createSurgeryMockHandlers({ store, isPersianRequest, isUnavailable, withDelay, unavailable, notFound }: MockSurgeryHandlerContext): RequestHandler[] {
  const catalogs: MockSurgeryCatalog[] = [];
  let settings: MockSurgerySettings | null = null;
  let version = 0;
  const nextToken = () => `mock-surgery-token-${++version}`;

  const startOfToday = new Date();
  startOfToday.setHours(0, 0, 0, 0);
  // Persian names of seeded surgeries, dropped once the plan is rewritten.
  const persianNames = new Map<string, string>();
  const surgeries: MockSurgery[] = seededSurgeries.slice(0, store.patients.length).map((seed, index) => {
    const scheduledAt = new Date(startOfToday);
    scheduledAt.setDate(scheduledAt.getDate() + seed.dayOffset);
    scheduledAt.setHours(seed.hour);
    const id = seed.id ?? store.nextId("mock-surgery");
    persianNames.set(id, seed.persianName);
    return {
      id,
      patientId: store.patients[index].id,
      catalogId: mockFixtureIds.surgeryType,
      name: seed.name,
      idempotencyKey: null,
      status: surgeryStatus.planned,
      scheduledAt: scheduledAt.toISOString(),
      tools: [],
      personsToNotify: [],
      contacts: [],
      reminderPolicies: [],
      concurrencyToken: nextToken(),
      createdAt: "2026-01-01T00:00:00.000Z",
      updatedAt: null,
      durationMinutes: 90,
      location: seed.location,
      team: [],
    };
  });

  const answer = (respond: (request: Request) => Promise<Response> | Response) => async ({ request }: { request: Request }) => {
    await withDelay();
    return isUnavailable ? unavailable() : respond(request);
  };
  const readBody = async (request: Request) => (await request.json().catch(() => null)) as Body | null;
  const writeCatalog = (body: Body | null, id: string): MockSurgeryCatalog => ({
    id,
    name: text(body, "name"),
    tools: strings(body?.tools),
    defaultTeamRoles: strings(body?.defaultTeamRoles),
    reminderPolicies: Array.isArray(body?.reminderPolicies) ? (body.reminderPolicies as MockSurgeryCatalog["reminderPolicies"]) : [],
    concurrencyToken: nextToken(),
  });
  const writeSettings = (body: Body | null): MockSurgerySettings => ({
    cancellationSubject: text(body, "cancellationSubject"),
    cancellationTextBody: text(body, "cancellationTextBody"),
    concurrencyToken: nextToken(),
  });
  const catalogAt = (request: Request) => {
    const id = catalogIdPattern.exec(new URL(request.url).pathname)?.[1];
    return catalogs.findIndex((catalog) => catalog.id === id);
  };
  // The written plan fields of SurgeryCreateRequest and SurgeryWriteRequest.
  const writePlan = (body: Body | null) => ({
    catalogId: text(body, "catalogId"),
    name: text(body, "name"),
    status: text(body, "status") || surgeryStatus.planned,
    scheduledAt: text(body, "scheduledAt"),
    tools: strings(body?.tools),
    personsToNotify: strings(body?.personsToNotify),
    contacts: Array.isArray(body?.contacts) ? body.contacts : [],
    reminderPolicies: Array.isArray(body?.reminderPolicies) ? (body.reminderPolicies as MockSurgery["reminderPolicies"]) : [],
    durationMinutes: optionalNumber(body?.durationMinutes),
    location: optionalText(body?.location),
    team: Array.isArray(body?.team) ? (body.team as MockSurgery["team"]) : [],
  });
  const surgeryAt = (request: Request) => {
    const id = surgeryIdPattern.exec(new URL(request.url).pathname)?.[1];
    return surgeries.findIndex((surgery) => surgery.id === id);
  };
  const localized = (surgery: MockSurgery, request: Request): MockSurgery => {
    const persianName = isPersianRequest(request) ? persianNames.get(surgery.id) : undefined;
    return persianName ? { ...surgery, name: persianName } : surgery;
  };
  const entry = (surgery: MockSurgery, request: Request) => {
    const patientIndex = store.patients.findIndex((patient) => patient.id === surgery.patientId);
    const persianPatientName = isPersianRequest(request) ? seededSurgeries[patientIndex]?.persianPatientName : undefined;
    return { surgery: localized(surgery, request), patientDisplayName: persianPatientName ?? store.patients[patientIndex]?.name ?? null };
  };
  const ends = (surgery: MockSurgery) => Date.parse(surgery.scheduledAt) + (surgery.durationMinutes ?? 0) * minuteMilliseconds;
  // A write that carries an older token was read before another change.
  const staleWrite = () => HttpResponse.json({ status: 409, code: "Conflict", title: "Surgery changed since it was read" }, { status: 409 });
  const changeStatus = (status: string) => answer(async (request) => {
    const index = surgeryAt(request);
    if (index < 0) return notFound("Surgery does not exist in the mock store.");
    if (text(await readBody(request), "concurrencyToken") !== surgeries[index].concurrencyToken) return staleWrite();
    surgeries[index] = { ...surgeries[index], status, concurrencyToken: nextToken(), updatedAt: new Date().toISOString() };
    return HttpResponse.json(localized(surgeries[index], request));
  });

  return [
    http.get(route("catalogs"), answer(() => HttpResponse.json(catalogs))),
    http.post(route("catalogs\\/default"), answer(async (request) => {
      if (!catalogs.length) catalogs.push(writeCatalog(await readBody(request), mockFixtureIds.surgeryType));
      return HttpResponse.json(catalogs[0]);
    })),
    http.post(route("catalogs"), answer(async (request) => {
      const catalog = writeCatalog(await readBody(request), `mock-surgery-type-${catalogs.length + 1}`);
      catalogs.push(catalog);
      return HttpResponse.json(catalog, { status: 201 });
    })),
    http.put(route("catalogs\\/[^/]+"), answer(async (request) => {
      const index = catalogAt(request);
      if (index < 0) return notFound("Surgery type does not exist in the mock store.");
      catalogs[index] = writeCatalog(await readBody(request), catalogs[index].id);
      return HttpResponse.json(catalogs[index]);
    })),
    http.post(route("catalogs\\/[^/]+\\/delete"), answer((request) => {
      const index = catalogAt(request);
      if (index < 0) return notFound("Surgery type does not exist in the mock store.");
      catalogs.splice(index, 1);
      return new HttpResponse(null, { status: 204 });
    })),
    http.get(route("settings"), answer(() => (settings ? HttpResponse.json(settings) : notFound("Surgery settings have not been created.")))),
    http.post(route("settings\\/default"), answer(async (request) => {
      settings ??= writeSettings(await readBody(request));
      return HttpResponse.json(settings);
    })),
    http.put(route("settings"), answer(async (request) => {
      settings = writeSettings(await readBody(request));
      return HttpResponse.json(settings);
    })),
    // Suggestions come from earlier plans and the types' roles.
    http.get(route("suggestions"), answer(() => HttpResponse.json({
      locations: [...new Set(surgeries.flatMap((surgery) => (surgery.location ? [surgery.location] : [])))],
      roles: [...new Set([...catalogs.flatMap((catalog) => catalog.defaultTeamRoles), ...surgeries.flatMap((surgery) => surgery.team.map((member) => member.role))])],
    }))),
    http.get(route("calendar"), answer((request) => {
      const query = new URL(request.url).searchParams;
      const from = Date.parse(query.get("from") ?? "");
      const to = Date.parse(query.get("to") ?? "");
      return HttpResponse.json(surgeries
        .filter((surgery) => Date.parse(surgery.scheduledAt) >= from && Date.parse(surgery.scheduledAt) < to)
        .map((surgery) => entry(surgery, request)));
    })),
    // Planned surgeries overlapping the probe in the same location or with a shared team member.
    http.get(route("conflicts"), answer((request) => {
      const query = new URL(request.url).searchParams;
      const start = Date.parse(query.get("scheduledAt") ?? "");
      const end = start + Number(query.get("durationMinutes") ?? 0) * minuteMilliseconds;
      const location = query.get("location")?.trim().toLowerCase();
      const teamUserIds = query.getAll("teamUserIds");
      return HttpResponse.json(surgeries.flatMap((surgery) => {
        if (surgery.id === query.get("excludeSurgeryId") || surgery.status !== surgeryStatus.planned) return [];
        if (Date.parse(surgery.scheduledAt) > end || ends(surgery) < start) return [];
        const sharedTeamUserIds = surgery.team.map((member) => member.userId).filter((userId) => teamUserIds.includes(userId));
        const reasons = [
          ...(location && surgery.location?.trim().toLowerCase() === location ? [conflictReason.sameLocation] : []),
          ...(sharedTeamUserIds.length ? [conflictReason.sharedTeamMember] : []),
        ];
        return reasons.length ? [{ entry: entry(surgery, request), reasons, sharedTeamUserIds }] : [];
      }));
    })),
    http.get(patientSurgeriesRoute, answer((request) => {
      const patientId = patientSurgeriesRoute.exec(new URL(request.url).pathname)?.[1];
      return HttpResponse.json(surgeries.filter((surgery) => surgery.patientId === patientId).map((surgery) => localized(surgery, request)));
    })),
    http.post(patientSurgeriesRoute, answer(async (request) => {
      const patientId = patientSurgeriesRoute.exec(new URL(request.url).pathname)?.[1] ?? "";
      const body = await readBody(request);
      // A retried plan answers with the surgery its first attempt created.
      const idempotencyKey = text(body, "idempotencyKey") || null;
      const existing = surgeries.find((surgery) => idempotencyKey && surgery.idempotencyKey === idempotencyKey);
      if (existing) return HttpResponse.json(existing);
      const surgery: MockSurgery = {
        id: store.nextId("mock-surgery"),
        patientId,
        idempotencyKey,
        ...writePlan(body),
        concurrencyToken: nextToken(),
        createdAt: new Date().toISOString(),
        updatedAt: null,
      };
      surgeries.push(surgery);
      return HttpResponse.json(surgery, { status: 201 });
    })),
    http.post(route("[^/]+\\/cancel"), changeStatus(surgeryStatus.cancelled)),
    http.post(route("[^/]+\\/complete"), changeStatus(surgeryStatus.completed)),
    http.get(route("[^/]+"), answer((request) => {
      const index = surgeryAt(request);
      return index < 0 ? notFound("Surgery does not exist in the mock store.") : HttpResponse.json(entry(surgeries[index], request));
    })),
    http.put(route("[^/]+"), answer(async (request) => {
      const index = surgeryAt(request);
      if (index < 0) return notFound("Surgery does not exist in the mock store.");
      const body = await readBody(request);
      if (text(body, "concurrencyToken") !== surgeries[index].concurrencyToken) return staleWrite();
      persianNames.delete(surgeries[index].id);
      surgeries[index] = { ...surgeries[index], ...writePlan(body), concurrencyToken: nextToken(), updatedAt: new Date().toISOString() };
      return HttpResponse.json(surgeries[index]);
    })),
  ];
}
