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

// White-ink marks: the header, hero and footer all sit on the anchor in both themes.
const logotypes = {
  mark: "/Logo-®.svg",
  registered: "/Master-Hel-Logotype-®.svg",
  copyright: "/Master-Hel-Logotype-©.svg",
} as const;

function Logotype({ mark, alt, className }: { mark: keyof typeof logotypes; alt: string; className?: string }) {
  return <img alt={alt} className={className} src={logotypes[mark]} />;
}

/** The public front door of Vitarish Darman, the clinic app: the sub-brand on the anchor, working samples, and the way in. */
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
      <div className={styles.anchorBand}>
        <header className={styles.header}>
          <Link aria-label={t("practiceLanding.brandHome")} className={styles.brand} href={Routes.Root}>
            <Logotype alt="" className={styles.headerMark} mark="mark" />
            <span aria-hidden="true" className={`${styles.headerSubBrand} font-tone-confident`}>
              {t("practiceLanding.subBrand")}
            </span>
          </Link>
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
        </header>
        <section aria-labelledby="landing-hero" className={styles.hero}>
          <p className={styles.byline}>
            <span>{t("practiceLanding.byline")}</span>
            <Logotype alt={t("appShell.brand")} className={styles.bylineLogotype} mark="registered" />
          </p>
          <h1 className={`${styles.heroTitle} font-tone-confident`} id="landing-hero">
            {t("practiceLanding.heroTitle")}
          </h1>
          <p className={styles.heroIntro}>{t("practiceLanding.description")}</p>
          <div className={styles.heroActions}>
            <MarketingActionLink action={leadAction} />
            {createClinicAction && <MarketingActionLink action={createClinicAction} />}
          </div>
        </section>
      </div>
      <div className={styles.main}>
        <LandingFeatures />
      </div>
      <footer className={`${styles.anchorBand} ${styles.footer}`}>
        <div className={styles.footerTop}>
          <Tooltip>
            <Button
              isIconOnly
              aria-label={t("practiceLanding.backToTop")}
              className={styles.onAnchor}
              variant="ghost"
              onPress={() => window.scrollTo({ top: 0, behavior: "smooth" })}
            >
              <FaIcon aria-hidden="true" icon="arrow-up" size="2x" />
            </Button>
            <Tooltip.Content>{t("practiceLanding.backToTop")}</Tooltip.Content>
          </Tooltip>
        </div>
        <p className={styles.footerNote}>
          {t("practiceLanding.footerDescription")}
          <br />
          {t("practiceLanding.footerRights")}
        </p>
        <Logotype alt={t("appShell.brand")} className={styles.footerLogotype} mark="copyright" />
      </footer>
    </StandaloneDocumentPage>
  );
}
