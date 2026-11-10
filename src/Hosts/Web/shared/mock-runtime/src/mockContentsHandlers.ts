import { HttpResponse, http, type RequestHandler } from "msw";

type MockContentsHandlerContext = {
  isEmpty: boolean;
  isUnavailable: boolean;
  withDelay: () => Promise<void>;
  unavailable: () => Response;
  notFound: (detail: string) => Response;
};

const siteId = "00000000-0000-4000-8000-00000000c001";
const pageTypeId = "00000000-0000-4000-8000-00000000c101";
const revisionTime = "2026-11-02T09:30:00.000Z";

const sites = [{ id: siteId, key: "vitarish-landing", name: "Vitarish landing", defaultLocale: "en", locales: ["en", "fa"] }];

const types = [{
  id: pageTypeId,
  key: "editorial-page",
  name: "Editorial page",
  description: "A long-form page of the public site.",
  currentSchemaVersion: 1,
  fields: [
    { key: "title", label: "Heading", kind: "Text", required: true, localized: true, maxLength: 120 },
    { key: "body", label: "Body", kind: "RichText", required: true, localized: true },
    { key: "related", label: "Related page", kind: "Reference", required: false, localized: false },
  ],
}];

const paragraph = (text: string) => ({ type: "doc", content: [{ type: "paragraph", content: [{ type: "text", text }] }] });

const entries = [
  { title: "Our purpose", slug: "our-purpose", status: "Published", publishedAt: "2026-11-01T08:00:00.000Z", heading: { en: "Our purpose", fa: "از آنچه برای شما مهم است آغاز می‌کنیم" } },
  { title: "How Darman keeps records safe", slug: "records-safety", status: "Draft", publishedAt: null, heading: { en: "Keeping records safe" } },
  { title: "از آنچه برای شما مهم است آغاز می‌کنیم", slug: "start-with-you", status: "Scheduled", publishedAt: null, heading: { fa: "از آنچه برای شما مهم است آغاز می‌کنیم" } },
  { title: "Spring announcement", slug: "spring-announcement", status: "Archived", publishedAt: "2026-03-20T08:00:00.000Z", heading: { en: "Spring announcement" } },
].map((entry, index) => {
  const id = `00000000-0000-4000-8000-00000000c2${String(index).padStart(2, "0")}`;
  return {
    id,
    siteId,
    contentTypeId: pageTypeId,
    schemaVersion: 1,
    slug: entry.slug,
    canonicalPath: `/${entry.slug}`,
    title: entry.title,
    excerpt: null,
    fields: { title: entry.heading, body: { en: paragraph("The page body in English."), fa: paragraph("متن صفحه به فارسی.") } },
    version: 3,
    status: entry.status,
    workingRevisionId: `${id}-r3`,
    publishedRevisionId: entry.publishedAt ? `${id}-r2` : null,
    publishedAt: entry.publishedAt,
  };
});

const contentsPath = (resource: string) => new RegExp(`/api/contents/${resource}(?:\\?|$)`);

/** A read-mostly contents studio for mock hosts: one bilingual site, one page type and entries in every status. */
export function createContentsMockHandlers({ isEmpty, isUnavailable, withDelay, unavailable, notFound }: MockContentsHandlerContext): RequestHandler[] {
  const guard = async () => { await withDelay(); return isUnavailable ? unavailable() : null; };
  return [
    http.get(contentsPath("sites"), async () => (await guard()) ?? HttpResponse.json(sites)),
    http.get(contentsPath("types"), async () => (await guard()) ?? HttpResponse.json(types)),
    http.get(contentsPath("entries"), async ({ request }) => {
      const failure = await guard();
      if (failure) return failure;
      const query = new URL(request.url).searchParams;
      const status = query.get("status");
      return HttpResponse.json(isEmpty ? [] : entries.filter((entry) => !status || entry.status === status));
    }),
    http.get(contentsPath("entries/([^/?]+)/revisions"), async ({ request }) => {
      const failure = await guard();
      if (failure) return failure;
      const id = new URL(request.url).pathname.split("/").at(-2);
      const entry = entries.find((item) => item.id === id);
      if (!entry) return notFound("Entry not found.");
      return HttpResponse.json([3, 2, 1].map((version) => ({ id: `${entry.id}-r${version}`, version, schemaVersion: 1, slug: entry.slug, canonicalPath: entry.canonicalPath, title: entry.title, excerpt: null, fields: entry.fields, createdBy: "mock-user", createdAt: revisionTime })));
    }),
    http.get(contentsPath("entries/([^/?]+)"), async ({ request }) => {
      const failure = await guard();
      if (failure) return failure;
      const entry = entries.find((item) => item.id === new URL(request.url).pathname.split("/").at(-1));
      return entry ? HttpResponse.json(entry) : notFound("Entry not found.");
    }),
  ];
}
