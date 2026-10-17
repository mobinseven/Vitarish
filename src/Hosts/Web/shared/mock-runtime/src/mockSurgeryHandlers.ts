import { HttpResponse, http, type RequestHandler } from "msw";

type MockSurgeryHandlerContext = {
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

type Body = Record<string, unknown>;

const surgeriesPath = "\\/api\\/modules\\/clinic\\/surgeries\\/";
const route = (path: string) => new RegExp(`${surgeriesPath}${path}(?:\\?|$)`);
const catalogIdPattern = /\/surgeries\/catalogs\/([^/?]+)/;

const strings = (value: unknown) => (Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : []);
const text = (body: Body | null, key: string) => (typeof body?.[key] === "string" ? (body[key] as string) : "");

/**
 * Surgery types and the cancellation message, starting empty like a new practice:
 * the app then creates its default type and message in the viewer's language.
 */
export function createSurgeryMockHandlers({ isUnavailable, withDelay, unavailable, notFound }: MockSurgeryHandlerContext): RequestHandler[] {
  const catalogs: MockSurgeryCatalog[] = [];
  let settings: MockSurgerySettings | null = null;
  let version = 0;
  const nextToken = () => `mock-surgery-token-${++version}`;

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

  return [
    http.get(route("catalogs"), answer(() => HttpResponse.json(catalogs))),
    http.post(route("catalogs\\/default"), answer(async (request) => {
      if (!catalogs.length) catalogs.push(writeCatalog(await readBody(request), "mock-surgery-type-default"));
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
    // Suggestions come from earlier plans; a new practice has none beyond its types' roles.
    http.get(route("suggestions"), answer(() => HttpResponse.json({
      locations: [],
      roles: [...new Set(catalogs.flatMap((catalog) => catalog.defaultTeamRoles))],
    }))),
  ];
}
