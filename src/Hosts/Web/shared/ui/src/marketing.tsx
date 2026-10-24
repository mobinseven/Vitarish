import type { ComponentProps, HTMLAttributes, ReactNode } from "react";
import { Button, Card, Link, Tooltip } from "@heroui/react";
import { ItemCard, ItemCardGroup, Navbar } from "@heroui-pro/react";
import { Carousel as HeroCarousel } from "@heroui-pro/react/carousel";
import { PageContainer } from "./layout";

const cx = (...values: Array<string | undefined | false>) => values.filter(Boolean).join(" ");

export type MarketingLink = { label: ReactNode; href: string; external?: boolean };
export type MarketingAction = {
  label: ReactNode;
  href?: string;
  external?: boolean;
  tone?: "primary" | "secondary" | "outline" | "quiet";
  onPress?: () => void;
  isDisabled?: boolean;
  isPending?: boolean;
  tooltip?: ReactNode;
  icon?: ReactNode;
};

const actionLayoutClasses = "inline-flex min-h-11 items-center justify-center rounded-xl px-5 py-3 text-sm font-semibold no-underline transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

// Links have no HeroUI variant, so their colors follow the tone here.
const linkClasses = (tone: MarketingAction["tone"]) => cx(
  actionLayoutClasses,
  tone === "primary" && "bg-accent text-accent-foreground hover:opacity-90",
  tone === "secondary" && "bg-surface-secondary text-foreground hover:bg-surface-tertiary",
  tone === "outline" && "border border-default bg-transparent text-foreground hover:bg-surface-secondary",
  tone === "quiet" && "text-foreground hover:bg-surface-secondary",
);

// Buttons take their colors from the matching HeroUI variant, which pairs each fill with its foreground.
const buttonVariants = {
  primary: "primary",
  secondary: "secondary",
  outline: "outline",
  quiet: "ghost",
} as const satisfies Record<NonNullable<MarketingAction["tone"]>, ComponentProps<typeof Button>["variant"]>;

export function MarketingActionLink({
  action,
  className,
  fullWidth = false,
}: { action: MarketingAction; className?: string; fullWidth?: boolean }) {
  const tone = action.tone ?? "primary";
  const sizing = cx(fullWidth && "w-full", className);

  const content = <><span>{action.label}</span>{action.icon}</>;

  if (action.href) {
    return (
      <Tooltip>
        <Link
          href={action.isDisabled ? undefined : action.href}
          target={action.external ? "_blank" : undefined}
          rel={action.external ? "noreferrer" : undefined}
          aria-disabled={action.isDisabled || undefined}
          className={cx(linkClasses(tone), sizing)}
          onPress={action.isDisabled ? undefined : action.onPress}
        >
          {content}
        </Link>
        <Tooltip.Content>{action.tooltip ?? action.label}</Tooltip.Content>
      </Tooltip>
    );
  }

  return (
    <Tooltip>
      <Button
        className={cx(actionLayoutClasses, sizing)}
        isDisabled={action.isDisabled}
        isPending={action.isPending}
        variant={buttonVariants[tone]}
        onPress={action.onPress}
      >
        {content}
      </Button>
      <Tooltip.Content>{action.tooltip ?? action.label}</Tooltip.Content>
    </Tooltip>
  );
}

export function MarketingHeader({
  brand,
  navigation = [],
  actions = [],
  navigationLabel,
  className,
}: {
  brand: ReactNode;
  navigation?: MarketingLink[];
  actions?: MarketingAction[];
  navigationLabel?: string;
  className?: string;
}) {
  return (
    <Navbar aria-label={navigationLabel} className={className}>
      <Navbar.Header>
        <PageContainer className="flex h-full items-center">
          <Navbar.Brand>{brand}</Navbar.Brand>
          <Navbar.Spacer />
          <Navbar.Content className="hidden gap-5 lg:flex">
            {navigation.map((item) => (
              <Navbar.Item key={item.href} href={item.href} target={item.external ? "_blank" : undefined} rel={item.external ? "noreferrer" : undefined}>{item.label}</Navbar.Item>
            ))}
          </Navbar.Content>
          <Navbar.Spacer />
          <Navbar.Content className="hidden gap-2 sm:flex">
            {actions.map((action, index) => (
              <MarketingActionLink
                key={`${action.href ?? "action"}-${index}`}
                action={action}
                className="min-h-9 px-4 py-2"
              />
            ))}
          </Navbar.Content>
          <Navbar.MenuToggle className="lg:hidden" />
        </PageContainer>
      </Navbar.Header>
      <Navbar.Menu>
        {navigation.map((item) => (
          <Navbar.MenuItem key={item.href} href={item.href} target={item.external ? "_blank" : undefined} rel={item.external ? "noreferrer" : undefined}>{item.label}</Navbar.MenuItem>
        ))}
        {actions.map((action, index) => (
          <Navbar.MenuItem key={`${action.href ?? "action"}-${index}`}>
            <MarketingActionLink action={action} fullWidth />
          </Navbar.MenuItem>
        ))}
      </Navbar.Menu>
    </Navbar>
  );
}

