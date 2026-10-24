"use client";

import Link from "next/link";
import { Button, Tooltip } from "@heroui/react";
import { FaIcon, MarketingActionLink, StandaloneDocumentPage } from "@vitarish/ui";
import { useAuthSession } from "@vitarish/auth";
import { Routes } from "@/app/routes";
import { setLocaleCookie, supportedLocales, useLocalizedText } from "@/localization";
import { useCreateClinicHref } from "@/features/onboarding/useCreateClinicHref";
import { LandingFeatures } from "./LandingFeatures";
import styles from "./owner-landing.module.css";

interface OwnerLandingPageProps {
  /** The onboarding host's address, where people create their own clinic; null when not offered. */
  onboardingUrl: string | null;
}

// Each mark has dark ink for the light theme and white ink for the dark theme; an inverted ground swaps them.
const logotypes = {
  mark: { light: "/Logo-Dark-®.svg", dark: "/Logo-®.svg" },
  registered: { light: "/Master-Hel-Logotype-Dark-®.svg", dark: "/Master-Hel-Logotype-®.svg" },
  copyright: { light: "/Master-Hel-Logotype-Dark-©.svg", dark: "/Master-Hel-Logotype-©.svg" },
} as const;

function Logotype({
  mark,
  alt,
  className,
  isInverted = false,
}: {
  mark: keyof typeof logotypes;
  alt: string;
  className?: string;
  isInverted?: boolean;
}) {
  const { light, dark } = logotypes[mark];
  return (
    <span className={className}>
      <img alt={alt} className="block h-full w-auto dark:hidden" src={isInverted ? dark : light} />
      <img alt={alt} className="hidden h-full w-auto dark:block" src={isInverted ? light : dark} />
    </span>
  );
}

/** The public front door of the clinic owner app: the Vitarish look, a few working samples, and the way in. */
export function OwnerLandingPage({ onboardingUrl }: OwnerLandingPageProps) {
  const { t, locale } = useLocalizedText();
  const { isAuthenticated } = useAuthSession();
  const createClinicHref = useCreateClinicHref(onboardingUrl);
  const otherLocale = supportedLocales.find((candidate) => candidate !== locale);

  const leadAction = isAuthenticated
    ? {
        label: t("practiceLanding.openClinic"),
        href: Routes.Clinic.Index,
        tooltip: t("practiceLanding.openClinicTooltip"),
        icon: <FaIcon aria-hidden="true" icon="hospital" size="1x" />,
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
        tone: "secondary" as const,
        tooltip: t("practiceLanding.createClinicTooltip"),
        icon: <FaIcon aria-hidden="true" icon="hospital" size="1x" />,
      }
    : null;

  return (
    <StandaloneDocumentPage aria-label={t("practiceLanding.mainContent")} className={styles.page}>
      <header className={styles.header}>
        <Link aria-label={t("practiceLanding.brandHome")} href={Routes.Root}>
          <Logotype alt="" className={styles.headerMark} mark="mark" />
        </Link>
        <div className={styles.headerActions}>
          {otherLocale && (
            <MarketingActionLink
              action={{
                label: t("practiceLanding.otherLanguage"),
                tone: "outline",
                tooltip: t("practiceLanding.otherLanguageTooltip"),
                icon: <FaIcon aria-hidden="true" icon="language" size="1x" />,
                onPress: () => {
                  setLocaleCookie(otherLocale);
                  window.location.reload();
                },
              }}
            />
          )}
        </div>
      </header>
      <div className={styles.main}>
        <section aria-labelledby="landing-hero" className={styles.hero}>
          <Logotype alt={t("appShell.brand")} className={styles.logotype} mark="registered" />
          <div className={styles.heroText}>
            <h1 className={styles.heroTitle} id="landing-hero">{t("practiceLanding.title")}</h1>
            <p className={styles.heroIntro}>{t("practiceLanding.description")}</p>
            <div className={styles.heroActions}>
              <MarketingActionLink action={leadAction} />
              {createClinicAction && <MarketingActionLink action={createClinicAction} />}
            </div>
          </div>
        </section>
        <LandingFeatures />
      </div>
      <footer className={styles.footer}>
        <div className={styles.footerTop}>
          <Tooltip>
            <Button
              isIconOnly
              aria-label={t("practiceLanding.backToTop")}
              className={styles.toTop}
              variant="ghost"
              onPress={() => window.scrollTo({ top: 0, behavior: "smooth" })}
            >
              <FaIcon aria-hidden="true" icon="arrow-up" size="2x" />
            </Button>
            <Tooltip.Content>{t("practiceLanding.backToTop")}</Tooltip.Content>
          </Tooltip>
        </div>
        <div className={styles.footerBottom}>
          <p className={styles.footerNote}>
            {t("practiceLanding.footerDescription")}
            <br />
            {t("practiceLanding.footerRights")}
          </p>
        </div>
        <Logotype isInverted alt={t("appShell.brand")} className={`${styles.logotype} ${styles.footerLogotype}`} mark="copyright" />
      </footer>
    </StandaloneDocumentPage>
  );
}
