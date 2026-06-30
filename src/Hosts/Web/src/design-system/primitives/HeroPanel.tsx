import type { ComponentPropsWithoutRef, ReactNode } from "react";
import { classNames } from "./classNames";

interface HeroPanelProps extends ComponentPropsWithoutRef<"section"> {
  children: ReactNode;
}

export function HeroPanel({ children, className, ...props }: HeroPanelProps) {
  return (
    <section className={classNames("ds-hero-panel", className)} {...props}>
      <div aria-hidden="true" className="ds-hero-orb ds-hero-orb-primary" />
      <div aria-hidden="true" className="ds-hero-orb ds-hero-orb-secondary" />
      <div className="ds-hero-content">{children}</div>
    </section>
  );
}
