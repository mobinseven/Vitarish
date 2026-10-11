"use client";

import { Button, Disclosure, DisclosureGroup } from "@heroui/react";
import { ViewportReveal } from "@vitarish/ui";
import { cn } from "tailwind-variants";
import { useLocalizedText } from "@/localization";
import * as React from "react";

const principleIds = ["gather", "understand", "coordinate"] as const;
type PrincipleId = (typeof principleIds)[number];

export function PracticePrinciplesShowcase() {
  const { t } = useLocalizedText();
  const [expandedKeys, setExpandedKeys] = React.useState<Set<string | number>>(
    new Set<PrincipleId>(["gather"]),
  );

  const principles = principleIds.map((id) => ({
    id,
    label: t(`practiceLanding.${id}`),
    description: t(`practiceLanding.${id}Description`),
  }));
  const activeId = Array.from(expandedKeys)[0] as PrincipleId | undefined;
  const activePrinciple = principles.find((principle) => principle.id === activeId);

  function handleExpandedChange(keys: Set<string | number>) {
    const nextKey = Array.from(keys).at(-1);
    setExpandedKeys(nextKey ? new Set([nextKey]) : new Set());
  }

  return (
    <ViewportReveal
      as="section"
      aria-labelledby="principles-heading"
      className="bg-background px-6 py-24 sm:px-10 lg:px-16 lg:py-32"
    >
      <div className="mx-auto grid max-w-7xl gap-12 lg:grid-cols-[minmax(0,0.8fr)_minmax(0,1.2fr)] lg:items-center lg:gap-20">
        <div>
          <p className="text-xs font-medium uppercase tracking-[0.24em] text-foreground/50">
            {t("practiceLanding.showcaseSectionLabel")}
          </p>
          <h2
            id="principles-heading"
            className="mt-4 max-w-lg text-3xl font-medium tracking-[-0.03em] text-foreground sm:text-4xl"
          >
            {t("practiceLanding.showcaseTitle")}
          </h2>

          <DisclosureGroup
            className="mt-10 flex flex-col gap-y-3"
            expandedKeys={expandedKeys}
            onExpandedChange={handleExpandedChange}
          >
            {principles.map((principle) => {
              const isSelected = expandedKeys.has(principle.id);

              return (
                <Disclosure key={principle.id} id={principle.id}>
                  <Disclosure.Heading>
                    <Button
                      slot="trigger"
                      variant="tertiary"
                      className={cn(
                        "h-14 w-full justify-start rounded-full px-5 text-start transition-colors duration-300",
                        isSelected && "bg-foreground text-background",
                      )}
                    >
                      <span className="flex w-full items-center gap-3">
                        <span
                          aria-hidden="true"
                          className={cn(
                            "flex size-6 items-center justify-center rounded-full border border-current",
                            isSelected && "border-current",
                          )}
                        >
                          <i
                            className={cn(
                              "fa-light text-xs",
                              isSelected ? "fa-minus" : "fa-plus",
                            )}
                          />
                        </span>
                        <span>{principle.label}</span>
                      </span>
                    </Button>
                  </Disclosure.Heading>
                  <Disclosure.Content>
                    <Disclosure.Body
                      data-expanded={isSelected}
                      className={cn(
                        "mt-3 rounded-3xl bg-foreground/[0.06] p-7 text-start text-foreground/70",
                        "translate-y-2 opacity-0 transition-[opacity,transform] duration-300 ease-out",
                        "data-[expanded=true]:translate-y-0 data-[expanded=true]:opacity-100",
                        "motion-reduce:translate-y-0 motion-reduce:transition-opacity motion-reduce:duration-150",
                      )}
                    >
                      <p className="max-w-md text-base leading-7">
                        {principle.description}
                      </p>
                    </Disclosure.Body>
                  </Disclosure.Content>
                </Disclosure>
              );
            })}
          </DisclosureGroup>
        </div>

        <div
          aria-live="polite"
          aria-label={t("practiceLanding.showcasePlaceholderLabel")}
          className="relative aspect-[4/3] overflow-hidden rounded-[2rem] bg-foreground/[0.06] p-4 shadow-2xl"
        >
          {principles.map((principle) => {
            const isSelected = principle.id === activeId;

            return (
              <div
                key={principle.id}
                aria-hidden={!isSelected}
                data-selected={isSelected}
                className={cn(
                  "absolute inset-4 rounded-[1.5rem] border border-foreground/10 bg-background/80 p-5 backdrop-blur-xl",
                  "translate-y-2 opacity-0 transition-[opacity,transform] duration-500 ease-out",
                  "data-[selected=true]:translate-y-0 data-[selected=true]:opacity-100",
                  "motion-reduce:translate-y-0 motion-reduce:transition-opacity motion-reduce:duration-150",
                )}
              >
                <p className="text-xs uppercase tracking-[0.24em] text-foreground/50">
                  {principle.label}
                </p>
                <div className="mt-8 space-y-3" aria-hidden="true">
                  <div className="h-3 w-2/5 rounded-full bg-foreground/10" />
                  <div className="h-20 rounded-2xl bg-foreground/[0.06]" />
                  <div className="grid grid-cols-2 gap-3">
                    <div className="h-24 rounded-2xl bg-foreground/[0.06]" />
                    <div className="h-24 rounded-2xl bg-foreground/[0.06]" />
                  </div>
                </div>
              </div>
            );
          })}
          {!activePrinciple ? (
            <p className="absolute inset-0 flex items-center justify-center text-sm text-foreground/50">
              {t("practiceLanding.showcasePlaceholderLabel")}
            </p>
          ) : null}
        </div>
      </div>
    </ViewportReveal>
  );
}
