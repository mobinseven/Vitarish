"use client";

import { useEffect, useRef, type RefObject } from "react";
import { useReducedMotion } from "motion/react";
import { particleAnchorAttributes } from "./particleAnchors";
import {
  buildParticleFormations,
  maskFormation,
  particleFormationIds,
  particleStride,
  type ParticleFormationId,
} from "./particleFormations";
import styles from "./particle-scene.module.css";

interface ParticleSceneProps {
  /** The page whose anchors (see particleAnchors) the field follows. */
  scopeRef: RefObject<HTMLElement | null>;
  /** The image the "logotype" formation is drawn from. */
  logotypeSrc: string;
  /** Reports whether the field is drawing, so the page can show its still fallbacks otherwise. */
  onLiveChange: (live: boolean) => void;
}

const vertexSource = `
attribute vec4 a_from;
attribute vec4 a_to;
attribute vec4 a_seed;
uniform vec3 u_fromPlace;
uniform vec3 u_toPlace;
uniform vec2 u_loose;
uniform float u_mix;
uniform float u_intro;
uniform float u_time;
uniform float u_aspect;
uniform float u_pointSize;
uniform vec3 u_pointer;
uniform vec2 u_tilt;
uniform vec4 u_pulse;
varying float v_tone;
varying float v_alpha;

float ease(float t) { return t * t * (3.0 - 2.0 * t); }

void main() {
  // Each particle leaves a little after the last, so a shape pours into the next.
  float s = ease(clamp((u_mix - a_seed.w * 0.4) / 0.6, 0.0, 1.0));
  vec3 from = vec3(a_from.xy * u_fromPlace.z + u_fromPlace.xy, a_from.z);
  vec3 to = vec3(a_to.xy * u_toPlace.z + u_toPlace.xy, a_to.z);
  vec3 p = mix(from, to, s);
  float loose = mix(u_loose.x, u_loose.y, s);

  // On the way, particles arc around the midpoint instead of sliding straight across.
  float midway = sin(3.14159 * s);
  vec2 centre = mix(u_fromPlace.xy, u_toPlace.xy, s);
  vec2 arm = p.xy - centre;
  p.xy += midway * (vec2(-arm.y, arm.x) * 0.45 + (a_seed.xy - 0.5) * 0.5);
  p.z += midway * (a_seed.z - 0.5) * 0.9;

  float phase = a_seed.x * 6.2831;
  p.xy += (0.004 + 0.05 * loose) * vec2(
    sin(u_time * (0.3 + a_seed.y * 0.5) + phase),
    cos(u_time * (0.25 + a_seed.z * 0.5) + phase * 1.7));

  // The opening: every particle starts adrift and spirals into place.
  float arrived = ease(clamp((u_intro - a_seed.w * 0.5) / 0.5, 0.0, 1.0));
  vec3 adrift = vec3((a_seed.x * 2.0 - 1.0) * u_aspect * 1.4, (a_seed.y * 2.0 - 1.0) * 1.4, a_seed.z * 1.5 - 1.0);
  p = mix(adrift, p, arrived);
  p.xy += (1.0 - arrived) * arrived * 0.6 * vec2(-p.y, p.x);

  // The pointer parts the field, and a press sends a ring through it.
  vec2 away = p.xy - u_pointer.xy;
  p.xy += normalize(away + 0.00001) * u_pointer.z * 0.09 * exp(-dot(away, away) / 0.03);
  vec2 fromPulse = p.xy - u_pulse.xy;
  float ring = (length(fromPulse) - u_pulse.z * 1.6) / 0.12;
  float wave = exp(-ring * ring) * u_pulse.w;
  p.xy += normalize(fromPulse + 0.00001) * wave * 0.12;

  float perspective = 1.0 / (1.0 - p.z * 0.25);
  vec2 projected = p.xy * perspective + p.z * u_tilt * 0.12;
  gl_Position = vec4(projected.x / u_aspect, projected.y, 0.0, 1.0);
  gl_PointSize = (1.3 + a_seed.z * 2.3) * perspective * mix(1.0, 1.5, loose) * u_pointSize;

  float twinkle = 0.65 + 0.35 * sin(u_time * (1.0 + a_seed.x * 2.0) + a_seed.y * 40.0);
  v_alpha = twinkle * mix(0.95, 0.4, loose) * (0.35 + 0.65 * arrived) + wave * 0.6;
  v_tone = mix(a_from.w, a_to.w, s);
}
`;

