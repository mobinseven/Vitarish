import { HttpResponse, http, type RequestHandler } from "msw";
import { ProgressState, RoleType } from "@vitarish/sea/api/generated/sea/model";
import type { MockSeaAttachment, MockSeaEvent, MockStore } from "./mockStore";

type MockSeaHandlerContext = {
  store: MockStore;
  isUnavailable: boolean;
  withDelay: () => Promise<void>;
  unavailable: () => Response;
  notFound: (detail: string) => Response;
};
type Body = Record<string, unknown>;

const seaPathPattern = /\/api\/modules\/sea\/([^?]+)/;
const mockSeaUser = "mock-user";
const mockSeaNow = "2026-02-10T11:00:00.000Z";
// Second path segments that name a sub-resource rather than an id, as in attachments/text.
const literalSecondSegments = new Set(["text", "file", "batch"]);

/** Mirrors SEA's Progress rule: committed share of events mapped to a coarse state. */
function progressState(completed: number, total: number) {
  const share = total === 0 ? 0 : completed / total;
  if (share === 0) return ProgressState.NotStarted;
  if (share <= 0.25) return ProgressState.Quarter;
  if (share <= 0.5) return ProgressState.Half;
  if (share <= 0.75) return ProgressState.ThreeQuarters;
  return share < 1 ? ProgressState.InProgress : ProgressState.Completed;
}

const text = (body: Body | null, key: string) => (typeof body?.[key] === "string" ? body[key] as string : undefined);

