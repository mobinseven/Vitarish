"use client";

import type { ComponentPropsWithoutRef } from "react";
import { Link } from "@heroui/react";

export type SettingsSurfaceWidth = "form" | "wide";

export interface SettingsSurfaceProps extends ComponentPropsWithoutRef<"section"> {
  width?: SettingsSurfaceWidth;
}

export interface SettingsSectionProps extends ComponentPropsWithoutRef<"section"> {
  title: string;
}

export interface SettingsBackLinkProps extends Omit<ComponentPropsWithoutRef<typeof Link>, "children"> {
  label: string;
}

const surfaceWidthClassNames: Record<SettingsSurfaceWidth, string> = {
  form: "max-w-xl",
  wide: "max-w-4xl",
};

export function SettingsSurface({ children, className = "", width = "form", ...props }: SettingsSurfaceProps) {
  return (
    <section
      className={`mx-auto flex w-full min-w-0 flex-col gap-4 ${surfaceWidthClassNames[width]} ${className}`.trim()}
      {...props}
    >
      {children}
    </section>
  );
}

export function SettingsSection({ children, className = "", title, ...props }: SettingsSectionProps) {
  return (
    <section className={`flex w-full min-w-0 flex-col gap-4 ${className}`.trim()} {...props}>
      <h1 className="whitespace-normal break-words text-2xl font-bold">{title}</h1>
      {children}
    </section>
  );
}

export function SettingsBackLink({ className = "", label, ...props }: SettingsBackLinkProps) {
  return (
    <Link className={`w-fit max-w-full text-sm whitespace-normal break-words ${className}`.trim()} {...props}>
      {label}
    </Link>
  );
}
