"use client";

import dynamic from "next/dynamic";
import { useState } from "react";
import { Accordion, Button, Link, Tooltip } from "@heroui/react";
import { supportedLocales, useLocalizedText, useSetLocaleCookie } from "@vitarish/localization";
import { FaIcon, FeatureItemCard, MarketingActionLink, ViewportReveal } from "@vitarish/ui";
import { whatsappChatUrl } from "./whatsapp";
import styles from "./front-door.module.css";

interface FrontDoorPageProps {
  /** Where people create their own clinic; null when this deployment offers none. */
  onboardingUrl: string | null;
  /** The WhatsApp number that answers "talk to a person"; null when none is configured. */
  whatsappNumber: string | null;
}

// Each mark has dark ink for the light theme and white ink for the dark theme; an inverted ground swaps them.
const logotypes = {
  registered: { light: "/Master-Hel-Logotype-Dark-®.svg", dark: "/Master-Hel-Logotype-®.svg" },
  copyright: { light: "/Master-Hel-Logotype-Dark-©.svg", dark: "/Master-Hel-Logotype-©.svg" },
} as const;

const GhostFibers = dynamic(() => import("./ghost-fibers"), { ssr: false });

const sectionIds = { apps: "landing-apps", questions: "landing-questions" } as const;

const practiceFeatures = [
  { key: "booking", icon: "calendar-days" },
  { key: "records", icon: "notes-medical" },
  { key: "team", icon: "users" },
] as const;
const plannedApps = [
  { key: "salon", icon: "scissors" },
  { key: "shop", icon: "bag-shopping" },
  { key: "dictionary", icon: "book-open" },
] as const;

const darmanLandingUrl = "https://darman.vitarish.com";

const questionKeys = ["what", "who", "cost"] as const;

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

