"use client";

import { Button, Chip, Surface, Tooltip } from "@heroui/react";
import { Sheet } from "@heroui-pro/react";
import { formatList, useLocalizedText } from "@vitarish/localization";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import type { LandingCatalog } from "../use-landing-catalog";
import { availabilityTone } from "../use-landing-catalog";
import { matchedFeatures, type ConsultationQuestionKey, type ConsultationState } from "./consultation-state";
import styles from "../front-door.module.css";

const answeredRows: readonly ConsultationQuestionKey[] = ["kind", "booking"];

/** What the consultation has learned about the owner's clinic so far, filled in as they answer. */
function ClinicSummary({ catalog, state }: { catalog: LandingCatalog; state: ConsultationState }) {
  const { t, locale } = useLocalizedText();
  const features = matchedFeatures(catalog.consultation, state.answers.pains);
  const availability = new Map(catalog.features.map((feature) => [feature.key, feature.availability]));

  return (
    <div className={styles.panelBody}>
      {answeredRows.map((question) => {
        const answers = state.answers[question];
        return (
          <div key={question} className={styles.panelRow}>
            <span className={styles.panelLabel}>{t(`landing.consultation.panel.${question}`)}</span>
            {answers.length > 0 ? (
              <span>{formatList(answers.map((option) => t(`landing.consultation.options.${question}.${option}`)), locale)}</span>
            ) : (
              <span className={styles.panelEmpty}>{t("landing.consultation.panel.empty")}</span>
            )}
          </div>
        );
      })}
      <div className={styles.panelRow}>
        <span className={styles.panelLabel}>{t("landing.consultation.panel.startWith")}</span>
        {features.length > 0 ? (
          <ul className={styles.panelFeatures}>
            {features.map((key) => {
              const status = availability.get(key);
              return (
                <li key={key}>
                  <span>{t(`landing.features.${key}.title`)}</span>
                  {status ? (
                    <Chip color={availabilityTone(status)} size="sm" variant="soft">{t(`landing.availability.${status}`)}</Chip>
                  ) : null}
                </li>
              );
            })}
          </ul>
        ) : (
          <span className={styles.panelEmpty}>{t("landing.consultation.panel.featuresEmpty")}</span>
        )}
      </div>
      {state.step === "open" && state.answers.kind[0] ? (
        <Surface className={styles.clinicCard} variant="secondary">
          <Chip color="success" size="sm" variant="soft">{t("landing.consultation.readyCard")}</Chip>
          <span className={styles.clinicCardName}>
            {t("landing.consultation.cardName", { kind: t(`landing.consultation.options.kind.${state.answers.kind[0]}`) })}
          </span>
          {features[0] ? (
            <span className={styles.panelLabel}>
              {t("landing.consultation.cardMeta", { feature: t(`landing.features.${features[0]}.title`) })}
            </span>
          ) : null}
        </Surface>
      ) : null}
    </div>
  );
}

export function ClinicPanel({ catalog, state }: { catalog: LandingCatalog; state: ConsultationState }) {
  const { t } = useLocalizedText();

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={(props) => <aside {...props} aria-label={t("landing.consultation.panel.title")} />}>
      <div className="flex items-center gap-3">
        <HeaderIcon icon="hospital" />
        <h2 className="text-lg font-semibold text-foreground">{t("landing.consultation.panel.title")}</h2>
      </div>
      <ClinicSummary catalog={catalog} state={state} />
    </Surface>
  );
}

/** On narrow screens the panel opens from the bottom so the conversation keeps the screen. */
export function ClinicPanelSheet({ catalog, state }: { catalog: LandingCatalog; state: ConsultationState }) {
  const { t } = useLocalizedText();

  return (
    <Sheet>
      <Tooltip>
        <Sheet.Trigger>
          <Button size="sm" variant="secondary">
            <FaIcon aria-hidden="true" icon="hospital" />
            {t("landing.consultation.panel.open")}
          </Button>
        </Sheet.Trigger>
        <Tooltip.Content>{t("landing.consultation.panel.openTooltip")}</Tooltip.Content>
      </Tooltip>
      <Sheet.Backdrop>
        <Sheet.Content className={styles.sheet}>
          <Sheet.Dialog>
            <Sheet.Handle />
            <Sheet.Header>
              <Sheet.Heading>{t("landing.consultation.panel.title")}</Sheet.Heading>
            </Sheet.Header>
            <Sheet.Body>
              <ClinicSummary catalog={catalog} state={state} />
            </Sheet.Body>
          </Sheet.Dialog>
        </Sheet.Content>
      </Sheet.Backdrop>
    </Sheet>
  );
}
