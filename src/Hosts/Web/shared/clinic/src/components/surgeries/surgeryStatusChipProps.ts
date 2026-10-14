import { chromaChipClassName } from "@vitarish/ui/chroma";
import {
  surgeryStatuses,
  type SurgeryStatus,
} from "../../models/surgeries/surgeryModels";

export type SurgeryStatusChipProps = {
  color?: "accent" | "danger" | "success" | "warning";
  variant?: "soft";
  className?: string;
};

const neutral: SurgeryStatusChipProps = {};

/**
 * A surgery status shown together with others keeps its own color, so planned
 * and completed never look alike; a cancelled surgery needs no attention and
 * stays neutral (docs/hosts/web/color-roles.md). Every chip is a soft chip.
 */
const surgeryStatusChips: Record<SurgeryStatus, SurgeryStatusChipProps> = {
  [surgeryStatuses.planned]: { color: "success", variant: "soft" },
  [surgeryStatuses.completed]: { className: chromaChipClassName("primary") },
  [surgeryStatuses.cancelled]: neutral,
};

/** Props that color a HeroUI Chip for a surgery status. */
export function surgeryStatusChipProps(
  status: SurgeryStatus,
): SurgeryStatusChipProps {
  return surgeryStatusChips[status] ?? neutral;
}
