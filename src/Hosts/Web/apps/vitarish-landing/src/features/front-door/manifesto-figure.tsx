import type { CSSProperties, ReactNode } from "react";
import type { ManifestoFigure as FigureKind } from "./manifesto-scenes";
import styles from "./manifesto.module.css";

/*
 * Decorative artwork for the manifesto. Every element is drawn in its resting place and animated in
 * CSS from the scene's `--on` value (0 hidden, 1 shown), so the same markup is the static figure
 * when motion is reduced. Positions come from a seeded generator so server and client agree.
 */

interface Dot { x: number; y: number; r: number }

const viewBox = "-200 -200 400 400";

function seeded(seed: number) {
  let state = seed;
  return () => {
    state = (state + 0x6d2b79f5) | 0;
    let value = Math.imul(state ^ (state >>> 15), 1 | state);
    value ^= value + Math.imul(value ^ (value >>> 7), 61 | value);
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
  };
}

function regular(sides: number, radius: number, rotation = 0) {
  return Array.from({ length: sides }, (_, index) => {
    const angle = ((index / sides) * 360 + rotation - 90) * (Math.PI / 180);
    return `${(Math.cos(angle) * radius).toFixed(1)},${(Math.sin(angle) * radius).toFixed(1)}`;
  }).join(" ");
}

function vertices(sides: number, radius: number, rotation = 0): Dot[] {
  return Array.from({ length: sides }, (_, index) => {
    const angle = ((index / sides) * 360 + rotation - 90) * (Math.PI / 180);
    return { x: Math.cos(angle) * radius, y: Math.sin(angle) * radius, r: 3 };
  });
}

/** Dots in a ring around the line, keeping a clear band for the text. */
function scattered(seed: number, count: number, minRadius: number, maxRadius: number, minSize: number, maxSize: number, clearBand = 34): Dot[] {
  const next = seeded(seed);
  const dots: Dot[] = [];
  while (dots.length < count) {
    const angle = next() * Math.PI * 2;
    const distance = minRadius + next() * (maxRadius - minRadius);
    const y = Math.sin(angle) * distance * 0.8;
    if (Math.abs(y) < clearBand) continue;
    dots.push({ x: Math.cos(angle) * distance, y, r: minSize + next() * (maxSize - minSize) });
  }
  return dots;
}

/** A soft mound of dots resting above the line. */
function heaped(seed: number, count: number): Dot[] {
  const next = seeded(seed);
  return Array.from({ length: count }, () => {
    const x = (next() * 2 - 1) * 92;
    const height = 46 * (1 - (x / 96) ** 2);
    return { x, y: 34 - next() * height, r: 3 + next() * 12 * (1 - Math.abs(x) / 120) };
  });
}

const figures = {
  octagon: regular(8, 172, 22.5),
  square: regular(4, 150, 45),
  hexagon: regular(6, 150, 30),
  corners: vertices(8, 172, 22.5),
  hexCorners: vertices(6, 150, 30),
  scatter: scattered(11, 9, 70, 185, 3, 9),
  heap: heaped(23, 34),
  bokehSmall: scattered(31, 60, 40, 380, 1.2, 3),
  bokehLarge: scattered(37, 10, 110, 380, 10, 30),
  sparse: scattered(41, 8, 40, 120, 1, 1.8),
  cloud: scattered(53, 64, 0, 190, 1.5, 7, 0).map((dot) => ({ ...dot, y: dot.y * 0.7 - 50 })),
  cloudBlur: scattered(59, 6, 120, 200, 10, 18, 0).map((dot) => ({ ...dot, y: dot.y * 0.7 - 50 })),
};

type Vars = CSSProperties & Record<`--${string}`, string | number>;

function at(dot: Dot, index: number, extra: Vars = {}): Vars {
  return { "--x": `${dot.x.toFixed(1)}px`, "--y": `${dot.y.toFixed(1)}px`, "--i": index, ...extra };
}

/** Ambient drift for one element; seeded so it is stable across renders. */
function drift(index: number, reach: number): Vars {
  const next = seeded(index * 97 + 5);
  return {
    "--dx": `${((next() * 2 - 1) * reach).toFixed(1)}px`,
    "--dy": `${((next() * 2 - 1) * reach).toFixed(1)}px`,
    "--drift": `${(6 + next() * 6).toFixed(1)}s`,
  };
}

