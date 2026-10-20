import { HttpResponse, http, type RequestHandler } from "msw";
import type { MockStore } from "./mockStore";

type MockOnboardingHandlerContext = {
  store?: MockStore;
  isPersianRequest: (request: Request) => boolean;
  isUnavailable: boolean;
  withDelay: () => Promise<void>;
  unavailable: () => Response;
  notFound?: (detail: string) => Response;
};

type Body = Record<string, unknown>;

const baseDomain = "clinic.vitarish.test";
const subdomainPattern = /^[a-z0-9](?:[a-z0-9]|-(?=[a-z0-9])){1,28}[a-z0-9]$/;
const reservedSubdomains = new Set(["www", "admin", "api", "start", "app", "mail"]);
const seededTakenSubdomain = "rezaei-taken";
const mockHandoffToken = "mock-handoff-token-0000000000000000000000000";
const idempotencyKeyHeader = "Idempotency-Key";
const defaultSpecialty = "dermatology";

/** Mirrors ClinicSpecialties.All from src/Apps/Clinic/Contracts/ClinicSpecialties.cs. */
const clinicSpecialties = [
  "plastic-surgery",
  "dermatology",
  "dentistry",
  "ophthalmology",
  "otolaryngology",
  "general-surgery",
  "orthopedics",
  "obstetrics-gynecology",
  "urology",
  "cardiology",
  "internal-medicine",
  "pediatrics",
  "general-practice",
  "psychiatry",
  "physiotherapy",
  "nutrition",
  "other",
] as const;

const text = (body: Body | null, key: string) => (typeof body?.[key] === "string" ? (body[key] as string) : "");

/** Self-service clinic onboarding, handoff redemption, and tenant profile/logo answers for mock hosts. */
export function createOnboardingMockHandlers({
  isPersianRequest,
  isUnavailable,
  withDelay,
  unavailable,
}: MockOnboardingHandlerContext): RequestHandler[] {
  const takenSubdomains = new Set<string>([seededTakenSubdomain]);
  let logoUrl: string | null = null;
  let logoPromptDismissed = false;

  const answer = (respond: (request: Request) => Promise<Response> | Response) => async ({ request }: { request: Request }) => {
    await withDelay();
    if (isUnavailable) return unavailable();
    return respond(request);
  };
  const readBody = async (request: Request) => (await request.json().catch(() => null)) as Body | null;

  const checkSubdomain = (subdomain: string) => {
    if (!subdomainPattern.test(subdomain)) {
      return { subdomain, hostname: null, status: "Malformed" as const };
    }
    const status = reservedSubdomains.has(subdomain)
      ? ("Reserved" as const)
      : takenSubdomains.has(subdomain)
        ? ("Taken" as const)
        : ("Available" as const);
    return { subdomain, hostname: `${subdomain}.${baseDomain}`, status };
  };

  return [
    http.get(/\/api\/modules\/parties\/onboarding\/categories(?:\?|$)/, answer(() =>
      HttpResponse.json({ categories: [...clinicSpecialties] }),
    )),
    http.get(/\/api\/modules\/parties\/onboarding\/subdomain-availability(?:\?|$)/, answer((request) => {
      const subdomain = new URL(request.url).searchParams.get("subdomain") ?? "";
      return HttpResponse.json(checkSubdomain(subdomain));
    })),
    http.post(/\/api\/modules\/parties\/onboarding\/tenants(?:\?|$)/, answer(async (request) => {
      if (!request.headers.get(idempotencyKeyHeader)?.trim()) {
        return HttpResponse.json({ code: "Validation", message: "Idempotency-Key header is required." }, { status: 400 });
      }
      const body = await readBody(request);
      const subdomain = text(body, "subdomain");
      const availability = checkSubdomain(subdomain);
      if (availability.status === "Taken") {
        return HttpResponse.json({ code: "Conflict", message: "This subdomain is already taken." }, { status: 409 });
      }
      if (availability.status !== "Available" || !availability.hostname) {
        return HttpResponse.json({ code: "Validation", message: "Subdomain is not valid." }, { status: 400 });
      }
      takenSubdomains.add(subdomain);
      return HttpResponse.json(
        {
          tenantId: crypto.randomUUID(),
          hostname: availability.hostname,
          handoffToken: mockHandoffToken,
        },
        { status: 201 },
      );
    })),
    http.get(/\/api\/modules\/parties\/tenant-profile(?:\?|$)/, answer((request) =>
      HttpResponse.json({
        name: isPersianRequest(request) ? "مطب دکتر رضایی" : "Dr. Rezaei's Clinic",
        description: null,
        logoUrl,
        category: defaultSpecialty,
        businessDetails: null,
        logoPromptDismissed,
      }),
    )),
    http.put(/\/api\/modules\/parties\/tenant-profile\/logo(?:\?|$)/, answer(async (request) => {
      const fileId = text(await readBody(request), "fileId");
      logoUrl = `/api/modules/files/${fileId}/content`;
      return new HttpResponse(null, { status: 204 });
    })),
    http.put(/\/api\/modules\/parties\/tenant-profile\/logo-prompt-dismissal(?:\?|$)/, answer(() => {
      logoPromptDismissed = true;
      return new HttpResponse(null, { status: 204 });
    })),
    http.put(/\/api\/modules\/parties\/tenant-profile(?:\?|$)/, answer(() =>
      new HttpResponse(null, { status: 204 }),
    )),
    http.post(/\/api\/modules\/files\/upload(?:\?|$)/, answer(async (request) => {
      const formData = await request.formData().catch(() => null);
      const content = formData?.get("content");
      const file = content instanceof File ? content : null;
      const fileName = typeof formData?.get("fileName") === "string" ? (formData.get("fileName") as string) : (file?.name ?? "");
      const contentType = typeof formData?.get("contentType") === "string" ? (formData.get("contentType") as string) : (file?.type ?? "");
      const rawSize = formData?.get("size");
      const size = typeof rawSize === "string" && rawSize !== "" ? Number(rawSize) : (file?.size ?? 0);
      return HttpResponse.json({
        file: {
          id: crypto.randomUUID(),
          fileName,
          contentType,
          size,
        },
      });
    })),
    http.post(/\/api\/modules\/authentication\/handoff\/redeem(?:\?|$)/, answer(async (request) => {
      const token = text(await readBody(request), "token");
      if (token === mockHandoffToken) {
        return HttpResponse.json({ returnUrl: "/home", requiresProfileCompletion: false });
      }
      return HttpResponse.json(
        { status: 401, code: "invalid_credentials", title: "Invalid sign-in link" },
        { status: 401 },
      );
    })),
    http.put(/\/api\/modules\/users\/[^/]+\/profile(?:\?|$)/, answer(() =>
      new HttpResponse(null, { status: 204 }),
    )),
  ];
}
