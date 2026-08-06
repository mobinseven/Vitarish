import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
  FeatureItemCard,
  FeatureItemGroup,
  HeroHeader,
  MarketingActionLink,
  PricingCard,
  PricingGrid,
} from "../marketing";

describe("shared marketing components", () => {
  it("composes a hero from caller-owned slots and supports action links", () => {
    render(
      <HeroHeader aria-label="Product introduction">
        <HeroHeader.Content>
          <HeroHeader.Eyebrow>For practices</HeroHeader.Eyebrow>
          <HeroHeader.Title>One clear operating view.</HeroHeader.Title>
          <HeroHeader.Description>Keep the next meaningful action visible.</HeroHeader.Description>
          <HeroHeader.Actions>
            <MarketingActionLink action={{ href: "/start", label: "Get started" }} />
          </HeroHeader.Actions>
        </HeroHeader.Content>
        <HeroHeader.Visual><div>Product preview</div></HeroHeader.Visual>
      </HeroHeader>,
    );

    expect(screen.getByRole("heading", { name: "One clear operating view." })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Get started" })).toHaveAttribute("href", "/start");
    expect(screen.getByText("Product preview")).toBeInTheDocument();
  });

  it("renders feature cards through the ItemCard compound contract", () => {
    render(
      <FeatureItemGroup layout="grid" columns={2}>
        <FeatureItemCard>
          <FeatureItemCard.Icon><span aria-hidden="true">*</span></FeatureItemCard.Icon>
          <FeatureItemCard.Content>
            <FeatureItemCard.Title>Patient context</FeatureItemCard.Title>
            <FeatureItemCard.Description>Keep relevant history together.</FeatureItemCard.Description>
          </FeatureItemCard.Content>
        </FeatureItemCard>
      </FeatureItemGroup>,
    );

    expect(screen.getByText("Patient context")).toBeInTheDocument();
    expect(screen.getByText("Keep relevant history together.")).toBeInTheDocument();
  });

  it("renders pricing states and keeps CTA behavior with the caller", () => {
    const onPress = vi.fn();

    render(
      <PricingGrid>
        <PricingCard>
          <PricingCard.Header>
            <PricingCard.Badge>Recommended</PricingCard.Badge>
            <PricingCard.Title>Practice</PricingCard.Title>
            <PricingCard.Description>For a growing practice.</PricingCard.Description>
            <PricingCard.Price>$99</PricingCard.Price>
            <PricingCard.Billing>per month</PricingCard.Billing>
          </PricingCard.Header>
          <PricingCard.Content>
            <PricingCard.Features>
              <PricingCard.Feature>Shared patient context</PricingCard.Feature>
            </PricingCard.Features>
          </PricingCard.Content>
          <PricingCard.Footer>
            <MarketingActionLink action={{ label: "Choose Practice", onPress }} />
          </PricingCard.Footer>
        </PricingCard>
      </PricingGrid>,
    );

    expect(screen.getByText("Recommended")).toBeInTheDocument();
    expect(screen.getByText("Shared patient context")).toBeInTheDocument();
    screen.getByRole("button", { name: "Choose Practice" }).click();
    expect(onPress).toHaveBeenCalledTimes(1);
  });
});
