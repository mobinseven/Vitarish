"use client";

import React, { type ReactNode } from "react";
import { HeaderIcon } from "../icons/HeaderIcon";
import { PageBackNavigation, type PageBackNavigationProps } from "./PageBackNavigation";

export interface PageHeaderProps {
  /** Font Awesome icon name of the page, such as `clock`. */
  icon: string;
  title: ReactNode;
  titleId?: string;
  /** A line of text, or richer content, under the title. */
  details?: ReactNode;
  actions?: ReactNode;
  /** Where the page sits in the hierarchy; omit on a top-level page. */
  back?: PageBackNavigationProps;
  /** Navigation between sibling pages, shown under the header. */
  tabs?: ReactNode;
}

/** The header of a list, form or settings page: back trail, icon, title, details and actions. */
export function PageHeader({ icon, title, titleId, details, actions, back, tabs }: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-3">
      {back ? <PageBackNavigation {...back} /> : null}
      <header className="flex flex-col items-start gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex min-w-0 items-center gap-4">
          <HeaderIcon icon={icon} level="page" />
          <div className="min-w-0">
            <h1 id={titleId} className="text-2xl font-bold">
              {title}
            </h1>
            {typeof details === "string" ? <p className="text-sm text-muted">{details}</p> : details}
          </div>
        </div>
        {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
      </header>
      {tabs}
    </div>
  );
}
