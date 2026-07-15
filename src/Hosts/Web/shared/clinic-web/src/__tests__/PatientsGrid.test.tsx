import { render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "../localization";
import { clinicMessageCatalogs } from "../localization";

const capturedDataGridProps = vi.hoisted(() => ({
  current: undefined as Record<string, unknown> | undefined,
}));

vi.mock("@heroui-pro/react", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@heroui-pro/react")>();

  return {
    ...actual,
    DataGrid: (props: Record<string, unknown>) => {
      capturedDataGridProps.current = props;
      return <div data-slot="data-grid" />;
    },
  };
});

import { PatientsGrid } from "../components/patients/list/PatientsGrid";

const messages = {
  ...clinicMessageCatalogs.en,
  common: {
    actions: { cancel: "Cancel", save: "Save", delete: "Delete" },
    values: { notAvailable: "Not available", unknown: "Unknown" },
    states: { loading: "Loading" },
  },
};

describe("PatientsGrid scroll ownership", () => {
  it("keeps virtualization, load-more, and sticky headers in its filling nested scrollport", () => {
    const onLoadMore = vi.fn();

    render(
      <LocalizationProvider locale="en" messages={messages}>
        <PatientsGrid
          descending={false}
          onLoadMore={onLoadMore}
          onSelectPatient={vi.fn()}
          onSort={vi.fn()}
          patients={[{ id: "patient-1", firstName: "Ada", lastName: "Lovelace" }]}
        />
      </LocalizationProvider>,
    );

    expect(capturedDataGridProps.current).toMatchObject({
      isLoadingMore: false,
      onLoadMore,
      virtualized: true,
    });
    expect(capturedDataGridProps.current?.scrollContainerClassName).toBe(
      "h-full min-h-0 overflow-y-auto after:block after:h-20 after:content-['']",
    );

    const columns = capturedDataGridProps.current?.columns as
      | Array<{ headerClassName?: string }>
      | undefined;
    expect(columns).toHaveLength(3);
    expect(columns?.every(({ headerClassName }) =>
      headerClassName === "sticky top-0 z-10 bg-surface-secondary"
    )).toBe(true);
  });
});