const fragmentSource = `
precision mediump float;
uniform vec3 u_base;
uniform vec3 u_accent;
varying float v_tone;
varying float v_alpha;

void main() {
  float glow = smoothstep(1.0, 0.0, length(gl_PointCoord - 0.5) * 2.0);
  float alpha = glow * glow * v_alpha;
  gl_FragColor = vec4(mix(u_base, u_accent, v_tone) * alpha, alpha);
}
`;

const morphSeconds = 1.5;
const introSeconds = 2.8;
const pulseSeconds = 1.4;
const pointerRestMilliseconds = 1200;
/** How long the opening waits for the logotype image before starting without it. */
const logotypeWaitMilliseconds = 1500;
const logotypeRaster = { width: 960, height: 160 } as const;
const particleCounts = { compact: 7000, full: 16000 } as const;
const compactViewQuery = "(max-width: 48rem), (pointer: coarse)";

const isFormationId = (value: string | null): value is ParticleFormationId =>
  particleFormationIds.some((id) => id === value);

/** A CSS colour as 0-1 channels, read by painting it: this resolves any colour syntax the theme uses. */
function channels(color: string): [number, number, number] {
  const probe = document.createElement("canvas");
  probe.width = probe.height = 1;
  const context = probe.getContext("2d", { willReadFrequently: true });
  if (!context) return [1, 1, 1];
  context.fillStyle = color;
  context.fillRect(0, 0, 1, 1);
  const [red, green, blue] = context.getImageData(0, 0, 1, 1).data;
  return [red / 255, green / 255, blue / 255];
}

function compile(gl: WebGLRenderingContext) {
  const program = gl.createProgram();
  for (const [type, source] of [[gl.VERTEX_SHADER, vertexSource], [gl.FRAGMENT_SHADER, fragmentSource]] as const) {
    const shader = gl.createShader(type);
    if (!shader) return null;
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) return null;
    gl.attachShader(program, shader);
  }
  gl.linkProgram(program);
  return gl.getProgramParameter(program, gl.LINK_STATUS) ? program : null;
}

/**
 * The landing page's illustration: one field of particles, fixed behind the page, that settles
 * into the formation of whichever anchor is nearest the middle of the view and pours into the
 * next when that changes. It is decoration only. Without WebGL it reports that it is not live
 * and draws nothing; with reduced motion it holds still and changes formation without travel.
 */
