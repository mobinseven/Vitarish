import type {ReactNode} from "react";
import Image from "next/image";
import {motion, useReducedMotion} from "motion/react";
import {Tooltip} from "@heroui/react";

import {Iconify} from "@/features/landing/icons/iconify";
import type {Figure} from "@/features/landing/data/figures";
import {useLocalizedText} from "@/localization";

export function Reveal({
  children,
  delay = 0,
  y = 16,
  className,
}: {
  children: ReactNode;
  delay?: number;
  y?: number;
  className?: string;
}) {
  const reduce = useReducedMotion();

  if (reduce) return <div className={className}>{children}</div>;

  return (
    <motion.div
      className={className}
      initial={{opacity: 0, y}}
      whileInView={{opacity: 1, y: 0}}
      viewport={{once: true, margin: "-80px"}}
      transition={{duration: 0.5, delay, ease: [0.16, 1, 0.3, 1]}}
    >
      {children}
    </motion.div>
  );
}

export function DrawLineX({className, delay = 0}: {className?: string; delay?: number}) {
  const reduce = useReducedMotion();

  if (reduce) return <div className={`bp-dotted-x ${className ?? ""}`} />;

  return (
    <motion.div
      className={`bp-dotted-x origin-left ${className ?? ""}`}
      initial={{scaleX: 0, opacity: 0}}
      whileInView={{scaleX: 1, opacity: 1}}
      viewport={{once: true, margin: "-60px"}}
      transition={{duration: 0.7, delay, ease: [0.16, 1, 0.3, 1]}}
    />
  );
}

export function SectionLabel({
  index,
  children,
  className,
}: {
  index?: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={`flex items-center gap-3 ${className ?? ""}`}>
      <span className="bg-accent inline-block size-1.5" />
      <span className="bp-label bp-label-accent">
        {index ? <span className="text-muted mr-2">{index}</span> : null}
        {children}
      </span>
    </div>
  );
}

export function RulerLine({accent = false, className}: {accent?: boolean; className?: string}) {
  return (
    <div className={`relative w-full ${className ?? ""}`} aria-hidden="true">
      <div className={`bp-ruler w-full ${accent ? "bp-ruler-accent" : ""}`} />
    </div>
  );
}

export function CornerMarks({className}: {className?: string}) {
  const base = "pointer-events-none absolute size-2.5 border-accent/60";

  return (
    <div className={`pointer-events-none absolute inset-0 ${className ?? ""}`} aria-hidden="true">
      <span className={`${base} left-0 top-0 border-l border-t`} />
      <span className={`${base} right-0 top-0 border-r border-t`} />
      <span className={`${base} bottom-0 left-0 border-b border-l`} />
      <span className={`${base} bottom-0 right-0 border-b border-r`} />
    </div>
  );
}

export function Figure({
  figure,
  fig,
  caption,
  className,
  imgClassName,
}: {
  figure: Figure;
  fig: string;
  caption?: string;
  className?: string;
  imgClassName?: string;
}) {
  const {t} = useLocalizedText();

  return (
    <div className={`group relative ${className ?? ""}`}>
      <div className="border-border bg-surface relative overflow-hidden border">
        <div className="bp-grid-fine absolute inset-0 opacity-60" aria-hidden="true" />
        <div className="absolute left-3 top-3 z-10 flex items-center gap-2">
          <span className="bp-label bp-label-accent">{fig}</span>
        </div>
        <div className="absolute right-3 top-3 z-10">
          <Tooltip delay={150} closeDelay={100}>
            <button
              aria-label={t("blueprint.viewGenerationPrompt")}
              className="border-border bg-background/70 text-muted hover:text-accent hover:border-accent/50 flex size-6 items-center justify-center border backdrop-blur transition-colors"
              type="button"
            >
              <Iconify icon="circle-info" className="size-3.5" />
            </button>
            <Tooltip.Content className="max-w-xs">
              <p className="bp-label mb-1 not-italic">{t("blueprint.generationPrompt")}</p>
              <p className="text-xs leading-relaxed">{figure.prompt}</p>
            </Tooltip.Content>
          </Tooltip>
        </div>
        <Image
          alt={figure.alt}
          className={`relative z-[1] w-full h-auto object-cover ${imgClassName ?? ""}`}
          loading="lazy"
          src={figure.src}
          width={0}
          height={0}
          sizes="100vw"
          unoptimized
        />
        <CornerMarks />
      </div>
      {caption ? (
        <div className="mt-3 flex items-start gap-2">
          <span className="bp-label mt-px">{fig}</span>
          <span className="text-muted text-xs leading-relaxed">{caption}</span>
        </div>
      ) : null}
    </div>
  );
}

export function Annotation({children, className}: {children: ReactNode; className?: string}) {
  return <span className={`bp-label ${className ?? ""}`}>{children}</span>;
}
