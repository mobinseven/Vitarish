"use client";

import { ItemCard, ItemCardGroup, PressableFeedback } from "@heroui-pro/react";
import { ImageActionCard } from "@vitarish/ui";
import { useLocalizedText } from "@/localization";

export function HomeDashboard() {
  const { t } = useLocalizedText();
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
        <ImageActionCard
          key={token}
          imageSrc={backgroundImage}
          label={label}
        />
      ))}
    </ItemCardGroup>
  );
}
