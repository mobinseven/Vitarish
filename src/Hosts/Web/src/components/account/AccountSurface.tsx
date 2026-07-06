"use client";

import type { ComponentPropsWithoutRef, ReactNode } from "react";
import { Link } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

type AccountSurfaceWidth = "form" | "wide";

interface AccountSurfaceProps extends ComponentPropsWithoutRef<"section"> {
  width?: AccountSurfaceWidth;
}

interface AccountWidgetProps extends ComponentPropsWithoutRef<typeof Widget> {
  title: string;
}

interface AccountBackLinkProps extends ComponentPropsWithoutRef<typeof Link> {
  href?: string;
}

const surfaceWidthClassNames: Record<AccountSurfaceWidth, string> = {
  form: "max-w-xl",
  wide: "max-w-4xl",
};

export function AccountSurface({ children, width = "form", className = "", ...props }: AccountSurfaceProps) {
  return (
    <section
      className={`mx-auto flex w-full min-w-0 flex-col gap-4 ${surfaceWidthClassNames[width]} ${className}`}
      {...props}
    >
      {children}
    </section>
  );
}

export function AccountWidget({ title, children, className = "", ...props }: AccountWidgetProps) {
  return (
    <Widget className={`w-full min-w-0 ${className}`} {...props}>
      <Widget.Header>
        <Widget.Title className="whitespace-normal break-words">{title}</Widget.Title>
      </Widget.Header>
      <Widget.Content className="flex min-w-0 flex-col gap-4">{children}</Widget.Content>
    </Widget>
  );
}

export function AccountBackLink({ href = Routes.Account.Index, className = "", ...props }: AccountBackLinkProps) {
  const { t } = useLocalizedText();

  return (
    <Link
      href={href}
      className={`w-fit max-w-full text-sm whitespace-normal break-words ${className}`}
      {...props}
    >
      {t("common.actions.backToSettings")}
    </Link>
  );
}
