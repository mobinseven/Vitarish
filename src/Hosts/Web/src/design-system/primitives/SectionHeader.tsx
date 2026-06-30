import type { ComponentPropsWithoutRef, ReactNode } from "react";
import { classNames } from "./classNames";

interface SectionHeaderProps extends ComponentPropsWithoutRef<"div"> {
  eyebrow?: string;
  title: string;
  description?: ReactNode;
  id?: string;
}

export function SectionHeader({
  eyebrow,
  title,
  description,
  id,
  className,
  ...props
}: SectionHeaderProps) {
  return (
    <div className={classNames("ds-section-header", className)} {...props}>
      <div>
        {eyebrow ? <p className="ds-eyebrow">{eyebrow}</p> : null}
        <h2 id={id} className="ds-section-title">
          {title}
        </h2>
      </div>
      {description ? (
        <p className="ds-section-description">{description}</p>
      ) : null}
    </div>
  );
}
