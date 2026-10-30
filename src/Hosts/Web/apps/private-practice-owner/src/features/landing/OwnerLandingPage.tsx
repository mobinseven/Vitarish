"use client";

import Link from "next/link";
import Image from "next/image";
import { useRef } from "react";
import { motion, useReducedMotion, useScroll, useTransform } from "motion/react";
import { Button, Surface, Tooltip } from "@heroui/react";
import { FaIcon, ImmersiveRegion, MarketingActionLink, PageContainer, StandaloneDocumentPage, ViewportReveal } from "@vitarish/ui";
import { useAuthSession } from "@vitarish/auth";
import { Routes } from "@/app/routes";
import { supportedLocales, useLocalizedText, useSetLocaleCookie } from "@/localization";
import { useCreateClinicHref } from "@/features/onboarding/useCreateClinicHref";
import { LandingFeatures } from "./LandingFeatures";
import styles from "./owner-landing.module.css";

interface OwnerLandingPageProps {
  /** The existing onboarding destination; null when registration is not offered. */
  onboardingUrl: string | null;
}

const logotypes = {
  mark: "/Logo-®.svg",
  registered: "/Master-Hel-Logotype-®.svg",
} as const;

const artwork = {
  street: "/artwork/darman/clinic-street.png",
  interior: "/artwork/darman/clinic-interior.png",
} as const;

const daySectionId = "darman-day";

function Logotype({ mark, alt, className }: { mark: keyof typeof logotypes; alt: string; className?: string }) {
  return <img alt={alt} className={className} src={logotypes[mark]} />;
}

