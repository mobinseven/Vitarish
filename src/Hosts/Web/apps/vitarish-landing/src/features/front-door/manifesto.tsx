"use client";

import { useEffect, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { useLocalizedText } from "@vitarish/localization";
import { catalogs } from "../../localization/messages";
import { ManifestoFigure } from "./manifesto-figure";
import { manifestoScenes, type ManifestoScene } from "./manifesto-scenes";
import styles from "./manifesto.module.css";

interface ManifestoProps {
  /** Stops the ambient drift; scroll still moves between scenes. */
  paused: boolean;
  /** The animation control, rendered beside the stage so it stays reachable while the stage is hidden from assistive technology. */
  motionControl: ReactNode;
}

type SceneLine = { lead: string; mark?: string };

function Words({ text, offset = 0 }: { text: string; offset?: number }) {
  return text.split(" ").map((word, index) => (
    <span className={styles.word} key={index} style={{ "--w": offset + index } as CSSProperties}>
      {word}{" "}
    </span>
  ));
}

function SceneText({ scene, line }: { scene: ManifestoScene; line: SceneLine }) {
  const leadWords = line.lead.split(" ").length;
  return (
    <p className={styles.lineText}>
      <Words text={line.lead} />
      {line.mark && (
        <span className={scene.mark === "disc" ? styles.markDisc : styles.markDisplay} style={{ "--w": leadWords } as CSSProperties}>
          {line.mark}
        </span>
      )}
    </p>
  );
}

/** Reads which scene the visitor has scrolled to; one scene per step of the track. */
function useActiveScene(sceneCount: number) {
  const trackRef = useRef<HTMLDivElement>(null);
  const [active, setActive] = useState(0);

  useEffect(() => {
    const track = trackRef.current;
    if (!track) return;
    let frame = 0;
    const update = () => {
      frame = 0;
      const bounds = track.getBoundingClientRect();
      const span = bounds.height - window.innerHeight;
      const progress = span > 0 ? Math.min(Math.max(-bounds.top / span, 0), 0.9999) : 0;
      setActive(Math.floor(progress * sceneCount));
    };
    const schedule = () => {
      if (!frame) frame = window.requestAnimationFrame(update);
    };
    update();
    window.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("resize", schedule);
    return () => {
      window.removeEventListener("scroll", schedule);
      window.removeEventListener("resize", schedule);
      if (frame) window.cancelAnimationFrame(frame);
    };
  }, [sceneCount]);

  return { trackRef, active };
}

/** The brand manifesto, told one line per scroll step. */
export function Manifesto({ paused, motionControl }: ManifestoProps) {
  const { locale } = useLocalizedText();
  const text = catalogs[locale].landing.manifesto;
  const lines = text.scenes as Record<ManifestoScene["key"], SceneLine>;
  const { trackRef, active } = useActiveScene(manifestoScenes.length);

  return (
    <div className={styles.manifesto} data-paused={paused || undefined}>
      <div className="sr-only">
        {manifestoScenes.map((scene) => (
          <p key={scene.key}>{[lines[scene.key].lead, lines[scene.key].mark].filter(Boolean).join(" ")}</p>
        ))}
      </div>
      <div className={styles.track} ref={trackRef} style={{ "--scene-count": manifestoScenes.length } as CSSProperties}>
        <div className={styles.stage}>
          <div aria-hidden="true" className={styles.scenes}>
            {manifestoScenes.map((scene, index) => (
              <div
                className={styles.scene}
                data-active={index === active || undefined}
                data-figure={scene.figure}
                data-ground={scene.ground}
                data-placement={scene.placement}
                key={scene.key}
              >
                <ManifestoFigure figure={scene.figure} />
                <SceneText line={lines[scene.key]} scene={scene} />
                {index === 0 && <p className={styles.scrollHint}>{text.scrollHint}</p>}
              </div>
            ))}
          </div>
          <div className={styles.control} data-ground={manifestoScenes[active]?.ground}>{motionControl}</div>
        </div>
      </div>
    </div>
  );
}
