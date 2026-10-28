"use client";

import { EditorialContentPage } from "@vitarish/contents/editorial";
import { FaIcon, MarketingActionLink } from "@vitarish/ui";
import { Routes } from "@/app/routes";
import { supportedLocales, useLocalizedText, useSetLocaleCookie } from "@/localization";
import { catalogs } from "@/localization/messages";
import styles from "./practice-overview.module.css";

export function PracticeOverviewPageWrapper() {
  const { locale } = useLocalizedText();
  const setLocaleCookie = useSetLocaleCookie();
  const otherLocale = supportedLocales.find((candidate) => candidate !== locale);
  const text = catalogs[locale].practiceLanding;
  const homeLink = { href: Routes.Root, label: text.brandHome };
  const signInLink = { href: Routes.Login.Index, label: text.signIn };
  const links = [homeLink, signInLink];

  return (
    <EditorialContentPage
      className={styles.page}
      brand={text.subBrand}
      homeHref={Routes.Root}
      homeLabel={text.brandHome}
      navigationLabel={text.editorial.navigation}
      navigationTooltip={text.editorial.navigationTooltip}
      navigationLinks={[homeLink]}
      navigationGroups={[{ label: text.editorial.navigation, tooltip: text.editorial.navigationTooltip, links: [signInLink] }]}
      headerAction={otherLocale && (
        <MarketingActionLink className={styles.headerAction} action={{
          label: text.otherLanguage,
          tooltip: text.otherLanguageTooltip,
          tone: "quiet",
          icon: <FaIcon aria-hidden="true" icon="language" />,
          onPress: () => { setLocaleCookie(otherLocale); window.location.reload(); },
        }} />
      )}
      title={text.editorial.title}
      metadata={[
        { label: text.editorial.product, value: text.subBrand },
        { label: text.editorial.category, value: text.editorial.categoryValue },
      ]}
      footerDescription={text.description}
      footerNavigationLabel={text.editorial.navigation}
      footerLinks={links}
    >
      <p>{text.description}</p>
      <h2>{text.featureBookings}</h2>
      <p>{text.featureBookingsDescription}</p>
      <h2>{text.featureReports}</h2>
      <p>{text.featureReportsDescription}</p>
      <h2>{text.featureAssistant}</h2>
      <p>{text.featureAssistantDescription}</p>
    </EditorialContentPage>
  );
}
