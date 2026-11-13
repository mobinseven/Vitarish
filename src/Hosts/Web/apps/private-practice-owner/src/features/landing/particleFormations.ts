/**
 * The shapes the landing page's particle field settles into. Each formation is a fixed set of
 * points in its own coordinates (x right, y up, origin at its centre) plus the half extents that
 * bound it, so the scene can fit it into any slot. Every formation holds the same number of
 * points, which lets one particle travel from any shape to any other.
 */
export const particleFormationIds = ["scatter", "logotype", "calendar", "sparkle", "bars", "sunrise"] as const;
export type ParticleFormationId = (typeof particleFormationIds)[number];

/** Floats per point: x, y, depth, and tone (0 is the base colour, 1 the accent). */
export const particleStride = 4;

export interface ParticleFormation {
  points: Float32Array;
  halfWidth: number;
  halfHeight: number;
}

type Random = () => number;
type Point = readonly [x: number, y: number, depth: number, tone: number];

/** Deterministic, so a formation is identical on every visit and in every test. */
function seededRandom(seed: number): Random {
  let state = seed >>> 0;
  return () => {
    state = (state + 0x6d2b79f5) >>> 0;
    let mixed = Math.imul(state ^ (state >>> 15), 1 | state);
    mixed = (mixed + Math.imul(mixed ^ (mixed >>> 7), 61 | mixed)) ^ mixed;
    return ((mixed ^ (mixed >>> 14)) >>> 0) / 4294967296;
  };
}

function formation(count: number, halfWidth: number, halfHeight: number, seed: number, place: (random: Random) => Point): ParticleFormation {
  const random = seededRandom(seed);
  const points = new Float32Array(count * particleStride);
  for (let index = 0; index < count; index++) points.set(place(random), index * particleStride);
  return { points, halfWidth, halfHeight };
}

/** Picks an index with probability proportional to its weight. */
function weightedPick(weights: readonly number[], random: Random) {
  const total = weights.reduce((sum, weight) => sum + weight, 0);
  let remaining = random() * total;
  for (let index = 0; index < weights.length; index++) {
    remaining -= weights[index];
    if (remaining <= 0) return index;
  }
  return weights.length - 1;
}

const shallow = (random: Random) => (random() - 0.5) * 0.08;

/** The unsettled day: points spread across and beyond the view, at every depth. */
function scatter(count: number): ParticleFormation {
  const halfWidth = 2.4;
  const halfHeight = 1.5;
  return formation(count, halfWidth, halfHeight, 11, (random) => [
    (random() * 2 - 1) * halfWidth,
    (random() * 2 - 1) * halfHeight,
    random() * 1.6 - 1.1,
    random() < 0.12 ? 1 : 0,
  ]);
}

const calendar = { columns: 7, rows: 5, cell: 0.24, gap: 0.045, today: 17, booked: [1, 3, 4, 8, 9, 11, 15, 16, 19, 22, 24, 25, 29, 31] } as const;

/** A month of days: booked days are filled, open days are outlined, and today is in the accent. */
function calendarMonth(count: number): ParticleFormation {
  const { columns, rows, cell, gap, today } = calendar;
  const booked = new Set<number>(calendar.booked);
  const halfWidth = (columns * cell + (columns - 1) * gap) / 2;
  const halfHeight = (rows * cell + (rows - 1) * gap) / 2;
  const weights = Array.from({ length: columns * rows }, (_, day) => (day === today ? 4 : booked.has(day) ? 2.2 : 1));

  return formation(count, halfWidth, halfHeight, 23, (random) => {
    const day = weightedPick(weights, random);
    const left = -halfWidth + (day % columns) * (cell + gap);
    const top = halfHeight - Math.floor(day / columns) * (cell + gap);
    let u = random();
    let v = random();
    if (day !== today && !booked.has(day)) {
      // An open day is only its outline: move the point to the nearest edge of the cell.
      const edge = Math.floor(random() * 4);
      if (edge === 0) u = 0;
      else if (edge === 1) u = 1;
      else if (edge === 2) v = 0;
      else v = 1;
    }
    return [left + u * cell, top - v * cell, shallow(random), day === today ? 1 : booked.has(day) ? 0.18 : 0];
  });
}

const sparkles = [
  { x: -0.12, y: 0, radius: 0.66, weight: 0.74 },
  { x: 0.62, y: 0.42, radius: 0.24, weight: 0.16 },
  { x: 0.56, y: -0.44, radius: 0.16, weight: 0.1 },
] as const;

