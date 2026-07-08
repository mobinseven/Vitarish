import {forwardRef} from "react";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {
  faArrowRight,
  faArrowUpRightFromSquare,
  faBoxesStacked,
  faCircleCheck,
  faCircleInfo,
  faCopy,
  faDisplay,
  faMoon,
  faHeartPulse,
  faRocket,
  faShieldHalved,
  faSun,
} from "@fortawesome/free-solid-svg-icons";
import type {FontAwesomeIconProps} from "@fortawesome/react-fontawesome";

const iconMap = {
  "arrow-right": faArrowRight,
  "arrow-up-right": faArrowUpRightFromSquare,
  "boxes-3": faBoxesStacked,
  "circle-check": faCircleCheck,
  "circle-info": faCircleInfo,
  copy: faCopy,
  display: faDisplay,
  moon: faMoon,
  pulse: faHeartPulse,
  rocket: faRocket,
  "shield-check": faShieldHalved,
  sun: faSun,
} as const;

type IconName = keyof typeof iconMap;

export type FaIconProps = Omit<FontAwesomeIconProps, "icon"> & {
  icon: IconName | string;
};

export const FaIcon = forwardRef<SVGSVGElement, FaIconProps>(
  ({icon: iconProp, ...props}, ref) => {
    const icon = iconMap[iconProp as IconName] ?? faCircleInfo;

    return <FontAwesomeIcon ref={ref} icon={icon} {...props} />;
  },
);

FaIcon.displayName = "FaIcon";
