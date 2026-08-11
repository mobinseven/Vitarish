import { http, HttpResponse, type JsonBodyType } from "msw";

const tracked = (id: string) => ({
  id,
  createdAt: "2026-08-01T08:00:00.000Z",
  createdBy: "mock-user",
  updatedAt: null,
  updatedBy: null,
  deletedAt: null,
  deletedBy: null,
  isDeleted: false,
  metadata: null,
  metadataValues: null,
});

const stats = (events: number, committedEvents: number, newItems = 0) => ({
  state: "InProgress",
  procedures: 0,
  events,
  committedEvents,
  eventItems: 3,
  newItems,
  unread: newItems > 0,
  lastUpdate: "2026-08-08T07:00:00.000Z",
});

const project = {
  ...tracked("project-northstar"),
  name: "Northstar rollout",
  description:
    "Coordinate the launch across product, operations, and customer teams.",
  avatarFileId: null,
  identificationCode: "NS-24",
  template: false,
  userIds: ["mock-user"],
  stats: { ...stats(8, 5, 2), procedures: 3 },
};

const procedures = [
  {
    id: "procedure-readiness",
    name: "Launch readiness",
    description:
      "Prepare the product, people, and support surface for release.",
    stats: stats(3, 2, 1),
  },
  {
    id: "procedure-handoff",
    name: "Operational handoff",
    description: "Turn launch decisions into a repeatable operating rhythm.",
    stats: stats(2, 1),
  },
  {
    id: "procedure-review",
    name: "Post-launch review",
    description: "Capture outcomes and convert learning into next actions.",
    stats: stats(3, 2, 1),
  },
].map((procedure) => ({
  ...tracked(procedure.id),
  ...procedure,
  projectId: project.id,
}));

const events = [
  [
    "event-brief",
    "Confirm launch brief",
    true,
    "Align the decision record and the release narrative.",
  ],
  [
    "event-owners",
    "Name operational owners",
    true,
    "Make ownership visible before handoff.",
  ],
  [
    "event-check",
    "Run support readiness check",
    false,
    "Review open questions with the customer team.",
  ],
  [
    "event-go",
    "Publish go-live note",
    false,
    "Share the final decision with the launch group.",
  ],
].map(([id, title, isCommitted, description], index) => ({
  ...tracked(String(id)),
  projectId: project.id,
  procedureId: procedures[0].id,
  previousEventId:
    index === 0
      ? null
      : (["event-brief", "event-owners", "event-check"][index - 1] ?? null),
  committedAt: isCommitted ? "2026-08-07T09:00:00.000Z" : null,
  committedBy: isCommitted ? "mock-user" : "",
  title: String(title),
  description: String(description),
  isCommitted: Boolean(isCommitted),
  stats: stats(0, 0),
}));

const attachments = [
  {
    ...tracked("attachment-brief"),
    eventId: events[0].id,
    fileName: null,
    content:
      "Decision record v3 — launch narrative approved by product and operations.",
    contentType: "text/plain",
    contentThumbnail: null,
    size: 74,
    kind: 0,
  },
];

const json = (value: JsonBodyType, status = 200) =>
  HttpResponse.json(value, { status });

export const richSeaHandlers = [
  http.get("*/api/modules/sea/projects", () => json([project])),
  http.get(`*/api/modules/sea/projects/${project.id}`, () => json(project)),
  http.get(`*/api/modules/sea/projects/${project.id}/procedures`, () =>
    json(procedures),
  ),
  http.get(`*/api/modules/sea/procedures/${procedures[0].id}`, () =>
    json(procedures[0]),
  ),
  http.get(`*/api/modules/sea/procedures/${procedures[0].id}/events`, () =>
    json(events),
  ),
  ...events.map((event) =>
    http.get(`*/api/modules/sea/events/${event.id}`, () => json(event)),
  ),
  ...events.map((event) =>
    http.get(`*/api/modules/sea/events/${event.id}/attachments`, () =>
      json(attachments.filter((item) => item.eventId === event.id)),
    ),
  ),
  ...events.map((event) =>
    http.get(`*/api/modules/sea/events/${event.id}/attachments/count`, () =>
      json(attachments.filter((item) => item.eventId === event.id).length),
    ),
  ),
  http.post("*/api/modules/sea/events/:eventId/commit", ({ params }) => {
    const event = events.find((item) => item.id === params.eventId);
    if (event) event.isCommitted = true;
    return json({ succeeded: true });
  }),
  http.post("*/api/modules/sea/events/:eventId/uncommit", ({ params }) => {
    const event = events.find((item) => item.id === params.eventId);
    if (event) event.isCommitted = false;
    return json({ succeeded: true });
  }),
];