/** Three four-pointed stars, the assistant's sign; each glows in the accent at its heart. */
function sparkle(count: number): ParticleFormation {
  const weights = sparkles.map((star) => star.weight);
  return formation(count, 0.9, 0.68, 37, (random) => {
    const star = sparkles[weightedPick(weights, random)];
    for (;;) {
      const x = random() * 2 - 1;
      const y = random() * 2 - 1;
      const reach = Math.sqrt(Math.abs(x)) + Math.sqrt(Math.abs(y));
      if (reach <= 1) return [star.x + x * star.radius, star.y + y * star.radius, shallow(random), 1 - reach];
    }
  });
}

/** Seven sample weeks of visits, shared by the bars formation and the reports example so the two agree. */
export const weeklyVisits = [52, 58, 55, 63, 68, 72, 86] as const;

/** Seven rising bars on a baseline; the latest week is in the accent. */
function bars(count: number): ParticleFormation {
  const width = 0.19;
  const gap = 0.085;
  const halfWidth = (weeklyVisits.length * width + (weeklyVisits.length - 1) * gap) / 2;
  const halfHeight = 0.62;
  const base = -halfHeight + 0.04;
  const tallest = Math.max(...weeklyVisits);
  const heights = weeklyVisits.map((visits) => (visits / tallest) * (halfHeight * 2 - 0.04));
  const baselineShare = 0.06;

  return formation(count, halfWidth, halfHeight, 41, (random) => {
    if (random() < baselineShare) return [(random() * 2 - 1) * halfWidth, -halfHeight + random() * 0.012, shallow(random), 0];
    const week = weightedPick(heights, random);
    const isLatest = week === weeklyVisits.length - 1;
    return [
      -halfWidth + week * (width + gap) + random() * width,
      base + random() * heights[week],
      shallow(random),
      isLatest ? 1 : 0.05 + 0.07 * week,
    ];
  });
}

/** A sun rising over a horizon, with its rays above and its light on the water below. */
function sunrise(count: number): ParticleFormation {
  const halfWidth = 1.1;
  const horizon = -0.12;
  const radius = 0.5;
  const rayCount = 7;
  const reflections = [1, 2, 3, 4] as const;
  const parts = [0.58, 0.1, 0.12, 0.2];

  return formation(count, halfWidth, 0.62, 53, (random) => {
    const part = weightedPick(parts, random);
    if (part === 0) {
      const angle = random() * Math.PI;
      const distance = Math.sqrt(random()) * radius;
      return [Math.cos(angle) * distance, horizon + Math.sin(angle) * distance, shallow(random), 1 - (distance / radius) * 0.6];
    }
    if (part === 1) {
      const ray = Math.floor(random() * rayCount);
      const angle = ((20 + (140 / (rayCount - 1)) * ray) * Math.PI) / 180 + (random() - 0.5) * 0.02;
      const distance = 0.58 + random() * 0.16;
      return [Math.cos(angle) * distance, horizon + Math.sin(angle) * distance, shallow(random), 0.5];
    }
    if (part === 2) return [(random() * 2 - 1) * halfWidth, horizon - random() * 0.01, shallow(random), 0];
    const line = reflections[Math.floor(random() * reflections.length)];
    return [(random() * 2 - 1) * (0.51 - 0.09 * line), horizon - 0.1 * line + (random() - 0.5) * 0.012, shallow(random), 0.85 - 0.15 * line];
  });
}

/**
 * Points placed on the opaque pixels of an RGBA image, such as the rasterised logotype. The
 * formation is two units wide and keeps the image's proportions. An image with nothing opaque
 * yields the scatter, so the field still has somewhere to be.
 */
export function maskFormation(rgba: Uint8ClampedArray, width: number, height: number, count: number): ParticleFormation {
  const opaque: number[] = [];
  for (let pixel = 0; pixel < width * height; pixel++) {
    if (rgba[pixel * 4 + 3] > 127) opaque.push(pixel);
  }
  if (opaque.length === 0) return scatter(count);

  const halfHeight = height / width;
  return formation(count, 1, halfHeight, 67, (random) => {
    const pixel = opaque[Math.floor(random() * opaque.length)];
    const x = ((pixel % width) + random()) / width;
    const y = (Math.floor(pixel / width) + random()) / height;
    return [x * 2 - 1, (1 - y * 2) * halfHeight, shallow(random), random() < 0.1 ? 1 : 0];
  });
}

/** Every formation with `count` points. The logotype starts as the scatter until its image is read. */
export function buildParticleFormations(count: number): Record<ParticleFormationId, ParticleFormation> {
  return {
    scatter: scatter(count),
    logotype: scatter(count),
    calendar: calendarMonth(count),
    sparkle: sparkle(count),
    bars: bars(count),
    sunrise: sunrise(count),
  };
}
