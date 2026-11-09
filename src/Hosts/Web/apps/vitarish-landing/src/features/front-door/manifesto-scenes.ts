import type { enMessages } from "../../localization/messages/en";

/** A scene key names its line in the `landing.manifesto.scenes` catalog. */
export type ManifestoSceneKey = keyof typeof enMessages.landing.manifesto.scenes;

/** Light scenes use the page ground; dark scenes invert it, so the rhythm holds in both themes. */
export type ManifestoGround = "light" | "dark";

export type ManifestoFigure =
  | "polygons"
  | "polygonsTurned"
  | "scatter"
  | "heap"
  | "bokeh"
  | "dot"
  | "pair"
  | "link"
  | "sparse"
  | "cloud"
  | "converge"
  | "rings"
  | "disc"
  | "none";

/** Where the line sits against its figure. */
export type ManifestoTextPlacement = "center" | "above" | "below";

/** How a scene's marked words stand out from the lead. */
export type ManifestoMark = "disc" | "display";

export interface ManifestoScene {
  key: ManifestoSceneKey;
  ground: ManifestoGround;
  figure: ManifestoFigure;
  placement: ManifestoTextPlacement;
  mark?: ManifestoMark;
}

/** The order and staging of the brand manifesto; the catalogs own the words. */
export const manifestoScenes: readonly ManifestoScene[] = [
  { key: "everyDay", ground: "light", figure: "polygons", placement: "center" },
  { key: "whatMatters", ground: "light", figure: "polygonsTurned", placement: "center" },
  { key: "confuse", ground: "light", figure: "scatter", placement: "center" },
  { key: "noise", ground: "light", figure: "heap", placement: "below" },
  { key: "focus", ground: "dark", figure: "bokeh", placement: "center", mark: "disc" },
  { key: "firstQuestion", ground: "dark", figure: "none", placement: "center" },
  { key: "matters", ground: "dark", figure: "none", placement: "center", mark: "display" },
  { key: "time", ground: "light", figure: "dot", placement: "below" },
  { key: "care", ground: "light", figure: "pair", placement: "below" },
  { key: "clarity", ground: "light", figure: "link", placement: "below" },
  { key: "craft", ground: "light", figure: "none", placement: "center" },
  { key: "takesTime", ground: "light", figure: "sparse", placement: "center" },
  { key: "thousandNo", ground: "light", figure: "cloud", placement: "below" },
  { key: "everyYes", ground: "light", figure: "converge", placement: "below" },
  { key: "startOver", ground: "dark", figure: "none", placement: "center" },
  { key: "untilEvery", ground: "dark", figure: "rings", placement: "above" },
  { key: "makesRoom", ground: "dark", figure: "disc", placement: "center" },
  { key: "sign", ground: "light", figure: "none", placement: "center" },
];
