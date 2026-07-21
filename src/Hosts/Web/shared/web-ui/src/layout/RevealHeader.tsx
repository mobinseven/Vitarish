"use client";

import React, {
  forwardRef,
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type HTMLAttributes,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";

type RevealHeaderPositioning = "overlay" | "inset";

export interface RevealHeaderClassNames {
  base?: string;
  content?: string;
}

export interface RevealHeaderProps extends Omit<HTMLAttributes<HTMLDivElement>, "children"> {
  compactContent: ReactNode;
  positioning?: RevealHeaderPositioning;
  scrollRoot?: HTMLElement | null;
  classNames?: RevealHeaderClassNames;
}

const boundaryEventName = "vitarish-reveal-header-boundary";

export const RevealHeaderBoundary = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(
  function RevealHeaderBoundary({ className, ...props }, forwardedRef) {
    const setRef = useCallback(
      (element: HTMLDivElement | null) => {
        document.dispatchEvent(new CustomEvent(boundaryEventName, { detail: element }));

        if (typeof forwardedRef === "function") {
          forwardedRef(element);
        } else if (forwardedRef) {
          forwardedRef.current = element;
        }
      },
      [forwardedRef]
    );

    useEffect(
      () => () => {
        document.dispatchEvent(new CustomEvent(boundaryEventName, { detail: null }));
      },
      []
    );

    return (
      <div
        {...props}
        ref={setRef}
        aria-hidden="true"
        className={mergeClassNames("vitarish-reveal-header-boundary", className)}
        data-vitarish-reveal-header-boundary=""
      />
    );
  }
);

export const RevealHeader = Object.assign(
  forwardRef<HTMLDivElement, RevealHeaderProps>(function RevealHeader(
    {
      compactContent,
      positioning = "overlay",
      scrollRoot,
      className,
      classNames,
      ...props
    },
    ref
  ) {
    const [boundary, setBoundary] = useState<HTMLElement | null>(null);
    const [isRevealed, setIsRevealed] = useState(false);
    const [headerHeight, setHeaderHeight] = useState(0);
    const headerRef = useRef<HTMLDivElement | null>(null);
    const resolvedScrollRoot = useMemo(
      () => scrollRoot ?? getScrollRoot(boundary),
      [boundary, scrollRoot]
    );
    const revealHost = useMemo(() => getRevealHeaderHost(boundary), [boundary]);

    const updateRevealedState = useCallback(() => {
      if (!boundary) {
        setIsRevealed(false);
        return;
      }

      const boundaryTop = boundary.getBoundingClientRect().top;
      const activationLine = resolvedScrollRoot?.getBoundingClientRect().top ?? 0;
      setIsRevealed(boundaryTop <= activationLine);
    }, [boundary, resolvedScrollRoot]);

    useEffect(() => {
      const onBoundaryChange = (event: Event) =>
        setBoundary((event as CustomEvent<HTMLElement | null>).detail);
      const existingBoundary = document.querySelector<HTMLElement>(
        "[data-vitarish-reveal-header-boundary]"
      );

      setBoundary(existingBoundary);
      document.addEventListener(boundaryEventName, onBoundaryChange);
      return () => document.removeEventListener(boundaryEventName, onBoundaryChange);
    }, []);

    useEffect(() => {
      updateRevealedState();
      const scrollTarget: EventTarget = resolvedScrollRoot ?? window;
      scrollTarget.addEventListener("scroll", updateRevealedState, { passive: true });

      if (!boundary || typeof IntersectionObserver === "undefined") {
        return () => scrollTarget.removeEventListener("scroll", updateRevealedState);
      }

      const observer = new IntersectionObserver(updateRevealedState, {
        root: resolvedScrollRoot,
        threshold: 0,
      });
      observer.observe(boundary);
      return () => {
        observer.disconnect();
        scrollTarget.removeEventListener("scroll", updateRevealedState);
      };
    }, [boundary, resolvedScrollRoot, updateRevealedState]);

    useLayoutEffect(() => {
      const header = headerRef.current;
      if (!header) {
        return;
      }

      const updateHeight = () => setHeaderHeight(header.getBoundingClientRect().height);
      updateHeight();

      if (typeof ResizeObserver === "undefined") {
        return;
      }

      const observer = new ResizeObserver(updateHeight);
      observer.observe(header);
      return () => observer.disconnect();
    }, [compactContent]);

    useEffect(() => {
      if (!isRevealed) {
        return;
      }

      const scrollContainer = resolvedScrollRoot ?? document.documentElement;
      const previousScrollPadding = scrollContainer.style.scrollPaddingBlockStart;
      scrollContainer.style.scrollPaddingBlockStart = `${headerHeight}px`;
      return () => {
        scrollContainer.style.scrollPaddingBlockStart = previousScrollPadding;
      };
    }, [headerHeight, isRevealed, resolvedScrollRoot]);

    const header = (
      <div
        {...props}
        ref={(element) => {
          headerRef.current = element;
          if (typeof ref === "function") ref(element);
          else if (ref) ref.current = element;
        }}
        aria-hidden={!isRevealed}
        className={mergeClassNames("vitarish-reveal-header", className, classNames?.base)}
        data-state={isRevealed ? "revealed" : "hidden"}
        data-positioning={positioning}
        inert={!isRevealed}
      >
        <div className={classNames?.content}>{compactContent}</div>
      </div>
    );

    return (
      <>
        {positioning === "inset" ? (
          <div
            aria-hidden="true"
            className="vitarish-reveal-header-inset"
            style={{ blockSize: headerHeight }}
          />
        ) : null}
        {revealHost ? createPortal(header, revealHost) : header}
      </>
    );
  }),
  { Boundary: RevealHeaderBoundary }
);

export type { RevealHeaderPositioning };

function mergeClassNames(...classNames: Array<string | undefined>) {
  const result = classNames.filter(Boolean).join(" ");
  return result || undefined;
}

function getRevealHeaderHost(boundary: HTMLElement | null): HTMLElement | null {
  const region = boundary?.closest<HTMLElement>("[data-vitarish-reveal-header-region]");
  return region?.querySelector<HTMLElement>("[data-vitarish-reveal-header-host]") ?? null;
}

function getScrollRoot(boundary: HTMLElement | null): HTMLElement | null {
  const workspaceRoot = boundary?.closest<HTMLElement>(
    '[data-vitarish-reveal-header-region="workspace"][data-vitarish-scroll-owner="workspace"]'
  );
  if (workspaceRoot) {
    return workspaceRoot;
  }

  const dashboardFrame = boundary?.closest<HTMLElement>('[data-vitarish-layout="dashboard-frame"]');
  return dashboardFrame?.querySelector<HTMLElement>(".app-layout__main")
    ?? boundary?.closest<HTMLElement>("[data-vitarish-scroll-owner]")
    ?? null;
}
