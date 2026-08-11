import { chromium } from "@playwright/test";
import { mkdir } from "node:fs/promises";

const baseURL = process.env.PM_SCREENSHOT_BASE_URL ?? "http://127.0.0.1:3004";
const output = process.env.PM_SCREENSHOT_OUTPUT ?? "artifacts/vitarish-pm-screenshots/latest";
const projectId = "project-northstar";
const procedureId = "procedure-readiness";
const eventId = "event-check";

const pages = [
  ["01-overview", "/"],
  ["03-projects", "/projects"],
  ["04-project-new", "/projects/new"],
  ["05-project", `/projects/${projectId}`],
  ["06-project-edit", `/projects/${projectId}/edit`],
  ["07-procedure", `/projects/${projectId}/procedures/${procedureId}`],
  ["08-event", `/projects/${projectId}/procedures/${procedureId}/events/${eventId}`],
  ["09-roles", "/settings/roles"],
  ["10-statistics", "/settings/statistics"],
];

await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, colorScheme: "light" });
const page = await context.newPage();

let actionStatus = "Proposed";
const action = () => ({
  actionId: "action-demo-confirm",
  status: actionStatus,
  review: { operation: "update_event", proposedValuesJson: '{"title":"Run support readiness check","description":"Agent-prepared update for review."}' },
});

await page.route(`**/api/modules/sea/events/${eventId}`, async (route) =>
  route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ id: eventId, projectId, procedureId, title: "Run support readiness check", description: "Review open questions with the customer team.", isCommitted: false, committedAt: null, committedBy: "", stats: { events: 0, committedEvents: 0, eventItems: 3, newItems: 1, unread: true } }) }));
await page.route(`**/api/modules/sea/events/${eventId}/attachments`, async (route) =>
  route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([]) }));

await page.route("**/api/modules/authentication/session", async (route) =>
  route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({
    isAuthenticated: true,
    canAccessPlatformAdmin: false,
    user: { userId: "mock-user", displayName: "Farzad" },
    tenantAccess: { canAccessTenantOwner: true, canAccessTenantUser: true },
  }) }));
await page.route(`**/api/modules/sea/events/${eventId}/attachments/count`, async (route) =>
  route.fulfill({ status: 200, contentType: "application/json", body: "0" }));

await page.route("**/api/modules/intelligence/agent-conversations", async (route) =>
  route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ conversationId: "conversation-demo", result: { assistant: { messages: ["I prepared an event update for your review."] }, preparedActions: [{ actionId: "action-demo-confirm", operation: "update_event" }] } }) }));
await page.route(`**/api/apps/project-management/events/${eventId}/actions/action-demo-confirm`, async (route) =>
  route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(action()) }));
await page.route(`**/api/apps/project-management/events/${eventId}/actions/action-demo-confirm/confirm`, async (route) => {
  actionStatus = "Completed";
  await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(action()) });
});

for (const [name, path] of pages) {
  await page.goto(`${baseURL}${path}`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1200);
  await page.screenshot({ path: `${output}/${name}.png`, fullPage: true });
}

await page.goto(`${baseURL}/projects/${projectId}/procedures/${procedureId}/events/${eventId}`, { waitUntil: "domcontentloaded" });
await page.waitForTimeout(1200);
await page.locator("button").filter({ hasText: /assistant|دستیار/i }).first().click({ timeout: 10_000 });
await page.locator("textarea").fill("Prepare the event update");
await page.locator("button").filter({ hasText: /send|ارسال/i }).last().click();
await page.waitForTimeout(1500);
await page.screenshot({ path: `${output}/10-agent-proposed-confirmation.png`, fullPage: true });
const confirmButton = page.locator("button").filter({ hasText: /confirm|تایید/i }).first();
if (await confirmButton.count()) {
  await confirmButton.click();
  await page.waitForTimeout(800);
}
await page.screenshot({ path: `${output}/11-agent-confirmed.png`, fullPage: true });

await browser.close();
console.log(`Created ${pages.length + 2} PM screenshots in ${output}`);
