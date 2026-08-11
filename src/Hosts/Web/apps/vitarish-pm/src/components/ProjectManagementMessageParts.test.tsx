import { render, screen } from "@testing-library/react";
import React from "react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@heroui/react", async () => {
  const React = await import("react");
  const passthrough = ({ children, ...props }: any) =>
    React.createElement("div", props, children);
  const Card = Object.assign(passthrough, {
    Header: passthrough,
    Title: passthrough,
    Description: passthrough,
    Content: passthrough,
  });
  const Disclosure = Object.assign(passthrough, {
    Trigger: passthrough,
    Indicator: passthrough,
    Content: passthrough,
    Body: passthrough,
  });
  const Tooltip = Object.assign(passthrough, { Content: passthrough });
  return {
    Button: ({ children }: any) =>
      React.createElement("button", undefined, children),
    Card,
    Chip: passthrough,
    Disclosure,
    Tooltip,
  };
});
vi.mock("@vitarish/web-localization", () => ({
  formatDateTime: () => "localized date",
  formatFileSize: () => "4 KB",
  formatNumber: (value: number) => String(value),
  useLocalizedText: () => ({
    locale: "en",
    t: (key: string, values?: Record<string, string>) =>
      key === "pm.cards.itemCount" ? `${values?.count} items` : key,
  }),
}));

import { ProjectManagementMessageParts } from "./ProjectManagementMessageParts";

describe("ProjectManagementMessageParts", () => {
  it("renders project snapshots as human-readable summaries rather than raw field names", () => {
    render(
      <ProjectManagementMessageParts
        parts={[
          {
            type: "resource-card",
            family: "project",
            operation: "get_project",
            id: "project-1",
            routeKind: "project",
            routeCoordinates: {},
            capturedAt: "2026-08-09T08:00:00Z",
            fields: {
              name: "Northstar readiness",
              description: "Prepare customer support.",
              updatedAt: "2026-08-09T08:00:00Z",
              id: "project-1",
            },
            items: [],
          },
        ]}
      />,
    );

    expect(screen.getByText("Northstar readiness")).toBeInTheDocument();
    expect(screen.getByText("Prepare customer support.")).toBeInTheDocument();
    expect(screen.getByText("pm.cards.updated")).toBeInTheDocument();
    expect(screen.queryByText("name")).not.toBeInTheDocument();
    expect(screen.queryByText("id")).not.toBeInTheDocument();
  });

  it("keeps collection entries as separate rows with localized semantic event state", () => {
    render(
      <ProjectManagementMessageParts
        parts={[
          {
            type: "resource-card",
            family: "event",
            operation: "list_events",
            routeKind: "event",
            routeCoordinates: {},
            capturedAt: "2026-08-09T08:00:00Z",
            fields: {},
            items: [
              { title: "Run readiness check", isCommitted: false },
              { title: "Review customer questions", isCommitted: true },
            ],
          },
        ]}
      />,
    );

    expect(screen.getByText("Run readiness check")).toBeInTheDocument();
    expect(screen.getByText("Review customer questions")).toBeInTheDocument();
    expect(screen.getByText("pm.cards.draft")).toBeInTheDocument();
    expect(screen.getByText("pm.cards.committed")).toBeInTheDocument();
  });
});
