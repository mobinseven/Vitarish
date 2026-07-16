import {forwardRef, type HTMLAttributes} from "react";

const iconMap = {
  "arrow-right": "arrow-right",
  "arrow-up-right": "arrow-up-right-from-square",
  "boxes-3": "boxes-stacked",
  "circle-check": "circle-check",
  "circle-info": "circle-info",
  copy: "copy",
  display: "display",
  moon: "moon",
  pulse: "heart-pulse",
  rocket: "rocket",
  "shield-check": "shield-halved",
  sun: "sun",
} as const;

type IconName = keyof typeof iconMap;

export type FaIconProps = HTMLAttributes<HTMLElement> & {
  icon: IconName | string;
  title?: string;
  size?: "2xs" | "xs" | "sm" | "lg" | "xl" | "2xl" | "1x" | "2x" | "3x" | "4x" | "5x" | "6x" | "7x" | "8x" | "9x" | "10x";
  spin?: boolean;
  pulse?: boolean;
  beat?: boolean;
  bounce?: boolean;
  fade?: boolean;
  flip?: "horizontal" | "vertical" | "both";
  rotation?: 90 | 180 | 270;
  fixedWidth?: boolean;
  border?: boolean;
  pull?: "left" | "right";
  swapOpacity?: boolean;
};

export const FaIcon = forwardRef<HTMLElement, FaIconProps>(
  ({icon: iconProp, className, size, spin, pulse, beat, bounce, fade, flip, rotation, fixedWidth, border, pull, swapOpacity, title, ...props}, ref) => {
    const icon = iconMap[iconProp as IconName] ?? "circle-info";
    const classes = [
      "fa-solid",
      `fa-${icon}`,
      size && `fa-${size}`,
      spin && "fa-spin",
      pulse && "fa-pulse",
      beat && "fa-beat",
      bounce && "fa-bounce",
      fade && "fa-fade",
      flip && `fa-flip-${flip}`,
      rotation && `fa-rotate-${rotation}`,
      fixedWidth && "fa-fw",
      border && "fa-border",
      pull && `fa-pull-${pull}`,
      swapOpacity && "fa-swap-opacity",
      className,
    ].filter(Boolean).join(" ");

    return <i ref={ref} className={classes} title={title} {...props} />;
  },
);

FaIcon.displayName = "FaIcon";
