import type { ReactNode } from "react";

export interface AgentWorkspaceSidebarProps {
  actions?: ReactNode;
  children: ReactNode;
  description?: ReactNode;
  title: ReactNode;
}

/** Presentation slots for recent agent tasks inside a shared dashboard sidebar. */
export function AgentWorkspaceSidebar({
  actions,
  children,
  description,
  title,
}: AgentWorkspaceSidebarProps) {
  return (
    <aside
      aria-label={typeof title === "string" ? title : undefined}
      className="flex h-full min-w-0 flex-col bg-transparent px-1 py-2"
      data-vitarish-layout="agent-workspace-sidebar"
    >
      <div className="flex items-center justify-between gap-2 px-2 py-2">
        <div className="min-w-0">
          <h2 className="truncate text-sm font-semibold tracking-tight">
            {title}
          </h2>
          {description ? (
            <p className="mt-0.5 truncate text-[11px] leading-4 text-muted">
              {description}
            </p>
          ) : null}
        </div>
        {actions ? <div className="shrink-0">{actions}</div> : null}
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto">{children}</div>
    </aside>
  );
}