interface HeroHeaderRootProps extends HTMLAttributes<HTMLElement> {
  children: ReactNode;
  variant?: "centered" | "split";
}

function HeroHeaderRoot({ children, className, variant = "centered", ...props }: HeroHeaderRootProps) {
  return (
    <section
      {...props}
      className={cx("py-16 sm:py-24", className)}
      data-marketing-component="hero-header"
      data-variant={variant}
    >
      <PageContainer
        className={cx(
          "grid items-center gap-12",
          variant === "split" && "lg:grid-cols-[minmax(0,1fr)_minmax(0,0.9fr)]",
        )}
      >
        {children}
      </PageContainer>
    </section>
  );
}

function HeroHeaderContent({ children, className, ...props }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return <div {...props} className={cx("min-w-0", className)}>{children}</div>;
}

function HeroHeaderEyebrow({ children, className, ...props }: HTMLAttributes<HTMLParagraphElement> & { children: ReactNode }) {
  return <p {...props} className={cx("text-sm font-semibold tracking-[0.12em] text-accent", className)}>{children}</p>;
}

function HeroHeaderTitle({ children, className, ...props }: HTMLAttributes<HTMLHeadingElement> & { children: ReactNode }) {
  return <h1 {...props} className={cx("mt-4 max-w-4xl text-balance text-4xl font-semibold tracking-[-0.03em] sm:text-6xl", className)}>{children}</h1>;
}

function HeroHeaderDescription({ children, className, ...props }: HTMLAttributes<HTMLParagraphElement> & { children: ReactNode }) {
  return <p {...props} className={cx("mt-6 max-w-2xl text-lg leading-relaxed text-muted", className)}>{children}</p>;
}

function HeroHeaderActions({ children, className, ...props }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return <div {...props} className={cx("mt-8 flex flex-wrap gap-3", className)}>{children}</div>;
}

function HeroHeaderVisual({ children, className, ...props }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return <div {...props} className={cx("min-w-0", className)}>{children}</div>;
}

export const HeroHeader = Object.assign(HeroHeaderRoot, {
  Content: HeroHeaderContent,
  Eyebrow: HeroHeaderEyebrow,
  Title: HeroHeaderTitle,
  Description: HeroHeaderDescription,
  Actions: HeroHeaderActions,
  Visual: HeroHeaderVisual,
});

export function MarketingHero({
  eyebrow,
  title,
  description,
  actions = [],
  visual,
  variant = "split",
  className,
}: {
  eyebrow?: ReactNode;
  title: ReactNode;
  description?: ReactNode;
  actions?: MarketingAction[];
  visual?: ReactNode;
  variant?: "split" | "centered";
  className?: string;
}) {
  return (
    <HeroHeader variant={variant} className={className}>
      <HeroHeader.Content className={variant === "centered" ? "mx-auto max-w-3xl text-center" : undefined}>
        {eyebrow && <HeroHeader.Eyebrow>{eyebrow}</HeroHeader.Eyebrow>}
        <HeroHeader.Title>{title}</HeroHeader.Title>
        {description && <HeroHeader.Description>{description}</HeroHeader.Description>}
        {actions.length > 0 && (
          <HeroHeader.Actions className={variant === "centered" ? "justify-center" : undefined}>
            {actions.map((action, index) => (
              <MarketingActionLink key={`${action.href ?? "action"}-${index}`} action={action} />
            ))}
          </HeroHeader.Actions>
        )}
      </HeroHeader.Content>
      {visual && <HeroHeader.Visual>{visual}</HeroHeader.Visual>}
    </HeroHeader>
  );
}

export function MarketingSection({ children, id, className, ...props }: HTMLAttributes<HTMLElement> & { children: ReactNode }) {
  return <section {...props} id={id} className={cx("py-16 sm:py-24", className)}><PageContainer>{children}</PageContainer></section>;
}

export function MarketingSectionHeading({ eyebrow, title, description, className }: { eyebrow?: ReactNode; title: ReactNode; description?: ReactNode; className?: string }) {
  return (
    <div className={cx("max-w-2xl", className)}>
      {eyebrow && <p className="text-sm font-semibold tracking-[0.12em] text-accent">{eyebrow}</p>}
      <h2 className="mt-3 text-balance text-3xl font-semibold tracking-[-0.025em] sm:text-4xl">{title}</h2>
      {description && <p className="mt-4 text-lg leading-relaxed text-muted">{description}</p>}
    </div>
  );
}

function FeatureItemCardRoot(props: ComponentProps<typeof ItemCard>) { return <ItemCard {...props} />; }
export const FeatureItemCard = Object.assign(FeatureItemCardRoot, {
  Root: ItemCard.Root,
  Icon: ItemCard.Icon,
  Content: ItemCard.Content,
  Title: ItemCard.Title,
  Description: ItemCard.Description,
  Action: ItemCard.Action,
});

