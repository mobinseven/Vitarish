"use client";

import Link from "next/link";
import { useRef, useState } from "react";
import { useReducedMotion } from "motion/react";
import { Button, Tooltip } from "@heroui/react";
import { FaIcon, ImmersiveRegion, MarketingActionLink, StandaloneDocumentPage, ViewportReveal } from "@vitarish/ui";
import { useAuthSession } from "@vitarish/auth";
import { Routes } from "@/app/routes";
import { darmanLogotypes } from "@/components/branding/darmanLogotypes";
import { supportedLocales, useLocalizedText, useSetLocaleCookie } from "@/localization";
import { useCreateClinicHref } from "@/features/onboarding/useCreateClinicHref";
import { LandingShowcase } from "./LandingShowcase";
import { ParticleScene } from "./ParticleScene";
import { particleAnchor, particleSlot } from "./particleAnchors";
import styles from "./owner-landing.module.css";

interface OwnerLandingPageProps {
  /** The existing onboarding destination; null when registration is not offered. */
  onboardingUrl: string | null;
}

const vitarishMark = "/Logo-®.svg";

const sectionIds = {
  opening: "landing-opening",
  showcase: "darman-showcase",
  closing: "landing-close",
} as const;

/**
 * Darman's front door, in three screens: the name, the product at work, and the way in. One field
 * of particles runs behind all three. It gathers into the logotype, then into a sign for each
 * part of the product, then into a sunrise, so a day's loose pieces are seen coming to order.
 */
export function OwnerLandingPage({ onboardingUrl }: OwnerLandingPageProps) {
  const { t, locale } = useLocalizedText();
  const setLocaleCookie = useSetLocaleCookie();
  const { isAuthenticated } = useAuthSession();
  const createClinicHref = useCreateClinicHref(onboardingUrl);
  const otherLocale = supportedLocales.find((candidate) => candidate !== locale);
  const pageRef = useRef<HTMLElement>(null);
  const [sceneLive, setSceneLive] = useState(false);
  const reducedMotion = useReducedMotion();

  const leadAction = isAuthenticated
    ? {
        label: t("practiceLanding.openClinic"),
        href: Routes.Clinic.Index,
        tooltip: t("practiceLanding.openClinicTooltip"),
        icon: <FaIcon aria-hidden="true" icon="grid-2" size="1x" />,
      }
    : {
        label: t("practiceLanding.signIn"),
        href: Routes.Login.Index,
        tooltip: t("practiceLanding.signInTooltip"),
        icon: <FaIcon aria-hidden="true" icon="arrow-right-to-bracket" size="1x" className="rtl:-scale-x-100" />,
      };
  const createClinicAction = createClinicHref
    ? {
        label: t("practiceLanding.createClinic"),
        href: createClinicHref,
        tooltip: t("practiceLanding.createClinicTooltip"),
        icon: <FaIcon aria-hidden="true" icon="sparkles" size="1x" />,
      }
    : null;
  const primaryAction = isAuthenticated ? leadAction : createClinicAction ?? leadAction;

  return (
    <StandaloneDocumentPage ref={pageRef} aria-label={t("practiceLanding.mainContent")} className={styles.page}>
      <div aria-hidden="true" className={styles.backdrop} />
      <ParticleScene scopeRef={pageRef} logotypeSrc={darmanLogotypes.onDark} onLiveChange={setSceneLive} />

      <div className={styles.headerDock}>
        <header className={styles.header}>
          <Link aria-label={t("practiceLanding.brandHome")} className={styles.brand} href={Routes.Root}>
            <img alt="" className={styles.headerMark} src={vitarishMark} />
            <span aria-hidden="true" className={`${styles.headerSubBrand} font-tone-confident`}>
              {t("practiceLanding.subBrand")}
            </span>
          </Link>
          <nav aria-label={t("practiceLanding.navigation")} className={styles.headerActions}>
            {otherLocale && (
              <MarketingActionLink
                className={styles.onAnchor}
                action={{
                  label: t("practiceLanding.otherLanguage"),
                  tone: "quiet",
                  tooltip: t("practiceLanding.otherLanguageTooltip"),
                  icon: <FaIcon aria-hidden="true" icon="language" size="1x" />,
                  onPress: () => {
                    setLocaleCookie(otherLocale);
                    window.location.reload();
                  },
                }}
              />
            )}
            <MarketingActionLink className={styles.onAnchor} action={{ ...leadAction, tone: "quiet" }} />
          </nav>
        </header>
      </div>

      <ImmersiveRegion className={styles.heroRegion} sizing="viewport">
        <section {...particleAnchor("logotype")} aria-labelledby={sectionIds.opening} className={styles.hero}>
          <div {...particleSlot} className={styles.heroMark}>
            {/* The particle field draws the logotype here; the image stands in until it does, and names the brand. */}
            <img alt={t("practiceLanding.subBrand")} className={sceneLive ? styles.stoodDown : undefined} src={darmanLogotypes.onDark} />
          </div>
          <div className={styles.heroCopy}>
            <p className={styles.eyebrow}>{t("practiceLanding.eyebrow")}</p>
            <h1 className={`${styles.heroTitle} font-tone-confident`} id={sectionIds.opening}>{t("practiceLanding.heroTitle")}</h1>
            <p className={styles.heroIntro}>{t("practiceLanding.description")}</p>
            <div className={styles.actions}>
              <MarketingActionLink action={primaryAction} />
              <Link className={styles.scrollLink} href={`#${sectionIds.showcase}`}>
                {t("practiceLanding.explore")}
                <FaIcon aria-hidden="true" icon="arrow-down" />
              </Link>
            </div>
          </div>
          {sceneLive && !reducedMotion && <p className={styles.playHint}>{t("practiceLanding.playHint")}</p>}
        </section>
      </ImmersiveRegion>

      <LandingShowcase id={sectionIds.showcase} />

      <section {...particleAnchor("sunrise")} aria-labelledby={sectionIds.closing} className={styles.closing}>
        <div {...particleSlot} aria-hidden="true" className={styles.closingEmblem} />
        <ViewportReveal className={styles.closingCopy}>
          <p className={styles.eyebrow}>{t("practiceLanding.closingEyebrow")}</p>
          <h2 className={`${styles.closingTitle} font-tone-confident`} id={sectionIds.closing}>{t("practiceLanding.closingTitle")}</h2>
          <p className={styles.heroIntro}>{t("practiceLanding.closingDescription")}</p>
          <div className={styles.actions}>
            <MarketingActionLink action={primaryAction} />
            <Link className={styles.scrollLink} href={Routes.PracticeOverview}>{t("practiceLanding.editorial.readMore")}</Link>
          </div>
        </ViewportReveal>
      </section>

      <footer className={styles.footer}>
        <p className={styles.footerNote}>{t("practiceLanding.footerRights")}</p>
        <Tooltip>
          <Button
            isIconOnly
            aria-label={t("practiceLanding.backToTop")}
            className={styles.onAnchor}
            variant="ghost"
            onPress={() => window.scrollTo({ top: 0, behavior: reducedMotion ? "instant" : "smooth" })}
          >
            <FaIcon aria-hidden="true" icon="arrow-up" size="1x" />
          </Button>
          <Tooltip.Content>{t("practiceLanding.backToTop")}</Tooltip.Content>
        </Tooltip>
      </footer>
    </StandaloneDocumentPage>
  );
}
