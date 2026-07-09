"use client";

import { Description, Label, Switch } from "@heroui/react";
import { RadioButtonGroup } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";
import { accountThemeOptions, type AccountThemeValue } from "./themeOptions";

interface PaletteSwitchProps {
  selected: AccountThemeValue;
  isVibrant: boolean;
  onSelect: (value: AccountThemeValue) => void;
  onVibrantChange: (value: boolean) => void;
}

export function PaletteSwitch({
  selected,
  isVibrant,
  onSelect,
  onVibrantChange,
}: PaletteSwitchProps) {
  const { t } = useLocalizedText();

  return (
    <div className="flex w-full max-w-3xl flex-col gap-5">
      <RadioButtonGroup
        className="w-full grid-cols-1 sm:grid-cols-2"
        layout="grid"
        name="account-theme-palette"
        value={selected}
        variant="secondary"
        onChange={(value) => onSelect(value as AccountThemeValue)}
      >
        <Label className="col-span-full text-2xl font-bold">{t("account.theme.title")}</Label>
        <Description className="col-span-full">{t("account.theme.description")}</Description>
        {accountThemeOptions.map((option) => (
          <RadioButtonGroup.Item key={option.value} value={option.value}>
            <RadioButtonGroup.Indicator />
            <RadioButtonGroup.ItemContent>
              <div className="flex items-start justify-between gap-4">
                <div className="flex min-w-0 flex-col gap-1">
                  <Label className="text-sm font-semibold">{t(option.labelKey)}</Label>
                  <Description>{t(option.descriptionKey)}</Description>
                </div>
                <div className="flex shrink-0 items-center gap-1" aria-hidden="true">
                  {option.swatches.map((color) => (
                    <span
                      key={color}
                      className="size-4 rounded-full border border-border"
                      style={{ backgroundColor: color }}
                    />
                  ))}
                </div>
              </div>
            </RadioButtonGroup.ItemContent>
          </RadioButtonGroup.Item>
        ))}
      </RadioButtonGroup>

      <Switch isSelected={isVibrant} onChange={onVibrantChange}>
        <Switch.Content className="text-sm">
          <Switch.Control>
            <Switch.Thumb />
          </Switch.Control>
          <span className="flex min-w-0 flex-col gap-1">
            <span className="font-medium text-foreground">{t("account.theme.vibrantPalette")}</span>
            <span className="text-muted">{t("account.theme.vibrantPaletteDescription")}</span>
          </span>
        </Switch.Content>
      </Switch>
    </div>
  );
}
