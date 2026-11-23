import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { GuidedSurfaceSurfaceResponse } from "../api/generated/intelligence/model";
import { PresentedSurface } from "../surfaces/PresentedSurface";
import { PresentedSurfaceAppearanceProvider, type PresentedSurfaceAppearance } from "../surfaces/PresentedSurfaceContext";
import { presentedSurfaceCatalogId } from "../surfaces/surfaceCatalog";

afterEach(cleanup);

const candidate = (reference: string, name: string, dateOfBirth: string) => ({
  kind: "clinic.patient-candidate",
  reference,
  title: { type: "Text", value: name },
  fields: {
    dateOfBirth: { type: "Date", value: dateOfBirth },
    maskedPhone: { type: "Text", value: "0912***1234" },
  },
});

const surface = (components: unknown[], dataModel: unknown): GuidedSurfaceSurfaceResponse => ({
  surfaceId: "call_1",
  protocol: "a2ui",
  protocolVersion: "1.0",
  catalogId: presentedSurfaceCatalogId,
  components,
  dataModel,
} as GuidedSurfaceSurfaceResponse);

const appearance = (overrides: Partial<PresentedSurfaceAppearance> = {}): PresentedSurfaceAppearance => ({
  fieldLabel: (_kind, field) => ({ dateOfBirth: "Born", maskedPhone: "Phone", count: "Appointments" })[field] ?? field,
  ...overrides,
});

describe("PresentedSurface", () => {
  it("fills a model-laid-out list with the records the server read, and opens the chosen one", async () => {
    const openRecord = vi.fn();
    render(
      <PresentedSurfaceAppearanceProvider value={appearance({ openRecord })}>
        <PresentedSurface
          surface={surface(
            [
              { id: "root", component: "Column", children: ["title", "list"] },
              { id: "title", component: "Heading", text: "Matching patients" },
              { id: "list", component: "RecordList", result: { path: "/r1" }, fields: ["dateOfBirth", "maskedPhone"] },
            ],
            { r1: { kind: "clinic.patient-candidate", isList: true, hasMore: false, records: [candidate("tok-1", "Sara Ahmadi", "1990-04-02"), candidate("tok-2", "Sara Karimi", "1985-01-20")] } },
          )}
        />
      </PresentedSurfaceAppearanceProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Matching patients" })).toBeVisible();
    expect(screen.getByText("Sara Ahmadi")).toBeVisible();
    expect(screen.getAllByText(/Phone: 0912\*\*\*1234/)).toHaveLength(2);
    fireEvent.click(screen.getAllByRole("button", { name: "intelligence.surface.openRecord" })[1]!);
    await waitFor(() => expect(openRecord).toHaveBeenCalledWith(expect.objectContaining({ reference: "tok-2", kind: "clinic.patient-candidate" })));
  });

  it("caps a long list and offers the full screen", async () => {
    const showAll = vi.fn();
    const records = Array.from({ length: 7 }, (_, index) => candidate(`tok-${index}`, `Patient ${index}`, "1990-01-01"));
    render(
      <PresentedSurfaceAppearanceProvider value={appearance({ showAll })}>
        <PresentedSurface surface={surface([{ id: "root", component: "RecordList", result: { path: "/r1" }, fields: ["dateOfBirth"] }], { r1: { kind: "clinic.patient-candidate", isList: true, hasMore: true, records } })} />
      </PresentedSurfaceAppearanceProvider>,
    );

    expect(await screen.findByText("Patient 4")).toBeVisible();
    expect(screen.queryByText("Patient 5")).toBeNull();
    expect(screen.getByText("intelligence.surface.moreExist")).toBeVisible();
    fireEvent.click(screen.getByRole("button", { name: /intelligence.surface.showAll/ }));
    expect(showAll).toHaveBeenCalledWith("clinic.patient-candidate");
  });

  it("shows a bound number with its localized label", async () => {
    render(
      <PresentedSurfaceAppearanceProvider value={appearance()}>
        <PresentedSurface surface={surface([{ id: "root", component: "Metric", result: { path: "/r1" }, field: "count" }], { r1: { kind: "clinic.appointment-count", isList: false, hasMore: false, records: [{ kind: "clinic.appointment-count", reference: "2026-11-20", title: { type: "Date", value: "2026-11-20" }, fields: { count: { type: "Number", value: 12 } } }] } })} />
      </PresentedSurfaceAppearanceProvider>,
    );

    expect(await screen.findByText("Appointments")).toBeVisible();
    expect(screen.getByText("12")).toBeVisible();
  });

  it("renders nothing for a surface in another catalog", async () => {
    const catalogId = "other";
    const diagnostic = vi.spyOn(console, "error").mockImplementation(() => undefined);
    try {
      const { container } = render(<PresentedSurface surface={{ ...surface([{ id: "root", component: "Heading", text: "x" }], {}), catalogId }} />);
      await waitFor(() => expect(diagnostic).toHaveBeenCalledWith(expect.stringContaining("uses an unknown catalog."), catalogId));
      expect(container).toBeEmptyDOMElement();
    } finally {
      diagnostic.mockRestore();
    }
  });
});
