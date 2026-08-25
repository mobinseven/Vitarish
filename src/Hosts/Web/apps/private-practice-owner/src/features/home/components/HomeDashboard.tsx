"use client";

import { Button } from "@heroui/react";
import { HoverCard, ItemCardGroup } from "@heroui-pro/react";
import { ImageActionCard } from "@vitarish/ui";
import { useState } from "react";
import { useLocalizedText } from "@/localization";

export function HomeDashboard() {
  const { t } = useLocalizedText();
  const [openCardToken, setOpenCardToken] = useState<string | null>(null);
  const accentCards = [
    {
      label: t("home.stubInfo"),
      token: "info",
      backgroundImage: "/card-red.webp",
    },
    {
      label: t("home.stubHighlight"),
      token: "highlight",
      backgroundImage: "/card-blue.webp",
    },
    {
      label: t("home.stubSupport"),
      token: "support",
      backgroundImage: "/card-green.webp",
    },
    {
      label: t("home.stubEmphasis"),
      token: "emphasis",
      backgroundImage: "/card-purple.webp",
    },
  ] as const;

  return (
    <ItemCardGroup layout="grid" columns={3} variant="transparent">
      {accentCards.map(({ label, token, backgroundImage }) => (
        <HoverCard
          key={token}
          closeDelay={1000}
          open={openCardToken === token}
          openDelay={0}
          onOpenChange={(isOpen) => {
            setOpenCardToken((currentToken) => {
              if (isOpen) {
                return token;
              }

              return currentToken === token ? null : currentToken;
            });
          }}
        >
          <HoverCard.Trigger className="block w-full">
            <ImageActionCard
              className="w-full"
              imageSrc={backgroundImage}
              label={label}
            />
          </HoverCard.Trigger>
          <HoverCard.Content placement="bottom">
            <HoverCard.Arrow />
            <p className="text-sm font-semibold">{label}</p>
            <p className="text-muted mt-1 max-w-64 text-sm leading-relaxed">
              {t("home.dashboardDescription")}
            </p>
            <div className="mt-3 flex flex-wrap gap-2">
              <Button size="sm">{t("home.stubPrimary")}</Button>
              <Button size="sm" variant="secondary">
                {t("home.stubSecondary")}
              </Button>
              <Button size="sm" variant="tertiary">
                {t("home.stubTertiary")}
              </Button>
            </div>
          </HoverCard.Content>
        </HoverCard>
      ))}
    </ItemCardGroup>
  );
}
