import type {IconProps} from "@iconify/react";

import {addCollection, Icon} from "@iconify/react";
import {forwardRef} from "react";

import gravityIcons from "./gravity-ui/icons.json";

addCollection(gravityIcons as Parameters<typeof addCollection>[0]);

export const gravityIconNames = Object.keys((gravityIcons as {icons: Record<string, unknown>}).icons).sort();

export type IconifyProps = Omit<IconProps, "icon"> & {
  icon: IconProps["icon"] | string;
  isGravityIcon?: boolean;
};

export const Iconify = forwardRef<SVGSVGElement, IconifyProps>(
  ({icon: iconProp, isGravityIcon, ...props}, ref) => {
    const shouldUseGravityIcon =
      typeof iconProp === "string" && (isGravityIcon ?? !iconProp.includes(":"));
    const icon = shouldUseGravityIcon ? "gravity-ui:" + iconProp : iconProp;

    return <Icon {...props} ref={ref} icon={icon} />;
  },
);

Iconify.displayName = "HeroUIBuilder.Iconify";