function Dots({ dots, className, from, reach = 0 }: { dots: Dot[]; className?: string; from?: Dot; reach?: number }) {
  return dots.map((dot, index) => (
    <circle
      className={`${styles.dot} ${reach ? styles.ambient : ""} ${className ?? ""}`}
      key={index}
      r={dot.r.toFixed(1)}
      style={at(dot, index, {
        ...(from ? { "--x0": `${from.x}px`, "--y0": `${from.y}px` } : {}),
        ...(reach ? drift(index, reach) : {}),
      })}
    />
  ));
}

function Shape({ points, index, turn, length = 1, faint = false }: { points: string; index: number; turn: number; length?: number; faint?: boolean }) {
  return (
    <polygon
      className={`${styles.shape} ${faint ? styles.faint : ""}`}
      pathLength={1}
      points={points}
      style={{ "--i": index, "--turn": turn, "--length": length } as Vars}
    />
  );
}

function Polygons({ turned }: { turned: boolean }) {
  return (
    <>
      <Shape faint index={0} points={figures.octagon} turn={turned ? 22.5 : -30} />
      <Shape index={1} points={figures.square} turn={turned ? -45 : 40} />
      <Shape index={2} points={figures.hexagon} turn={turned ? 30 : -50} />
      <circle
        className={styles.shape}
        pathLength={1}
        r={184}
        style={{ "--i": 3, "--turn": turned ? 120 : 60, "--length": turned ? 0.45 : 0.3 } as Vars}
      />
      <Dots dots={turned ? figures.hexCorners : figures.corners} />
    </>
  );
}

function Converge() {
  const point = { x: 0, y: 50, r: 7 };
  return (
    <>
      {figures.cloud.map((dot, index) => (
        <line
          className={`${styles.stroke} ${styles.faint}`}
          key={index}
          pathLength={1}
          style={{ "--i": index } as Vars}
          x1={dot.x.toFixed(1)}
          x2={point.x}
          y1={dot.y.toFixed(1)}
          y2={point.y}
        />
      ))}
      <Dots dots={figures.cloud} />
      <circle className={`${styles.dot} ${styles.accentFill}`} r={point.r} style={at(point, 0)} />
    </>
  );
}

const figureArt: Record<Exclude<FigureKind, "none">, () => ReactNode> = {
  polygons: () => <Polygons turned={false} />,
  polygonsTurned: () => <Polygons turned />,
  scatter: () => <Dots dots={figures.scatter} reach={10} />,
  heap: () => <Dots dots={figures.heap} from={{ x: 0, y: -200, r: 0 }} />,
  bokeh: () => (
    <>
      <Dots className={styles.blur} dots={figures.bokehLarge} reach={18} />
      <Dots dots={figures.bokehSmall} reach={12} />
    </>
  ),
  dot: () => <Dots dots={[{ x: 0, y: 36, r: 7 }]} />,
  pair: () => <Dots dots={[{ x: -11, y: 30, r: 16 }, { x: 11, y: 30, r: 16 }]} />,
  link: () => (
    <>
      <line className={styles.stroke} pathLength={1} style={{ "--i": 1 } as Vars} x1={-12} x2={8} y1={-26} y2={34} />
      <Dots dots={[{ x: -12, y: -26, r: 5 }, { x: 8, y: 34, r: 10 }]} />
    </>
  ),
  sparse: () => <Dots dots={figures.sparse} reach={14} />,
  cloud: () => (
    <>
      <Dots className={styles.blur} dots={figures.cloudBlur} reach={16} />
      <Dots dots={figures.cloud} reach={8} />
    </>
  ),
  converge: () => <Converge />,
  rings: () => (
    <>
      <circle className={`${styles.shape} ${styles.ring}`} cx={-48} pathLength={1} r={11} style={{ "--i": 0 } as Vars} />
      <circle className={`${styles.shape} ${styles.ring}`} cx={48} pathLength={1} r={11} style={{ "--i": 4 } as Vars} />
      <circle className={`${styles.dot} ${styles.accentFill}`} r={4.5} style={at({ x: 48, y: 0, r: 0 }, 8)} />
    </>
  ),
  disc: () => <circle className={`${styles.disc} ${styles.accentFill}`} r={178} />,
};

export function ManifestoFigure({ figure }: { figure: FigureKind }) {
  if (figure === "none") return null;
  return (
    <svg className={styles.figure} focusable="false" viewBox={viewBox}>
      {figureArt[figure]()}
    </svg>
  );
}
