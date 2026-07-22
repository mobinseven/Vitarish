import React, {forwardRef, type HTMLAttributes} from "react";

export type FaIconSize =
  | "2xs"
  | "xs"
  | "sm"
  | "lg"
  | "xl"
  | "2xl"
  | "1x"
  | "2x"
  | "3x"
  | "4x"
  | "5x"
  | "6x"
  | "7x"
  | "8x"
  | "9x"
  | "10x";

export type FaIconProps = HTMLAttributes<HTMLElement> & {
  icon: string;
  title?: string;
  size?: FaIconSize;
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

const sizeClasses = {
  "2xs": "text-[0.625rem]",
  xs: "text-[0.75rem]",
  sm: "text-[0.875rem]",
  "1x": "text-[1em]",
  lg: "text-[1.125rem]",
  xl: "text-[1.25rem]",
  "2xl": "text-[1.5rem]",
  "2x": "text-[2em]",
  "3x": "text-[3em]",
  "4x": "text-[4em]",
  "5x": "text-[5em]",
  "6x": "text-[6em]",
  "7x": "text-[7em]",
  "8x": "text-[8em]",
  "9x": "text-[9em]",
  "10x": "text-[10em]",
} satisfies Record<FaIconSize, string>;

export const FaIcon = forwardRef<HTMLElement, FaIconProps>(
  (
    {
      icon,
      className,
      size = "xs",
      spin,
      pulse,
      beat,
      bounce,
      fade,
      flip,
      rotation,
      fixedWidth,
      border,
      pull,
      swapOpacity,
      title,
      ...props
    },
    ref,
  ) => {
    const classes = [
      "fa-light",
      `fa-${icon}`,
      "inline-block leading-none",
      sizeClasses[size],
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
    ]
      .filter(Boolean)
      .join(" ");

    return <i ref={ref} className={classes} title={title} {...props} />;
  },
);

FaIcon.displayName = "FaIcon";
