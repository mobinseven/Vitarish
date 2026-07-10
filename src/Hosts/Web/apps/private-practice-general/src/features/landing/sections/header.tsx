import {Button} from "@heroui/react";
import {Navbar} from "@heroui-pro/react";

import {BrandLogotipo} from "@/features/landing/brand";
import {FaIcon} from "@/features/landing/icons/fa-icon";
import {useLocalizedText} from "@/localization";
import {Routes} from "@/app/routes";
import {ThemeToggle} from "./theme-toggle";

const navItems = [
  {href: "#pillars", key: "landing.header.navPlatform" as const},
  {href: "#architecture", key: "landing.header.navArchitecture" as const},
  {href: "#features", key: "landing.header.navFeatures" as const},
  {href: "#pricing", key: "landing.header.navPricing" as const},
];

export function Header() {
  const {t} = useLocalizedText();

  return (
    <Navbar aria-label={t("landing.header.primaryNav")} height="4.5rem">
      <Navbar.Header className="mx-auto w-full max-w-[1200px]">
        <Navbar.Brand className="flex items-center gap-2">
          <BrandLogotipo className="h-7 w-auto" />
        </Navbar.Brand>

        <Navbar.Spacer />

        <Navbar.Content className="hidden md:flex">
          {navItems.map((item) => (
            <Navbar.Item key={item.href} href={item.href} className="text-[0.7rem]">
              {t(item.key)}
            </Navbar.Item>
          ))}
        </Navbar.Content>

        <Navbar.Spacer />

        <Navbar.Content className="gap-2">
          <ThemeToggle />
          <Button
            className="hidden md:inline-flex"
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            render={(props: any) => <a href={Routes.Login.Index} {...props} />}
            size="sm"
            variant="primary"
          >
            {t("landing.header.ctaStartBuilding")}
            <FaIcon icon="arrow-right" className="size-4" />
          </Button>
          <Button
            className="hidden md:inline-flex"
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            render={(props: any) => <a href={Routes.Login.Index} {...props} />}
            size="sm"
            variant="ghost"
          >
            {t("landing.header.loginAction")}
          </Button>
          <Navbar.MenuToggle className="md:hidden" />
        </Navbar.Content>
      </Navbar.Header>

      <Navbar.Menu>
        {navItems.map((item) => (
          <Navbar.MenuItem key={item.href} href={item.href} className="text-sm">
            {t(item.key)}
          </Navbar.MenuItem>
        ))}
        <Button
          className="mt-3"
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          render={(props: any) => <a href={Routes.Login.Index} {...props} />}
          variant="primary"
        >
          {t("landing.header.ctaStartBuilding")}
        </Button>
        <Button
          className="mt-2"
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          render={(props: any) => <a href={Routes.Login.Index} {...props} />}
          variant="ghost"
        >
          {t("landing.header.loginAction")}
        </Button>
      </Navbar.Menu>
    </Navbar>
  );
}