export function ParticleScene({ scopeRef, logotypeSrc, onLiveChange }: ParticleSceneProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const reducedMotion = useReducedMotion();

  useEffect(() => {
    const canvas = canvasRef.current;
    const scope = scopeRef.current;
    const gl = canvas?.getContext("webgl", { alpha: true, antialias: false, premultipliedAlpha: true });
    const program = gl ? compile(gl) : null;
    if (!canvas || !scope || !gl || !program) {
      onLiveChange(false);
      return;
    }

    const count = window.matchMedia(compactViewQuery).matches ? particleCounts.compact : particleCounts.full;
    const formations = buildParticleFormations(count);
    const buffers = {} as Record<ParticleFormationId, WebGLBuffer | null>;
    const upload = (id: ParticleFormationId) => {
      buffers[id] ??= gl.createBuffer();
      gl.bindBuffer(gl.ARRAY_BUFFER, buffers[id]);
      gl.bufferData(gl.ARRAY_BUFFER, formations[id].points, gl.STATIC_DRAW);
    };
    particleFormationIds.forEach(upload);

    const seeds = new Float32Array(count * 4).map(() => Math.random());
    const seedBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, seedBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, seeds, gl.STATIC_DRAW);

    gl.useProgram(program);
    const attribute = (name: string) => gl.getAttribLocation(program, name);
    const uniform = (name: string) => gl.getUniformLocation(program, name);
    const locations = { from: attribute("a_from"), to: attribute("a_to"), seed: attribute("a_seed") };
    const point = (location: number, buffer: WebGLBuffer | null) => {
      gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
      gl.enableVertexAttribArray(location);
      gl.vertexAttribPointer(location, particleStride, gl.FLOAT, false, 0, 0);
    };
    point(locations.seed, seedBuffer);
    gl.disable(gl.DEPTH_TEST);
    gl.enable(gl.BLEND);
    gl.blendFunc(gl.ONE, gl.ONE);

    // The stylesheet owns the colours: the canvas's text colour is the base, its outline colour the accent.
    const style = getComputedStyle(canvas);
    gl.uniform3fv(uniform("u_base"), channels(style.color));
    gl.uniform3fv(uniform("u_accent"), channels(style.outlineColor));

    let introStart: number | null = reducedMotion ? 0 : null;
    const logotype = new Image();
    logotype.onload = () => {
      const raster = document.createElement("canvas");
      raster.width = logotypeRaster.width;
      raster.height = logotypeRaster.height;
      const context = raster.getContext("2d", { willReadFrequently: true });
      if (!context) return;
      context.drawImage(logotype, 0, 0, raster.width, raster.height);
      formations.logotype = maskFormation(context.getImageData(0, 0, raster.width, raster.height).data, raster.width, raster.height, count);
      upload("logotype");
      introStart ??= performance.now();
      requestDraw();
    };
    logotype.src = logotypeSrc;
    const logotypeWait = window.setTimeout(() => { introStart ??= performance.now(); }, logotypeWaitMilliseconds);

    type Target = { element: Element; formation: ParticleFormationId };
    const anchors = Array.from(scope.querySelectorAll(`[${particleAnchorAttributes.formation}]`));
    const slots = new Map(anchors.map((anchor) => [anchor, anchor.querySelector(`[${particleAnchorAttributes.slot}]`)]));
    const targetOf = (element: Element): Target => {
      const formation = element.getAttribute(particleAnchorAttributes.formation);
      return { element, formation: isFormationId(formation) ? formation : "scatter" };
    };

    let width = 1;
    let height = 1;
    const resize = () => {
      const density = Math.min(window.devicePixelRatio || 1, 2);
      width = canvas.clientWidth || 1;
      height = canvas.clientHeight || 1;
      canvas.width = Math.round(width * density);
      canvas.height = Math.round(height * density);
      gl.viewport(0, 0, canvas.width, canvas.height);
      gl.uniform1f(uniform("u_aspect"), width / height);
      gl.uniform1f(uniform("u_pointSize"), canvas.height / 900);
      requestDraw();
    };

    /** The anchor whose centre is nearest the centre of the view. */
    const nearestAnchor = () => {
      let nearest: Element | null = null;
      let nearestDistance = Infinity;
      for (const anchor of anchors) {
        const box = anchor.getBoundingClientRect();
        const distance = Math.abs(box.top + box.height / 2 - height / 2);
        if (distance < nearestDistance) [nearest, nearestDistance] = [anchor, distance];
      }
      return nearest;
    };

    /** Offset and scale, in half-view-heights, that fit a target's formation into its slot (or over the whole view). */
    const placeOf = ({ element, formation }: Target): [number, number, number] => {
      const { halfWidth, halfHeight } = formations[formation];
      const slot = slots.get(element);
      if (!slot) return [0, 0, Math.max((width / height) * 1.05 / halfWidth, 1.05 / halfHeight)];
      const box = slot.getBoundingClientRect();
      const half = height / 2;
      return [
        (box.left + box.width / 2 - width / 2) / half,
        -(box.top + box.height / 2 - half) / half,
        Math.min(box.width / (2 * halfWidth), box.height / (2 * halfHeight)) / half,
      ];
    };
    const looseness = (target: Target) => (target.formation === "scatter" ? 1 : 0);

    const first = nearestAnchor();
    let from: Target | null = first && targetOf(first);
    let to = from;
    let morphStart = -Infinity;
    const pointer = { x: 0, y: 0, strength: 0, movedAt: -Infinity, tiltX: 0, tiltY: 0 };
    const pulse = { x: 0, y: 0, startedAt: -Infinity };

    let frame = 0;
    const draw = (now: number) => {
      frame = 0;
      const nearest = nearestAnchor();
      if (!nearest || !from || !to) return;
      const wanted = targetOf(nearest);
      let mix = reducedMotion ? 1 : Math.min(1, (now - morphStart) / (morphSeconds * 1000));
      // A change waits for the one under way, so particles never jump.
      if (mix >= 1 && (wanted.element !== to.element || wanted.formation !== to.formation)) {
        [from, to, morphStart] = [to, wanted, now];
        mix = reducedMotion ? 1 : 0;
      }

      const resting = now - pointer.movedAt > pointerRestMilliseconds;
      pointer.strength += ((resting ? 0 : 1) - pointer.strength) * 0.08;
      pointer.tiltX += (pointer.x / (width / height) - pointer.tiltX) * 0.05;
      pointer.tiltY += (pointer.y - pointer.tiltY) * 0.05;
      const pulseAge = (now - pulse.startedAt) / 1000;

      point(locations.from, buffers[from.formation]);
      point(locations.to, buffers[to.formation]);
      gl.uniform3fv(uniform("u_fromPlace"), placeOf(from));
      gl.uniform3fv(uniform("u_toPlace"), placeOf(to));
      gl.uniform2f(uniform("u_loose"), looseness(from), looseness(to));
      gl.uniform1f(uniform("u_mix"), mix);
      gl.uniform1f(uniform("u_intro"), introStart === null ? 0 : reducedMotion ? 1 : Math.min(1, (now - introStart) / (introSeconds * 1000)));
      gl.uniform1f(uniform("u_time"), reducedMotion ? 0 : now / 1000);
      gl.uniform3f(uniform("u_pointer"), pointer.x, pointer.y, reducedMotion ? 0 : pointer.strength);
      gl.uniform2f(uniform("u_tilt"), reducedMotion ? 0 : pointer.tiltX, reducedMotion ? 0 : pointer.tiltY);
      gl.uniform4f(uniform("u_pulse"), pulse.x, pulse.y, pulseAge, reducedMotion || pulseAge > pulseSeconds ? 0 : 1 - pulseAge / pulseSeconds);
      gl.clearColor(0, 0, 0, 0);
      gl.clear(gl.COLOR_BUFFER_BIT);
      gl.drawArrays(gl.POINTS, 0, count);

      // With motion the field is always alive; without it, it is redrawn only when something changes.
      if (!reducedMotion) requestDraw();
    };
    function requestDraw() {
      frame ||= requestAnimationFrame(draw);
    }

    const toField = (event: PointerEvent): [number, number] => [(event.clientX - width / 2) / (height / 2), -(event.clientY - height / 2) / (height / 2)];
    const onPointerMove = (event: PointerEvent) => {
      [pointer.x, pointer.y] = toField(event);
      pointer.movedAt = performance.now();
    };
    const onPointerDown = (event: PointerEvent) => {
      [pulse.x, pulse.y] = toField(event);
      pulse.startedAt = performance.now();
    };
    const onContextLost = (event: Event) => {
      event.preventDefault();
      cancelAnimationFrame(frame);
      onLiveChange(false);
    };

    const resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(canvas);
    // An anchor may change its formation (the showcase does); without motion that needs its own redraw.
    const anchorObserver = new MutationObserver(requestDraw);
    anchors.forEach((anchor) => anchorObserver.observe(anchor, { attributes: true, attributeFilter: [particleAnchorAttributes.formation] }));
    window.addEventListener("scroll", requestDraw, { passive: true });
    window.addEventListener("pointermove", onPointerMove, { passive: true });
    window.addEventListener("pointerdown", onPointerDown, { passive: true });
    canvas.addEventListener("webglcontextlost", onContextLost);
    resize();
    onLiveChange(true);

    return () => {
      cancelAnimationFrame(frame);
      window.clearTimeout(logotypeWait);
      logotype.onload = null;
      resizeObserver.disconnect();
      anchorObserver.disconnect();
      window.removeEventListener("scroll", requestDraw);
      window.removeEventListener("pointermove", onPointerMove);
      window.removeEventListener("pointerdown", onPointerDown);
      canvas.removeEventListener("webglcontextlost", onContextLost);
      Object.values(buffers).forEach((buffer) => gl.deleteBuffer(buffer));
      gl.deleteBuffer(seedBuffer);
      gl.deleteProgram(program);
    };
  }, [scopeRef, logotypeSrc, onLiveChange, reducedMotion]);

  return <canvas ref={canvasRef} aria-hidden="true" className={styles.canvas} />;
}
