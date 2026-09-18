"use client";

import {
  forwardRef,
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type HTMLAttributes,
} from "react";

type ViewportRevealElement = "article" | "div" | "li" | "section";

export interface ViewportRevealProps extends HTMLAttributes<HTMLElement> {
  /** The semantic element used for the reveal region. */
  as?: ViewportRevealElement;
  /** Whether the region remains revealed after it has entered the viewport. */
  once?: boolean;
  /** Optional delay for coordinated section or item reveals. */
  delayMs?: number;
}

const defaultDelayMs = 0;

/**
 * Reveals caller-owned content when it enters the viewport.
 *
 * The shared visual contract is opacity 0 -> 1 and translateY(24px) -> 0.
 * Content remains visible when scripting or IntersectionObserver is unavailable.
 */
export const ViewportReveal = forwardRef<HTMLElement, ViewportRevealProps>(
  function ViewportReveal(
    {
      as = "div",
      children,
      className,
      delayMs = defaultDelayMs,
      once = true,
      style,
      ...props
    },
    forwardedRef,
  ) {
    const elementRef = useRef<HTMLElement | null>(null);
    const [isEnhanced, setIsEnhanced] = useState(false);
    const [isRevealed, setIsRevealed] = useState(false);

    useEffect(() => {
      const element = elementRef.current;
      if (!element) {
        return;
      }

      if (typeof IntersectionObserver === "undefined") {
        setIsRevealed(true);
        return;
      }

      setIsEnhanced(true);
      const observer = new IntersectionObserver(
        ([entry]) => {
          if (entry?.isIntersecting) {
            setIsRevealed(true);
            if (once) {
              observer.disconnect();
            }
          } else if (!once) {
            setIsRevealed(false);
          }
        },
        // An inset entry line lets the fade read inside the viewport, including
        // sections taller than the screen (no height-dependent threshold).
        { threshold: 0, rootMargin: "0px 0px -12% 0px" },
      );

      observer.observe(element);
      return () => observer.disconnect();
    }, [once]);

    const setRefs = (element: HTMLElement | null) => {
      elementRef.current = element;
      if (typeof forwardedRef === "function") {
        forwardedRef(element);
      } else if (forwardedRef) {
        forwardedRef.current = element;
      }
    };

    const revealStyle = {
      ...style,
      "--vitarish-viewport-reveal-delay": `${Math.max(0, delayMs)}ms`,
    } as CSSProperties;
    const Component = as;

    return (
      <Component
        {...props}
        ref={setRefs}
        className={[
          "vitarish-viewport-reveal",
          className,
        ].filter(Boolean).join(" ")}
        data-state={isRevealed ? "revealed" : "hidden"}
        data-vitarish-viewport-reveal=""
        data-vitarish-viewport-reveal-enhanced={isEnhanced ? "" : undefined}
        style={revealStyle}
      >
        {children}
      </Component>
    );
  },
);
