"use client";

import { useRef } from "react";
// The subpath keeps the rest of the Pro library out of this page.
import { Stepper } from "@heroui-pro/react/stepper";
import { useLocalizedText } from "@vitarish/localization";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";
import { TenantAddressStep } from "../features/tenantSetup/TenantAddressStep";
import { TenantProfileStep } from "../features/tenantSetup/TenantProfileStep";
import { TenantReadyPanel } from "../features/tenantSetup/TenantReadyPanel";
import { useTenantSetup } from "../features/tenantSetup/useTenantSetup";

interface TenantSetupPageProps {
  /** Where the hosting app keeps each business category's label: `${prefix}.${code}`. */
  categoryMessagePrefix: string;
  /** The app route on the new clinic that redeems the one-time sign-in token. */
  handoffPath: string;
  /** The app route on the new clinic for signing in when no token was issued. */
  signInPath: string;
}

/** Creates the signed-in user's own clinic in two steps, then hands them over to it. */
export function TenantSetupPage({ categoryMessagePrefix, handoffPath, signInPath }: TenantSetupPageProps) {
  const { t, locale } = useLocalizedText();
  const setup = useTenantSetup(locale);
  // The suffix is learned from the first check that names an address, then kept while typing.
  const addressSuffix = useRef<string | null>(null);
  const subdomain = setup.values.subdomain.trim().toLowerCase();
  if (setup.hostname && setup.hostname.startsWith(`${subdomain}.`)) {
    addressSuffix.current = setup.hostname.slice(subdomain.length);
  }

  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            eyebrow={t("journey.setup.eyebrow")}
            title={setup.created ? t("journey.setup.ready.title") : t("journey.setup.title")}
            description={setup.created ? undefined : t("journey.setup.description")}
          />
        }
      >
        {setup.created ? (
          <TenantReadyPanel tenant={setup.created} handoffPath={handoffPath} signInPath={signInPath} />
        ) : (
          <div className="flex flex-col gap-6">
            <Stepper currentStep={setup.step} aria-label={t("journey.setup.stepsLabel")}>
              {[t("journey.setup.steps.address"), t("journey.setup.steps.profile")].map((title) => (
                <Stepper.Step key={title}>
                  <Stepper.Indicator />
                  <Stepper.Content>
                    <Stepper.Title>{title}</Stepper.Title>
                  </Stepper.Content>
                  <Stepper.Separator />
                </Stepper.Step>
              ))}
            </Stepper>
            {setup.step === 0 ? (
              <TenantAddressStep
                name={setup.values.name}
                subdomain={setup.values.subdomain}
                addressSuffix={addressSuffix.current}
                availabilityStatus={setup.availabilityStatus}
                isChecking={setup.isCheckingAvailability}
                checkFailed={setup.availabilityFailed}
                canContinue={setup.canContinue}
                onNameChange={(name) => setup.setField("name", name)}
                onSubdomainChange={(next) => setup.setField("subdomain", next)}
                onContinue={() => setup.setStep(1)}
              />
            ) : (
              <TenantProfileStep
                categories={setup.categories}
                categoryMessagePrefix={categoryMessagePrefix}
                category={setup.values.category}
                description={setup.values.description}
                businessDetails={setup.values.businessDetails}
                failure={setup.failure}
                isCreating={setup.isCreating}
                onCategoryChange={(category) => setup.setField("category", category)}
                onDescriptionChange={(description) => setup.setField("description", description)}
                onBusinessDetailsChange={(details) => setup.setField("businessDetails", details)}
                onBack={() => setup.setStep(0)}
                onCreate={() => void setup.create()}
              />
            )}
          </div>
        )}
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}
