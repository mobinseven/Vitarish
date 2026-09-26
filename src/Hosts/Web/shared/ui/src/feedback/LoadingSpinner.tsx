"use client";

import { useId } from "react";

export interface LoadingSpinnerProps {
  size?: number;
  className?: string;
  primaryColor?: string;
  secondaryColor?: string;
  tertiaryColor?: string;
}

export function LoadingSpinner({
  size = 128,
  className,
}: LoadingSpinnerProps) {
  const animationId = useId().replaceAll(":", "");
  const ids = Array.from({ length: 12 }, (_, index) => `${animationId}-${index}`);

  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      aria-hidden="true"
    >
      <style>{`@media (prefers-reduced-motion: reduce) { animate { display: none; } }`}</style>
      <rect width="10" height="10" x="1" y="1" fill="currentColor" rx="1">
        <animate id={ids[0]} fill="freeze" attributeName="x" begin={`0;${ids[11]}.end`} dur="0.2s" values="1;13" />
        <animate id={ids[1]} fill="freeze" attributeName="y" begin={`${ids[8]}.end`} dur="0.2s" values="1;13" />
        <animate id={ids[2]} fill="freeze" attributeName="x" begin={`${ids[9]}.end`} dur="0.2s" values="13;1" />
        <animate id={ids[3]} fill="freeze" attributeName="y" begin={`${ids[10]}.end`} dur="0.2s" values="13;1" />
      </rect>
      <rect width="10" height="10" x="1" y="13" fill="currentColor" rx="1">
        <animate id={ids[4]} fill="freeze" attributeName="y" begin={`${ids[0]}.end`} dur="0.2s" values="13;1" />
        <animate id={ids[5]} fill="freeze" attributeName="x" begin={`${ids[1]}.end`} dur="0.2s" values="1;13" />
        <animate id={ids[6]} fill="freeze" attributeName="y" begin={`${ids[2]}.end`} dur="0.2s" values="1;13" />
        <animate id={ids[7]} fill="freeze" attributeName="x" begin={`${ids[3]}.end`} dur="0.2s" values="13;1" />
      </rect>
      <rect width="10" height="10" x="13" y="13" fill="currentColor" rx="1">
        <animate id={ids[8]} fill="freeze" attributeName="x" begin={`${ids[4]}.end`} dur="0.2s" values="13;1" />
        <animate id={ids[9]} fill="freeze" attributeName="y" begin={`${ids[5]}.end`} dur="0.2s" values="13;1" />
        <animate id={ids[10]} fill="freeze" attributeName="x" begin={`${ids[6]}.end`} dur="0.2s" values="1;13" />
        <animate id={ids[11]} fill="freeze" attributeName="y" begin={`${ids[7]}.end`} dur="0.2s" values="1;13" />
      </rect>
    </svg>
  );
}
