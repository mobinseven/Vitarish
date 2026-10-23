import { HttpResponse, http, type RequestHandler } from "msw";

type MockMemoryHandlerContext = {
  isPersianRequest: (request: Request) => boolean;
  isEmpty: boolean;
  isUnavailable: boolean;
  withDelay: () => Promise<void>;
  unavailable: () => Response;
  notFound: (detail: string) => Response;
};

type MockMemoryItem = { id: string; text: { en: string; fa: string }; source: "Landing" | "Onboarding"; rememberedAt: string };

/**
 * Mock of the signed-in person's memory (`/api/modules/intelligence/memory`, ADR-0017); the shape
 * follows UserMemoryContracts in src/Modules/Intelligence/Contracts.
 */
export function createMemoryMockHandlers({ isPersianRequest, isEmpty, isUnavailable, withDelay, unavailable, notFound }: MockMemoryHandlerContext): RequestHandler[] {
  let items: MockMemoryItem[] = isEmpty ? [] : [
    { id: "mock-memory-kind", text: { en: "Runs a dental clinic with two providers", fa: "یک کلینیک دندان‌پزشکی با دو پزشک دارد" }, source: "Landing", rememberedAt: "2026-10-20T09:30:00.000Z" },
    { id: "mock-memory-booking", text: { en: "Patients book by phone today", fa: "بیماران الان تلفنی نوبت می‌گیرند" }, source: "Landing", rememberedAt: "2026-10-20T09:31:00.000Z" },
  ];
  const itemId = (request: Request) => decodeURIComponent(new URL(request.url).pathname.split("/").at(-1) ?? "");
  const answer = (respond: (request: Request) => Promise<Response> | Response) => async ({ request }: { request: Request }) => {
    await withDelay();
    if (isUnavailable) return unavailable();
    return respond(request);
  };

  // Mirrors the server's limit, which it sends with the memory.
  const mockMaxTextLength = 280;

  return [
    http.get(/\/api\/modules\/intelligence\/memory(?:\?|$)/, answer((request) =>
      HttpResponse.json({
        items: items.map((item) => ({ id: item.id, text: isPersianRequest(request) ? item.text.fa : item.text.en, source: item.source, rememberedAt: item.rememberedAt })),
        maxTextLength: mockMaxTextLength,
      }),
    )),
    http.put(/\/api\/modules\/intelligence\/memory\/[^/?]+(?:\?|$)/, answer(async (request) => {
      const id = itemId(request);
      const body = (await request.json().catch(() => null)) as { text?: unknown } | null;
      const text = typeof body?.text === "string" ? body.text.trim() : "";
      if (!text || text.length > mockMaxTextLength) return HttpResponse.json({ code: "ValidationFailure", message: "Remembered text is required." }, { status: 400 });
      if (!items.some((item) => item.id === id)) return notFound("Memory item does not exist in the mock store.");
      items = items.map((item) => (item.id === id ? { ...item, text: { en: text, fa: text } } : item));
      return new HttpResponse(null, { status: 204 });
    })),
    http.delete(/\/api\/modules\/intelligence\/memory\/[^/?]+(?:\?|$)/, answer((request) => {
      const id = itemId(request);
      if (!items.some((item) => item.id === id)) return notFound("Memory item does not exist in the mock store.");
      items = items.filter((item) => item.id !== id);
      return new HttpResponse(null, { status: 204 });
    })),
    http.delete(/\/api\/modules\/intelligence\/memory(?:\?|$)/, answer(() => {
      items = [];
      return new HttpResponse(null, { status: 204 });
    })),
  ];
}