/** Darman's front door: an illustrated arrival, followed by working product examples. */
export function OwnerLandingPage({ onboardingUrl }: OwnerLandingPageProps) {
  const { t, locale } = useLocalizedText();
  const setLocaleCookie = useSetLocaleCookie();
  const { isAuthenticated } = useAuthSession();
  const createClinicHref = useCreateClinicHref(onboardingUrl);
  const otherLocale = supportedLocales.find((candidate) => candidate !== locale);
  const heroRef = useRef<HTMLDivElement>(null);
  const closingRef = useRef<HTMLDivElement>(null);
  const reducedMotion = useReducedMotion();
  const { scrollYProgress } = useScroll({ target: heroRef, offset: ["start start", "end end"] });
  const { scrollYProgress: closingProgress } = useScroll({ target: closingRef, offset: ["start end", "end end"] });
  // Full-range keyframes keep native ScrollTimeline from returning to the underlying style.
  const copyY = useTransform(scrollYProgress, [0, 0.25, 0.85, 1], [32, 32, 0, 0]);
  const copyOpacity = useTransform(scrollYProgress, [0, 0.25, 0.8, 1], [0, 0, 1, 1]);
  const copyPointerEvents = useTransform(scrollYProgress, (progress) => progress < 0.8 ? "none" : "auto");
  const scrimOpacity = useTransform(scrollYProgress, [0, 0.2, 0.8, 1], [0, 0, 1, 1]);
  const portalTop = useTransform(scrollYProgress, [0, 0.85, 1], ["18%", "0%", "0%"]);
  const portalWidth = useTransform(scrollYProgress, [0, 0.85, 1], ["54%", "100%", "100%"]);
  const portalHeight = useTransform(scrollYProgress, [0, 0.85, 1], ["78%", "100%", "100%"]);
  const portalRadius = useTransform(scrollYProgress, [0, 0.85, 1], ["48% 48% 0% 0%", "0% 0% 0% 0%", "0% 0% 0% 0%"]);
  const streetScale = useTransform(scrollYProgress, [0, 1], [1, 1.05]);
  const ornamentOpacity = useTransform(scrollYProgress, [0, 0.2, 1], [1, 0, 0]);
  const closingScale = useTransform(closingProgress, [0, 1], [1.15, 1]);

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
    <StandaloneDocumentPage aria-label={t("practiceLanding.mainContent")} className={styles.page}>
      <div className={styles.headerDock}>
        <header className={styles.header}>
          <Link aria-label={t("practiceLanding.brandHome")} className={styles.brand} href={Routes.Root}>
            <Logotype alt="" className={styles.headerMark} mark="mark" />
            <span aria-hidden="true" className={`${styles.headerSubBrand} font-tone-confident`}>
              {t("practiceLanding.subBrand")}
            </span>
          </Link>
          <nav aria-label={t("practiceLanding.navigation")} className={styles.headerActions}>
            <Link className={styles.dayLink} href={`#${daySectionId}`}>{t("practiceLanding.explore")}</Link>
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
          </nav>
        </header>
      </div>

      <div className={styles.heroJourney} ref={heroRef}>
        <ImmersiveRegion className={styles.heroStage} sizing="viewport">
          <motion.div
            className={styles.heroCopy}
            style={reducedMotion ? undefined : { y: copyY, opacity: copyOpacity, pointerEvents: copyPointerEvents }}
            onFocusCapture={() => {
              const hero = heroRef.current;
              const stage = hero?.firstElementChild;
              if (!hero || !stage || getComputedStyle(stage).position !== "sticky") return;
              // Keyboard focus reveals the completed scene, including its action.
              window.scrollTo({ top: hero.offsetTop + hero.offsetHeight - window.innerHeight, behavior: "instant" });
            }}
          >
            <p className={styles.eyebrow}>{t("practiceLanding.eyebrow")}</p>
            <h1 className={`${styles.heroTitle} font-tone-confident`}>{t("practiceLanding.heroTitle")}</h1>
            <p className={styles.heroIntro}>{t("practiceLanding.description")}</p>
            <div className={styles.heroActions}>
              <MarketingActionLink action={primaryAction} />
              {!isAuthenticated && createClinicAction && (
                <MarketingActionLink className={styles.onAnchor} action={{ ...leadAction, tone: "quiet" }} />
              )}
              <Link className={styles.scrollLink} href={`#${daySectionId}`}>
                {t("practiceLanding.stepInside")}
                <FaIcon aria-hidden="true" icon="arrow-down" />
              </Link>
            </div>
          </motion.div>

          <motion.div aria-hidden="true" className={styles.archLines} style={reducedMotion ? undefined : { opacity: ornamentOpacity }}>
            <span /><span /><span />
          </motion.div>
          <motion.p className={styles.sceneMargin} style={reducedMotion ? undefined : { opacity: ornamentOpacity }}>
            {t("practiceLanding.heroFootnote")}
          </motion.p>
          <motion.div
            className={styles.streetPortal}
            style={reducedMotion ? undefined : { top: portalTop, width: portalWidth, height: portalHeight, borderRadius: portalRadius }}
          >
            <motion.div className={styles.artFrame} style={reducedMotion ? undefined : { scale: streetScale }}>
              <Image alt={t("practiceLanding.streetAlt")} className={styles.artImage} src={artwork.street} fill priority sizes="100vw" />
            </motion.div>
            <motion.div aria-hidden="true" className={styles.heroScrim} style={reducedMotion ? undefined : { opacity: scrimOpacity }} />
          </motion.div>
        </ImmersiveRegion>
      </div>

      <PageContainer className={styles.introduction} id={daySectionId} width="content">
        <ViewportReveal>
          <p className={styles.eyebrow}>{t("practiceLanding.introEyebrow")}</p>
          <h2 className={`${styles.introTitle} font-tone-confident`}>{t("practiceLanding.introTitle")}</h2>
          <p className={styles.introDescription}>{t("practiceLanding.introDescription")}</p>
        </ViewportReveal>
        <div className={styles.abilityIndex}>
          <span><FaIcon aria-hidden="true" icon="calendar-check" />{t("practiceLanding.abilityAppointments")}</span>
          <span><FaIcon aria-hidden="true" icon="folder-open" />{t("practiceLanding.abilityRecords")}</span>
          <span><FaIcon aria-hidden="true" icon="users" />{t("practiceLanding.abilityTeam")}</span>
          <span><FaIcon aria-hidden="true" icon="message-bot" />{t("practiceLanding.abilityResponses")}</span>
        </div>
      </PageContainer>

      <LandingFeatures interiorArtwork={artwork.interior} />

      <div className={styles.closingScene} ref={closingRef}>
        <motion.div className={styles.closingArtwork} style={reducedMotion ? undefined : { scale: closingScale }}>
          <Image alt="" className={styles.artImage} src={artwork.street} fill sizes="100vw" />
        </motion.div>
        <div aria-hidden="true" className={styles.closingScrim} />
        <Surface render={(props) => <section {...props} />} className={styles.closing} aria-labelledby="landing-close">
          <ViewportReveal className={styles.closingContent}>
            <p className={styles.eyebrow}>{t("practiceLanding.closingEyebrow")}</p>
            <h2 className={`${styles.closingTitle} font-tone-confident`} id="landing-close">{t("practiceLanding.closingTitle")}</h2>
            <p className={styles.closingDescription}>{t("practiceLanding.closingDescription")}</p>
            <MarketingActionLink action={primaryAction} />
            <Link className={styles.overviewLink} href={Routes.PracticeOverview}>{t("practiceLanding.editorial.readMore")}</Link>
          </ViewportReveal>
        </Surface>
      </div>
      <footer className={styles.footer}>
        <span className={`${styles.footerSubBrand} font-tone-confident`}>{t("practiceLanding.subBrand")}</span>
        <div className={styles.footerBottom}>
          <p className={styles.byline}>
            <span>{t("practiceLanding.byline")}</span>
            <Logotype alt={t("appShell.brand")} className={styles.bylineLogotype} mark="registered" />
          </p>
          <p className={styles.footerNote}>{t("practiceLanding.footerRights")}</p>
          <Tooltip>
            <Button
              isIconOnly
              aria-label={t("practiceLanding.backToTop")}
              className={styles.onAnchor}
              variant="ghost"
              onPress={() => window.scrollTo({ top: 0, behavior: reducedMotion ? "instant" : "smooth" })}
            >
              <FaIcon aria-hidden="true" icon="arrow-up" size="2x" />
            </Button>
            <Tooltip.Content>{t("practiceLanding.backToTop")}</Tooltip.Content>
          </Tooltip>
        </div>
      </footer>
    </StandaloneDocumentPage>
  );
}
