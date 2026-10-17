"use client";

import type { ReactNode } from "react";
import { Card } from "@heroui/react";
import { HeaderIcon } from "@vitarish/ui";

interface SurgeryPageHeaderProps {
  title: ReactNode;
  /** A line of text, or richer content such as the patient and status, under the title. */
  details?: ReactNode;
  actions?: ReactNode;
}

/** The page header of every surgery page: a band in the host anchor, as the patient profile's header is. */
export function SurgeryPageHeader({ title, details, actions }: SurgeryPageHeaderProps) {
  return (
    <Card className="rounded-3xl bg-anchor">
      <header className="flex flex-col items-start gap-5 px-4 py-6 sm:flex-row sm:items-center sm:justify-between sm:px-6">
        <div className="flex min-w-0 items-center gap-4">
          <HeaderIcon icon="scissors" level="page" tone="inverse" />
          <div className="min-w-0">
            <h1 className="text-2xl font-bold text-anchor-foreground">{title}</h1>
            {typeof details === "string" ? (
              <p className="text-sm text-anchor-foreground/75">{details}</p>
            ) : details}
          </div>
        </div>
        {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
      </header>
    </Card>
  );
}
