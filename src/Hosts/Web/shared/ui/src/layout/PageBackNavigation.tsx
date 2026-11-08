"use client";

import React from "react";
import { Breadcrumbs, Button } from "@heroui/react";
import { FaIcon } from "../icons/FaIcon";

/** One ancestor of the current page. */
export interface PageCrumb {
  id: string;
  label: string;
  onPress: () => void;
}

export interface PageBackNavigationProps {
  /** Ancestors from the top of the hierarchy down to the page's parent. */
  crumbs: readonly PageCrumb[];
  /** The current page, shown last in the trail and not pressable. */
  currentLabel?: string;
  /** The accessible name of the trail, such as "Page location". */
  navigationLabel: string;
  /** The text of the single back control on narrow screens, such as "Back to Shifts". */
  backLabel: string;
}

/** The trail of ancestors above a page title; on narrow screens a single control back to the parent. */
export function PageBackNavigation({ crumbs, currentLabel, navigationLabel, backLabel }: PageBackNavigationProps) {
  const parent = crumbs.at(-1);
  if (!parent) return null;

  return (
    <>
      <nav aria-label={navigationLabel} className="hidden sm:block">
        <Breadcrumbs onAction={(key) => crumbs.find((crumb) => crumb.id === key)?.onPress()}>
          {crumbs.map((crumb) => (
            <Breadcrumbs.Item key={crumb.id} id={crumb.id}>
              {crumb.label}
            </Breadcrumbs.Item>
          ))}
          {currentLabel ? <Breadcrumbs.Item>{currentLabel}</Breadcrumbs.Item> : null}
        </Breadcrumbs>
      </nav>
      <Button className="self-start sm:hidden" size="sm" variant="ghost" onPress={parent.onPress}>
        <FaIcon icon="chevron-left" aria-hidden="true" className="rtl:rotate-180" />
        {backLabel}
      </Button>
    </>
  );
}
