"use client";

import { useLayoutEffect, useRef, useState, type ReactNode } from "react";

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

export interface AgentChatWorkspaceProps {
  /** The page header, usually a `PageHeader` with the conversation's actions. */
  header: ReactNode;
  /** Optional conversation list beside the thread on wide screens, such as an `AgentWorkspaceSidebar`. */
  history?: ReactNode;
  /** A notice above the thread, such as a continued-conversation note. */
  notice?: ReactNode;
  /** The scrolling thread. */
  children: ReactNode;
  /** The message composer, kept at the bottom. */
  composer: ReactNode;
}

/**
 * A full-height assistant page: header, optional conversation list, scrolling thread and a composer kept in view.
 * Inside a scrolling page it takes the viewport height left below where it starts, so only the thread scrolls.
 */
export function AgentChatWorkspace({ children, composer, header, history, notice }: AgentChatWorkspaceProps) {
  const root = useRef<HTMLDivElement>(null);
  const [blockSize, setBlockSize] = useState<string>();
  useLayoutEffect(() => {
    const element = root.current;
    if (!element) return;
    const measure = () => {
      const container = element.closest<HTMLElement>("[data-vitarish-layout='page-container']");
      const bottomGap = container ? Number.parseFloat(getComputedStyle(container).paddingBlockEnd) || 0 : 0;
      const scroller = element.closest<HTMLElement>("[data-vitarish-scroll-owner='workspace']");
      const top = element.getBoundingClientRect().top + (scroller?.scrollTop ?? 0);
      setBlockSize(`calc(100dvh - ${Math.max(0, top + bottomGap)}px)`);
    };
    measure();
    window.addEventListener("resize", measure);
    return () => window.removeEventListener("resize", measure);
  }, []);
  return (
    <div
      ref={root}
      className={`flex min-h-0 flex-col gap-4 ${blockSize ? "flex-none" : "flex-1"}`}
      data-vitarish-layout="agent-chat-workspace"
      style={blockSize ? { blockSize, minBlockSize: "24rem" } : undefined}
    >
      {header}
      <div className="flex min-h-0 flex-1 gap-4">
        {history ? <div className="hidden w-72 shrink-0 lg:flex">{history}</div> : null}
        <div className="flex min-h-0 min-w-0 flex-1 flex-col">
          {notice ? <div className="mx-auto w-full max-w-3xl px-4">{notice}</div> : null}
          <div className="flex min-h-0 flex-1 flex-col">{children}</div>
          <div className="mx-auto w-full max-w-3xl shrink-0 px-4 pt-3">{composer}</div>
        </div>
      </div>
    </div>
  );
}