/** The public front door: the Vitarish apps, a few questions, and the brand. */
export function FrontDoorPage({ onboardingUrl, whatsappNumber }: FrontDoorPageProps) {
  const [backgroundPaused, setBackgroundPaused] = useState(false);
  const { t, locale } = useLocalizedText();
  const setLocaleCookie = useSetLocaleCookie();
  const otherLocale = supportedLocales.find((candidate) => candidate !== locale);

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <Link className={styles.headerBrand} href={`#${sectionIds.apps}`}>{t("landing.brand.name")}</Link>
        <nav className={styles.headerNav} aria-label={t("landing.apps.title")}>
          <Link href={`#${sectionIds.apps}`}>{t("landing.hero.explore")}</Link>
        </nav>
        {otherLocale && (
          <MarketingActionLink
            action={{
              label: t("landing.header.language"),
              tone: "outline",
              tooltip: t("landing.header.languageTooltip"),
              icon: <FaIcon aria-hidden="true" icon="language" size="1x" />,
              onPress: () => {
                setLocaleCookie(otherLocale);
                window.location.reload();
              },
            }}
            className={styles.headerAction}
          />
        )}
      </header>
      <main className={styles.main} id="vitarish-landing-main">
        <section aria-labelledby="landing-hero" className={styles.hero}>
          <Logotype alt={t("landing.brand.name")} className={styles.heroLogotype} mark="registered" />
          <div className={styles.heroScene}>
            <div className={styles.fibers} aria-hidden="true">
              <GhostFibers glowColor="white" lineColor="white" secondaryColor="white" threads={36} density={0.6} layers={2} dpr={1} speed={0.3} mouseInteraction={false} pulseOnClick={false} paused={backgroundPaused} />
            </div>
            <div className={styles.heroText}>
              <p className={styles.eyebrow}>{t("landing.hero.eyebrow")}</p>
              <h1 className={styles.heroTitle} id="landing-hero">
                <span className={styles.heroLead}>{t("landing.hero.lead")}</span>
              </h1>
              <p className={styles.heroIntro}>{t("landing.hero.intro")}</p>
              <div className={styles.actions}>
                <MarketingActionLink action={{ label: t("landing.apps.practice.discover"), href: darmanLandingUrl, tooltip: t("landing.apps.practice.discoverTooltip"), icon: <FaIcon aria-hidden="true" icon="user-doctor" /> }} />
                <Link className={styles.heroExplore} href={`#${sectionIds.apps}`}>{t("landing.hero.explore")}</Link>
              </div>
            </div>
            <Tooltip>
              <Button isIconOnly aria-label={t(backgroundPaused ? "landing.hero.play" : "landing.hero.pause")} className={styles.motionControl} variant="ghost" onPress={() => setBackgroundPaused((value) => !value)}>
                <i aria-hidden="true" className={backgroundPaused ? "fa-light fa-play" : "fa-light fa-pause"} />
              </Button>
              <Tooltip.Content>{t(backgroundPaused ? "landing.hero.play" : "landing.hero.pause")}</Tooltip.Content>
            </Tooltip>
          </div>
        </section>
        <ViewportReveal as="section" aria-labelledby={sectionIds.apps} className={styles.band}>
          <div className={styles.sectionIntro}>
            <p className={styles.eyebrow}>{t("landing.apps.eyebrow")}</p>
            <h2 className={styles.display} id={sectionIds.apps}>{t("landing.apps.title")}</h2>
            <p className={styles.sectionText}>{t("landing.apps.intro")}</p>
          </div>
          <FeatureItemCard className={styles.app} variant="secondary">
            <FeatureItemCard.Icon>
              <FaIcon aria-hidden="true" icon="user-doctor" size="xl" />
            </FeatureItemCard.Icon>
            <FeatureItemCard.Content>
              <p className={styles.eyebrow}>{t("landing.apps.practice.status")}</p>
              <FeatureItemCard.Title className={styles.appTitle}>{t("landing.apps.practice.title")}</FeatureItemCard.Title>
              <p className={styles.productLead}>{t("landing.apps.practice.lead")}</p>
              <FeatureItemCard.Description className={styles.appText}>{t("landing.apps.practice.description")}</FeatureItemCard.Description>
            </FeatureItemCard.Content>
            <FeatureItemCard.Action className={`${styles.appAction} flex flex-wrap gap-3`}>
              <MarketingActionLink
                action={{
                  label: t("landing.apps.practice.discover"),
                  href: darmanLandingUrl,
                  tone: "outline",
                  tooltip: t("landing.apps.practice.discoverTooltip"),
                  icon: <FaIcon aria-hidden="true" icon="book-open" size="1x" />,
                }}
              />
              {onboardingUrl && (
                <MarketingActionLink
                  action={{
                    label: t("landing.apps.practice.open"),
                    href: onboardingUrl,
                    tooltip: t("landing.apps.practice.openTooltip"),
                    icon: <FaIcon aria-hidden="true" icon="hospital" size="1x" />,
                  }}
                />
              )}
            </FeatureItemCard.Action>
            <p className={styles.productNote}>{t("landing.apps.practice.note")}</p>
          </FeatureItemCard>
        </ViewportReveal>
        <ViewportReveal as="section" className={styles.story} aria-labelledby="practice-story-title">
          <div className={styles.storyHeading}>
            <p className={styles.eyebrow}>{t("landing.practiceStory.eyebrow")}</p>
            <h2 className={styles.display} id="practice-story-title">{t("landing.practiceStory.title")}</h2>
            <p className={styles.sectionText}>{t("landing.practiceStory.intro")}</p>
          </div>
          <div className={styles.featureStories}>
            {practiceFeatures.map(({ key, icon }, index) => (
              <div className={styles.featureStory} key={key}>
                <div className={styles.featureIndex} aria-hidden="true"><span>{new Intl.NumberFormat(locale, { minimumIntegerDigits: 2 }).format(index + 1)}</span><FaIcon icon={icon} /></div>
                <h3>{t(`landing.practiceStory.${key}.title`)}</h3>
                <p>{t(`landing.practiceStory.${key}.description`)}</p>
                <span className={styles.featureDetail}>{t(`landing.practiceStory.${key}.detail`)}</span>
              </div>
            ))}
          </div>
        </ViewportReveal>
        <ViewportReveal as="section" className={styles.band} aria-labelledby="horizon-title">
          <div className={styles.sectionIntro}>
            <p className={styles.eyebrow}>{t("landing.horizon.eyebrow")}</p>
            <h2 className={styles.display} id="horizon-title">{t("landing.horizon.title")}</h2>
            <p className={styles.sectionText}>{t("landing.horizon.intro")}</p>
          </div>
          <div className={styles.plannedApps}>
            {plannedApps.map(({ key, icon }) => (
              <div className={styles.plannedApp} key={key}>
                <FaIcon aria-hidden="true" icon={icon} size="xl" />
                <div><h3>{t(`landing.horizon.${key}.title`)}</h3><p>{t(`landing.horizon.${key}.description`)}</p></div>
                <span className={styles.plannedStatus}>{t("landing.horizon.status")}</span>
              </div>
            ))}
            <p className={styles.productNote}>{t("landing.horizon.note")}</p>
          </div>
        </ViewportReveal>
        <ViewportReveal as="section" aria-labelledby={sectionIds.questions} className={styles.band}>
          <h2 className={styles.display} id={sectionIds.questions}>{t("landing.questions.title")}</h2>
          <Accordion className={styles.questions}>
            {questionKeys.map((key) => (
              <Accordion.Item key={key} id={key}>
                <Accordion.Heading>
                  <Accordion.Trigger className={styles.question}>
                    {t(`landing.questions.${key}.question`)}
                    <Accordion.Indicator className={styles.questionIndicator}>
                      <FaIcon aria-hidden="true" icon="plus" />
                    </Accordion.Indicator>
                  </Accordion.Trigger>
                </Accordion.Heading>
                <Accordion.Panel>
                  <Accordion.Body className={styles.answer}>{t(`landing.questions.${key}.answer`)}</Accordion.Body>
                </Accordion.Panel>
              </Accordion.Item>
            ))}
            {whatsappNumber && (
              <Accordion.Item id="contact">
                <Accordion.Heading>
                  <Accordion.Trigger className={styles.question}>
                    {t("landing.questions.contact.question")}
                    <Accordion.Indicator className={styles.questionIndicator}>
                      <FaIcon aria-hidden="true" icon="plus" />
                    </Accordion.Indicator>
                  </Accordion.Trigger>
                </Accordion.Heading>
                <Accordion.Panel>
                  <Accordion.Body className={styles.answer}>
                    {t("landing.questions.contact.answer")}{" "}
                    <Link
                      href={whatsappChatUrl(whatsappNumber, t("landing.questions.contact.message"))}
                      rel="noreferrer"
                      target="_blank"
                    >
                      {t("landing.questions.contact.link")}
                    </Link>
                  </Accordion.Body>
                </Accordion.Panel>
              </Accordion.Item>
            )}
          </Accordion>
        </ViewportReveal>
      </main>
      <footer className={styles.footer}>
        <div className={styles.footerTop}>
          <p className={styles.eyebrow}>{t("landing.footer.eyebrow")}</p>
          <Tooltip>
            <Button
              isIconOnly
              aria-label={t("landing.footer.top")}
              className={styles.toTop}
              variant="ghost"
              onPress={() => window.scrollTo({ top: 0, behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "instant" : "smooth" })}
            >
              <FaIcon aria-hidden="true" icon="arrow-up" size="2x" />
            </Button>
            <Tooltip.Content>{t("landing.footer.top")}</Tooltip.Content>
          </Tooltip>
        </div>
        <div className={styles.footerInvitation}>
          <h2>{t("landing.footer.title")}</h2>
          <p>{t("landing.footer.intro")}</p>
          <div className={styles.actions}>
            <MarketingActionLink action={{ label: t("landing.apps.practice.discover"), href: darmanLandingUrl, tooltip: t("landing.apps.practice.discoverTooltip"), icon: <FaIcon aria-hidden="true" icon="user-doctor" /> }} />
          </div>
        </div>
        <nav className={styles.footerNavigation} aria-label={t("landing.footer.navigation")}>
          <div><h3>{t("landing.footer.explore")}</h3><Link href={darmanLandingUrl}>{t("landing.apps.practice.title")}</Link><Link href={`#${sectionIds.apps}`}>{t("landing.hero.explore")}</Link><Link href={`#${sectionIds.questions}`}>{t("landing.footer.questions")}</Link></div>
          {whatsappNumber && <div><h3>{t("landing.footer.connect")}</h3><Link href={whatsappChatUrl(whatsappNumber, t("landing.questions.contact.message"))} rel="noreferrer" target="_blank">{t("landing.questions.contact.link")}</Link><p>{t("landing.questions.contact.answer")}</p></div>}
        </nav>
        <div className={styles.footerBottom}>
          <p className={styles.footerNote}>
            {t("landing.footer.rights")}
          </p>
          <p className={styles.footerNote}>{t("landing.footer.availability")}</p>
        </div>
        <Logotype isInverted alt={t("landing.brand.name")} className={styles.footerLogotype} mark="copyright" />
      </footer>
    </div>
  );
}
