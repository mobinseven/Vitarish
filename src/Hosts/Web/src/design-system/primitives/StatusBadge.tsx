interface StatusBadgeProps {
  children: string;
  tone?: "accent" | "success" | "muted";
}

export function StatusBadge({ children, tone = "accent" }: StatusBadgeProps) {
  return (
    <span className={`ds-status-badge ds-status-badge-${tone}`}>
      <span aria-hidden="true" className="ds-status-dot" />
      {children}
    </span>
  );
}
