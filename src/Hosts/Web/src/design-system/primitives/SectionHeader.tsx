import type { ReactNode } from "react";

interface SectionHeaderProps {
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
}: SectionHeaderProps) {
  return (
    <div className="ds-section-header">
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
