"use client";

import { Link } from "@heroui/react";
import { EditorialContentPage } from "@vitarish/contents/editorial";
import { supportedLocales, useLocalizedText, useSetLocaleCookie } from "@vitarish/localization";
import { FaIcon, MarketingActionLink } from "@vitarish/ui";
import { LandingRoutes } from "../../app/routes";
import { catalogs } from "../../localization/messages";

export function PurposePage() {
  const { locale } = useLocalizedText();
  const setLocaleCookie = useSetLocaleCookie();
  const otherLocale = supportedLocales.find((candidate) => candidate !== locale);
  const text = catalogs[locale].landing;
  const homeLink = { href: LandingRoutes.Home, label: text.editorial.home };
  const darmanLink = { href: LandingRoutes.Darman, label: text.apps.practice.discover };
  const links = [homeLink, darmanLink];

  return (
    <EditorialContentPage
      brand={text.brand.name}
      homeHref={LandingRoutes.Home}
      homeLabel={text.editorial.home}
      navigationLabel={text.editorial.navigation}
      navigationTooltip={text.editorial.navigationTooltip}
      navigationLinks={[homeLink]}
      navigationGroups={[{ label: text.editorial.navigation, tooltip: text.editorial.navigationTooltip, links: [darmanLink] }]}
      headerAction={otherLocale && (
        <MarketingActionLink action={{
          label: text.header.language,
          tooltip: text.header.languageTooltip,
          tone: "quiet",
          icon: <FaIcon aria-hidden="true" icon="language" />,
          onPress: () => { setLocaleCookie(otherLocale); window.location.reload(); },
        }} />
      )}
      title={text.purpose.title}
      metadata={[
        { label: text.editorial.by, value: text.brand.name },
        { label: text.editorial.category, value: text.editorial.categoryValue },
      ]}
      footerDescription={text.hero.lead}
      footerNavigationLabel={text.editorial.navigation}
      footerLinks={links}
    >
      <p>{text.purpose.description}</p>
      <h2>{text.apps.title}</h2>
      <p>{text.apps.intro}</p>
      <p>{text.apps.practice.description}</p>
      <p>{text.apps.practice.note}</p>
      <p><Link href={LandingRoutes.Darman}>{text.apps.practice.discover}</Link></p>
    </EditorialContentPage>
  );
}