/** A stateful SEA module for mock hosts: templates, patient procedures, ordered events, items, and roles. */
export function createSeaMockHandlers({ store, isUnavailable, withDelay, unavailable, notFound }: MockSeaHandlerContext): RequestHandler[] {
  const sea = store.sea;
  const live = <T extends { isDeleted: boolean }>(records: T[]) => records.filter((record) => !record.isDeleted);
  const ok = (entityId?: string, status = 200) => HttpResponse.json({ succeeded: true, entityId }, { status });
  const attachmentsOf = (eventId: string) => live(sea.attachments).filter((item) => item.eventId === eventId);
  const eventsOf = (procedureId: string) => live(sea.events).filter((event) => event.procedureId === procedureId);
  const proceduresOf = (projectId: string) => live(sea.procedures).filter((procedure) => procedure.projectId === projectId);
  // Last activity is the newest edit, commit, or item among the summarized events.
  const stats = (procedures: number, events: MockSeaEvent[]) => {
    const items = events.flatMap((event) => attachmentsOf(event.id));
    const committedEvents = events.filter((event) => event.isCommitted).length;
    const activity = [...events.flatMap((event) => [event.updatedAt, event.committedAt]), ...items.map((item) => item.createdAt)]
      .filter((value): value is string => value !== null)
      .sort();
    return {
      state: progressState(committedEvents, events.length),
      procedures,
      events: events.length,
      committedEvents,
      eventItems: items.length,
      newItems: 0,
      unread: false,
      lastUpdate: activity.at(-1) ?? null,
    };
  };

  const readEvent = (event: MockSeaEvent) => {
    const siblings = eventsOf(event.procedureId);
    const index = siblings.indexOf(event);
    return { ...event, previousEventId: index > 0 ? siblings[index - 1].id : null, stats: stats(0, [event]) };
  };
  const readProcedure = (procedure: (typeof sea.procedures)[number]) => ({ ...procedure, stats: stats(1, eventsOf(procedure.id)) });
  const readProject = (project: (typeof sea.projects)[number]) => {
    const procedures = proceduresOf(project.id);
    return { ...project, stats: stats(procedures.length, procedures.flatMap((procedure) => eventsOf(procedure.id))) };
  };
  const roleNames = (eventId: string, type: RoleType) => live(sea.eventRoles)
    .filter((assignment) => assignment.eventId === eventId && assignment.type === type)
    .map((assignment) => sea.roles.find((role) => role.id === assignment.roleId)?.name ?? assignment.roleId);
  const created = (id: string) => ({ id, createdAt: mockSeaNow, createdBy: mockSeaUser, updatedAt: null, isDeleted: false });
  const touch = (record: { updatedAt: string | null }) => { record.updatedAt = mockSeaNow; };
  const find = <T extends { id: string; isDeleted: boolean }>(records: T[], id: string) => records.find((record) => record.id === id && !record.isDeleted);
  const remove = (records: Array<{ id: string; isDeleted: boolean }>, id: string, kind: string) => {
    const record = find(records, id);
    if (!record) return notFound(`SEA ${kind} '${id}' was not found.`);
    record.isDeleted = true;
    return ok(id);
  };
  const insertEvent = (body: Body | null, previousEventId: string | undefined) => {
    const procedureId = text(body, "procedureId") ?? "";
    const previous = previousEventId ? find(sea.events, previousEventId) : undefined;
    if (previousEventId && !previous) return notFound(`SEA event '${previousEventId}' was not found.`);
    const event: MockSeaEvent = {
      ...created(store.nextId("mock-sea-event")),
      projectId: text(body, "projectId") ?? previous?.projectId ?? "",
      procedureId: previous?.procedureId ?? procedureId,
      title: text(body, "title") ?? "",
      description: text(body, "description") ?? "",
      committedAt: null,
      committedBy: "",
      isCommitted: false,
    };
    // A first event is placed before the procedure's existing events; a following event right after its predecessor.
    const anchor = previous ? sea.events.indexOf(previous) + 1 : sea.events.findIndex((candidate) => candidate.procedureId === event.procedureId);
    sea.events.splice(anchor < 0 ? sea.events.length : anchor, 0, event);
    return ok(event.id, 201);
  };
  const moveEvent = (id: string, offset: -1 | 1) => {
    const event = find(sea.events, id);
    if (!event) return notFound(`SEA event '${id}' was not found.`);
    const siblings = eventsOf(event.procedureId);
    const neighbour = siblings[siblings.indexOf(event) + offset];
    if (neighbour) {
      const [from, to] = [sea.events.indexOf(event), sea.events.indexOf(neighbour)];
      [sea.events[from], sea.events[to]] = [neighbour, event];
    }
    return ok(id);
  };
  const addAttachment = (body: Body | null) => {
    const attachment: MockSeaAttachment = {
      ...created(store.nextId("mock-sea-attachment")),
      eventId: text(body, "eventId") ?? "",
      fileName: text(body, "fileName") ?? null,
      content: text(body, "content") ?? "",
      contentType: text(body, "contentType") ?? "text/plain",
      size: typeof body?.size === "number" ? body.size : 0,
      kind: typeof body?.kind === "number" ? body.kind : 0,
    };
    sea.attachments.push(attachment);
    return attachment.id;
  };

  async function route(method: string, segments: string[], body: Body | null): Promise<Response> {
    const [resource, id, action, subAction] = segments;
    // POST events/after creates a procedure's first event; POST events/after/{previousEventId} follows one.
    if (method === "POST" && resource === "events" && id === "after" && subAction === undefined) return insertEvent(body, action);
    const idSegment = id === undefined ? "" : `/${literalSecondSegments.has(id) ? id : ":id"}`;
    const key = `${method} ${resource}${idSegment}${action === undefined ? "" : `/${action}`}${subAction === undefined ? "" : `/${subAction}`}`;
    switch (key) {
      case "GET health": return HttpResponse.json({ status: "Healthy" });
      case "GET capabilities": return HttpResponse.json({ canManage: true });
      case "GET statistics": return HttpResponse.json({
        projects: live(sea.projects).length,
        procedures: live(sea.procedures).length,
        events: live(sea.events).length,
        roles: live(sea.roles).length,
        attachments: live(sea.attachments).length,
        reminders: 0,
      });

      case "GET projects": return HttpResponse.json(live(sea.projects).map(readProject));
      case "POST projects": {
        const project = { ...created(store.nextId("mock-sea-project")), name: text(body, "name") ?? "", description: text(body, "description") ?? "", identificationCode: text(body, "identificationCode") ?? null, template: body?.template === true, userIds: [mockSeaUser] };
        sea.projects.push(project);
        return ok(project.id, 201);
      }
      case "PUT projects": {
        const project = find(sea.projects, text(body, "id") ?? "");
        if (!project) return notFound("SEA project was not found.");
        Object.assign(project, { name: text(body, "name") ?? project.name, description: text(body, "description") ?? project.description });
        touch(project);
        return ok(project.id);
      }
      case "GET projects/:id": {
        const project = find(sea.projects, id);
        return project ? HttpResponse.json(readProject(project)) : notFound(`SEA project '${id}' was not found.`);
      }
      case "DELETE projects/:id": return remove(sea.projects, id, "project");
      case "GET projects/:id/procedures": return HttpResponse.json(proceduresOf(id).map(readProcedure));
      case "POST projects/:id/read":
      case "POST projects/:id/unread":
      case "POST procedures/:id/read":
      case "POST procedures/:id/unread":
      case "POST events/:id/read":
      case "POST events/:id/unread": return ok(id);

      case "POST procedures": {
        const procedure = { ...created(store.nextId("mock-sea-procedure")), projectId: text(body, "projectId") ?? "", name: text(body, "name") ?? "", description: text(body, "description") ?? null };
        sea.procedures.push(procedure);
        return ok(procedure.id, 201);
      }
      case "PUT procedures": {
        const procedure = find(sea.procedures, text(body, "id") ?? "");
        if (!procedure) return notFound("SEA procedure was not found.");
        Object.assign(procedure, { name: text(body, "name") ?? procedure.name, description: text(body, "description") ?? procedure.description });
        touch(procedure);
        return ok(procedure.id);
      }
      case "GET procedures/:id": {
        const procedure = find(sea.procedures, id);
        return procedure ? HttpResponse.json(readProcedure(procedure)) : notFound(`SEA procedure '${id}' was not found.`);
      }
      case "DELETE procedures/:id": return remove(sea.procedures, id, "procedure");
      case "GET procedures/:id/events": return HttpResponse.json(eventsOf(id).map(readEvent));

      case "PUT events": {
        const event = find(sea.events, text(body, "id") ?? "");
        if (!event) return notFound("SEA event was not found.");
        Object.assign(event, { title: text(body, "title") ?? event.title, description: text(body, "description") ?? event.description });
        touch(event);
        return ok(event.id);
      }
      case "GET events/:id": {
        const event = find(sea.events, id);
        return event ? HttpResponse.json(readEvent(event)) : notFound(`SEA event '${id}' was not found.`);
      }
      case "DELETE events/:id": return remove(sea.events, id, "event");
      case "GET events/:id/capabilities": return find(sea.events, id)
        ? HttpResponse.json({ canRead: true, canWrite: true, canManage: true })
        : notFound(`SEA event '${id}' was not found.`);
      case "GET events/:id/permissions/write": return HttpResponse.json(find(sea.events, id) !== undefined);
      case "POST events/:id/move-up": return moveEvent(id, -1);
      case "POST events/:id/move-down": return moveEvent(id, 1);
      case "POST events/:id/commit":
      case "POST events/:id/uncommit": {
        const event = find(sea.events, id);
        if (!event) return notFound(`SEA event '${id}' was not found.`);
        const isCommitted = action === "commit";
        Object.assign(event, { isCommitted, committedAt: isCommitted ? mockSeaNow : null, committedBy: isCommitted ? mockSeaUser : "" });
        touch(event);
        return ok(id);
      }
      case "GET events/:id/attachments": return HttpResponse.json(attachmentsOf(id));
      case "GET events/:id/attachments/count": return HttpResponse.json(attachmentsOf(id).length);
      case "GET events/:id/roles": return HttpResponse.json([...roleNames(id, RoleType.Read), ...roleNames(id, RoleType.Write)]);
      case "GET events/:id/roles/read": return HttpResponse.json(roleNames(id, RoleType.Read));
      case "GET events/:id/roles/write": return HttpResponse.json(roleNames(id, RoleType.Write));

      case "POST attachments/text":
      case "POST attachments/file": return ok(addAttachment(body), 201);
      case "POST attachments/batch": {
        const items = Array.isArray(body) ? body as Body[] : [];
        return ok(items.map(addAttachment).at(-1), 201);
      }
      case "PUT attachments/text": {
        const attachment = find(sea.attachments, text(body, "id") ?? "");
        if (!attachment) return notFound("SEA attachment was not found.");
        Object.assign(attachment, { content: text(body, "content") ?? attachment.content });
        touch(attachment);
        return ok(attachment.id);
      }
      case "DELETE attachments/:id": return remove(sea.attachments, id, "attachment");

      case "GET roles": return HttpResponse.json(live(sea.roles));
      case "POST roles": {
        const role = { ...created(store.nextId("mock-sea-role")), name: text(body, "name") ?? "" };
        sea.roles.push(role);
        return ok(role.id, 201);
      }
      case "PUT roles": {
        const role = find(sea.roles, text(body, "id") ?? "");
        if (!role) return notFound("SEA role was not found.");
        role.name = text(body, "name") ?? role.name;
        touch(role);
        return ok(role.id);
      }
      case "DELETE roles/:id": return remove(sea.roles, id, "role");
      case "GET users/:id/roles": return HttpResponse.json([]);
      case "POST user-roles": return ok(store.nextId("mock-sea-user-role"), 201);
      case "DELETE user-roles/:id": return ok(id);
      case "POST event-roles": {
        const type = text(body, "type") === RoleType.Write ? RoleType.Write : RoleType.Read;
        const assignment = { ...created(store.nextId("mock-sea-event-role")), eventId: text(body, "eventId") ?? "", roleId: text(body, "roleId") ?? "", type };
        sea.eventRoles.push(assignment);
        return ok(assignment.id, 201);
      }
      case "DELETE event-roles/:id": return remove(sea.eventRoles, id, "event role");
      default: return notFound(`Unknown SEA route '${key}'.`);
    }
  }

  return [
    http.all(seaPathPattern, async ({ request }) => {
      await withDelay();
      if (isUnavailable) return unavailable();
      const path = seaPathPattern.exec(new URL(request.url).pathname)?.[1] ?? "";
      const segments = path.split("/").filter(Boolean).map(decodeURIComponent);
      const body = request.method === "GET" || request.method === "DELETE"
        ? null
        : await request.json().catch(() => null) as Body | null;
      return route(request.method, segments, body);
    }),
  ];
}
