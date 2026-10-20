"use client";

import { Alert, Button, Input, Label, Separator, TextField, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { BusinessLocationPicker } from "./BusinessLocationPicker";
import { WorkingHoursEditor } from "./WorkingHoursEditor";
import { maximumPhones, maximumSocialLinks, type BusinessDetailsForm } from "./businessDetailsForm";

interface BusinessDetailsFieldsProps {
  value: BusinessDetailsForm;
  onChange: (value: BusinessDetailsForm) => void;
}

function GroupHeading({ icon, title, description }: { icon: string; title: string; description?: string }) {
  return (
    <div className="flex items-start gap-3">
      <FaIcon icon={icon} size="lg" aria-hidden="true" className="mt-1 text-accent" />
      <div className="flex flex-col gap-1">
        <h3 className="text-base font-semibold text-foreground">{title}</h3>
        {description ? <p className="text-sm text-muted">{description}</p> : null}
      </div>
    </div>
  );
}

interface ListFieldProps {
  values: string[];
  maximum: number;
  label: string;
  placeholder: string;
  addLabel: string;
  removeLabel: string;
  type: "tel" | "url";
  onChange: (values: string[]) => void;
}

/** A short list of text entries with add and remove, such as phone numbers. */
function ListField({ values, maximum, label, placeholder, addLabel, removeLabel, type, onChange }: ListFieldProps) {
  return (
    <div className="flex flex-col gap-2">
      {values.map((value, index) => (
        <div key={index} className="flex items-end gap-2">
          <TextField className="flex-1" type={type}>
            <Label className={index === 0 ? undefined : "sr-only"}>{label}</Label>
            <Input
              dir="ltr"
              placeholder={placeholder}
              value={value}
              onChange={(event) => onChange(values.map((item, itemIndex) => (itemIndex === index ? event.target.value : item)))}
            />
          </TextField>
          {values.length > 1 ? (
            <Tooltip>
              <Button
                isIconOnly
                variant="tertiary"
                aria-label={removeLabel}
                onPress={() => onChange(values.filter((_, itemIndex) => itemIndex !== index))}
              >
                <FaIcon icon="trash-can" aria-hidden="true" />
              </Button>
              <Tooltip.Content>{removeLabel}</Tooltip.Content>
            </Tooltip>
          ) : null}
        </div>
      ))}
      {values.length < maximum ? (
        <Tooltip>
          <Button variant="tertiary" className="self-start" onPress={() => onChange([...values, ""])}>
            <FaIcon icon="plus" aria-hidden="true" />
            {addLabel}
          </Button>
          <Tooltip.Content>{addLabel}</Tooltip.Content>
        </Tooltip>
      ) : null}
    </div>
  );
}

export function BusinessDetailsFields({ value, onChange }: BusinessDetailsFieldsProps) {
  const { t } = useLocalizedText();
  const set = <TKey extends keyof BusinessDetailsForm>(key: TKey, next: BusinessDetailsForm[TKey]) =>
    onChange({ ...value, [key]: next });

  return (
    <div className="flex flex-col gap-6">
      <Alert status="default">
        <Alert.Indicator />
        <Alert.Content>
          <Alert.Description>{t("journey.setup.details.publicNotice")}</Alert.Description>
        </Alert.Content>
      </Alert>

      <section className="flex flex-col gap-4" aria-label={t("journey.setup.details.addressTitle")}>
        <GroupHeading icon="location-dot" title={t("journey.setup.details.addressTitle")} />
        <div className="grid gap-4 sm:grid-cols-2">
          <TextField value={value.province} onChange={(next) => set("province", next)}>
            <Label>{t("journey.setup.details.province")}</Label>
            <Input autoComplete="address-level1" />
          </TextField>
          <TextField value={value.city} onChange={(next) => set("city", next)}>
            <Label>{t("journey.setup.details.city")}</Label>
            <Input autoComplete="address-level2" />
          </TextField>
          <TextField className="sm:col-span-2" value={value.street} onChange={(next) => set("street", next)}>
            <Label>{t("journey.setup.details.street")}</Label>
            <Input autoComplete="street-address" />
          </TextField>
          <TextField value={value.postalCode} onChange={(next) => set("postalCode", next)}>
            <Label>{t("journey.setup.details.postalCode")}</Label>
            <Input dir="ltr" inputMode="numeric" autoComplete="postal-code" />
          </TextField>
        </div>
        <BusinessLocationPicker location={value.location} onChange={(next) => set("location", next)} />
      </section>

      <Separator />

      <section className="flex flex-col gap-4" aria-label={t("journey.setup.details.contactTitle")}>
        <GroupHeading icon="address-book" title={t("journey.setup.details.contactTitle")} />
        <ListField
          values={value.phones}
          maximum={maximumPhones}
          type="tel"
          label={t("journey.setup.details.phone")}
          placeholder={t("journey.setup.details.phonePlaceholder")}
          addLabel={t("journey.setup.details.addPhone")}
          removeLabel={t("journey.setup.details.removePhone")}
          onChange={(next) => set("phones", next)}
        />
        <TextField type="url" value={value.website} onChange={(next) => set("website", next)}>
          <Label>{t("journey.setup.details.website")}</Label>
          <Input dir="ltr" placeholder={t("journey.setup.details.websitePlaceholder")} />
        </TextField>
        <ListField
          values={value.socialLinks}
          maximum={maximumSocialLinks}
          type="url"
          label={t("journey.setup.details.socialLink")}
          placeholder={t("journey.setup.details.socialLinkPlaceholder")}
          addLabel={t("journey.setup.details.addSocialLink")}
          removeLabel={t("journey.setup.details.removeSocialLink")}
          onChange={(next) => set("socialLinks", next)}
        />
      </section>

      <Separator />

      <section className="flex flex-col gap-4" aria-label={t("journey.setup.details.hoursTitle")}>
        <GroupHeading
          icon="clock"
          title={t("journey.setup.details.hoursTitle")}
          description={t("journey.setup.details.hoursDescription")}
        />
        <WorkingHoursEditor days={value.workingDays} onChange={(next) => set("workingDays", next)} />
      </section>
    </div>
  );
}