function FeatureItemGroupRoot(props: ComponentProps<typeof ItemCardGroup>) { return <ItemCardGroup {...props} />; }
export const FeatureItemGroup = Object.assign(FeatureItemGroupRoot, {
  Root: ItemCardGroup.Root,
  Header: ItemCardGroup.Header,
  Title: ItemCardGroup.Title,
  Description: ItemCardGroup.Description,
});

function PricingCardRoot({ className, ...props }: ComponentProps<typeof Card>) {
  return <Card {...props} className={cx("h-full", className)} data-marketing-component="pricing-card" />;
}

function PricingCardBadge({ children, className, ...props }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return <div {...props} className={cx("mb-4 inline-flex w-fit items-center rounded-full bg-accent/10 px-3 py-1 text-xs font-semibold text-accent", className)}>{children}</div>;
}

function PricingCardPrice({ children, className, ...props }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return <div {...props} className={cx("mt-6 text-4xl font-semibold tracking-[-0.03em]", className)}>{children}</div>;
}

function PricingCardBilling({ children, className, ...props }: HTMLAttributes<HTMLParagraphElement> & { children: ReactNode }) {
  return <p {...props} className={cx("mt-2 text-sm text-muted", className)}>{children}</p>;
}

function PricingCardFeatures({ children, className, ...props }: HTMLAttributes<HTMLUListElement> & { children: ReactNode }) {
  return <ul {...props} className={cx("flex flex-col gap-3", className)}>{children}</ul>;
}

function PricingCardFeature({ children, icon, className, ...props }: HTMLAttributes<HTMLLIElement> & { children: ReactNode; icon?: ReactNode }) {
  return <li {...props} className={cx("flex items-start gap-3 text-sm text-muted", className)}>{icon && <span aria-hidden="true" className="mt-0.5 shrink-0 text-accent">{icon}</span>}<span>{children}</span></li>;
}

export const PricingCard = Object.assign(PricingCardRoot, {
  Badge: PricingCardBadge,
  Header: Card.Header,
  Title: Card.Title,
  Description: Card.Description,
  Price: PricingCardPrice,
  Billing: PricingCardBilling,
  Content: Card.Content,
  Features: PricingCardFeatures,
  Feature: PricingCardFeature,
  Footer: Card.Footer,
});

export function PricingGrid({ children, className, ...props }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return <div {...props} className={cx("grid items-stretch gap-5 md:grid-cols-2 xl:grid-cols-3", className)} data-marketing-component="pricing-grid">{children}</div>;
}

function FeatureCarouselRoot({ children, className, opts, ...props }: ComponentProps<typeof HeroCarousel.Root>) {
  return (
    <HeroCarousel.Root
      {...props}
      opts={{ align: "center", containScroll: false, ...opts }}
      className={cx("relative", className)}
      data-marketing-component="feature-carousel"
    >
      {children}
      <div className="mt-5 flex justify-center gap-2">
        <HeroCarousel.Previous />
        <HeroCarousel.Next />
      </div>
    </HeroCarousel.Root>
  );
}

function FeatureCarouselContent({ className, ...props }: ComponentProps<typeof HeroCarousel.Content>) {
  return <HeroCarousel.Content {...props} className={cx("-ms-3", className)} />;
}

function FeatureCarouselItem({ className, ...props }: ComponentProps<typeof HeroCarousel.Item>) {
  return <HeroCarousel.Item {...props} className={cx("min-w-0 basis-[86%] ps-3 sm:basis-[58%] lg:basis-[38%]", className)} />;
}

export const FeatureCarousel = Object.assign(FeatureCarouselRoot, {
  Content: FeatureCarouselContent,
  Item: FeatureCarouselItem,
  Previous: HeroCarousel.Previous,
  Next: HeroCarousel.Next,
});

export function MarketingFinalCta({ title, description, actions = [], className }: { title: ReactNode; description?: ReactNode; actions?: MarketingAction[]; className?: string }) {
  return <MarketingSection className={cx("text-center", className)}><Card className="mx-auto max-w-2xl" variant="secondary"><Card.Header><Card.Title className="text-3xl">{title}</Card.Title>{description && <Card.Description>{description}</Card.Description>}</Card.Header><Card.Footer className="justify-center"><div className="flex flex-wrap justify-center gap-3">{actions.map((action, index) => <MarketingActionLink key={`${action.href ?? "action"}-${index}`} action={action} />)}</div></Card.Footer></Card></MarketingSection>;
}

export function MarketingFooter({ brand, description, links = [], navigationLabel }: { brand: ReactNode; description?: ReactNode; links?: MarketingLink[]; navigationLabel?: string }) {
  return <div className="border-t border-default py-10"><PageContainer className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between"><div><div>{brand}</div>{description && <p className="mt-2 max-w-md text-sm text-muted">{description}</p>}</div><nav aria-label={navigationLabel} className="flex flex-wrap gap-4">{links.map((link) => <Link key={link.href} href={link.href} target={link.external ? "_blank" : undefined} rel={link.external ? "noreferrer" : undefined} className="text-sm text-muted">{link.label}</Link>)}</nav></PageContainer></div>;
}
