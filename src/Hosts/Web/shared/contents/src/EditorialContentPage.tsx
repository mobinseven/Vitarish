"use client";

import type { ReactNode } from "react";
import { Accordion, Button, Dropdown, Header, Label, Link } from "@heroui/react";
import { PublicDocumentShell, StandaloneDocumentPage } from "@vitarish/ui/layout";
import styles from "./editorial-content.module.css";

export interface EditorialLink {
  label: string;
  href: string;
}

export interface EditorialLinkGroup {
  label: string;
  tooltip?: string;
  links: EditorialLink[];
}

export interface EditorialMetadata {
  label: string;
  value: ReactNode;
}

/** Presentation only: the host owns publication, routes, copy and localized values. */
export interface EditorialContentPageProps {
  brand: ReactNode;
  homeHref: string;
  homeLabel: string;
  navigationLabel: string;
  navigationTooltip: string;
  navigationLinks: EditorialLink[];
  navigationGroups?: EditorialLinkGroup[];
  headerAction?: ReactNode;
  title: string;
  metadata?: EditorialMetadata[];
  children: ReactNode;
  footerDescription?: ReactNode;
  footerNavigationLabel: string;
  footerLinks: EditorialLink[];
  footerGroups?: EditorialLinkGroup[];
  className?: string;
}

export function EditorialContentPage({
  brand,
  homeHref,
  homeLabel,
  navigationLabel,
  navigationTooltip,
  navigationLinks,
  navigationGroups = [],
  headerAction,
  title,
  metadata = [],
  children,
  footerDescription,
  footerNavigationLabel,
  footerLinks,
  footerGroups,
  className,
}: EditorialContentPageProps) {
  const visibleNavigationGroups = navigationGroups.filter((group) => group.links.length > 0);
  const visibleFooterGroups = (footerGroups ?? [{
    label: footerNavigationLabel,
    tooltip: navigationTooltip,
    links: footerLinks,
  }]).filter((group) => group.links.length > 0);

  return (
    <PublicDocumentShell
      className={[styles.page, className].filter(Boolean).join(" ")}
      header={
        <div className={styles.header}>
          <div className={styles.navigation}>
            {(navigationLinks.length > 0 || visibleNavigationGroups.length > 0) && (
              <nav aria-label={navigationLabel}>
                <div className={styles.desktopNavigation}>
                  {navigationLinks.map((link) => (
                    <Link className={styles.navigationLink} href={link.href} key={link.href}>{link.label}</Link>
                  ))}
                  {visibleNavigationGroups.map((group) => (
                    <Dropdown key={group.label}>
                      <Button
                        aria-label={group.label}
                        className={styles.menuButton}
                        render={(props) => <button {...props} title={group.tooltip ?? group.label} />}
                        variant="ghost"
                      >
                        <span>{group.label}</span>
                        <i aria-hidden="true" className="fa-light fa-chevron-down" />
                      </Button>
                      <Dropdown.Popover>
                        <Dropdown.Menu aria-label={group.label}>
                          {group.links.map((link) => (
                            <Dropdown.Item href={link.href} id={link.href} key={link.href} textValue={link.label}>
                              <Label>{link.label}</Label>
                            </Dropdown.Item>
                          ))}
                        </Dropdown.Menu>
                      </Dropdown.Popover>
                    </Dropdown>
                  ))}
                </div>
                <div className={styles.mobileNavigation}>
                  <Dropdown>
                    <Button
                      aria-label={navigationLabel}
                      className={styles.menuButton}
                      render={(props) => <button {...props} title={navigationTooltip} />}
                      variant="ghost"
                    >
                      <i aria-hidden="true" className="fa-light fa-bars" />
                      <span>{navigationLabel}</span>
                    </Button>
                    <Dropdown.Popover>
                      <Dropdown.Menu aria-label={navigationLabel}>
                        {navigationLinks.map((link) => (
                          <Dropdown.Item href={link.href} id={link.href} key={link.href} textValue={link.label}>
                            <Label>{link.label}</Label>
                          </Dropdown.Item>
                        ))}
                        {visibleNavigationGroups.map((group) => (
                          <Dropdown.Section key={group.label}>
                            <Header>{group.label}</Header>
                            {group.links.map((link) => (
                              <Dropdown.Item href={link.href} id={`${group.label}:${link.href}`} key={link.href} textValue={link.label}>
                                <Label>{link.label}</Label>
                              </Dropdown.Item>
                            ))}
                          </Dropdown.Section>
                        ))}
                      </Dropdown.Menu>
                    </Dropdown.Popover>
                  </Dropdown>
                </div>
              </nav>
            )}
          </div>
          <Link aria-label={homeLabel} className={styles.brand} href={homeHref}>{brand}</Link>
          <div className={styles.headerAction}>{headerAction}</div>
        </div>
      }
      footer={
        <div className={styles.footer}>
          <Link aria-label={homeLabel} className={styles.footerBrand} href={homeHref}>{brand}</Link>
          {visibleFooterGroups.length > 0 && (
            <nav aria-label={footerNavigationLabel} className={styles.footerNavigation}>
              <Accordion allowsMultipleExpanded hideSeparator>
                {visibleFooterGroups.map((group) => (
                  <Accordion.Item className={styles.footerItem} id={group.label} key={group.label}>
                    <Accordion.Heading>
                      <Accordion.Trigger
                        className={styles.footerTrigger}
                        render={(props) => <button {...props} title={group.tooltip ?? group.label} />}
                      >
                        <span>{group.label}</span>
                        <Accordion.Indicator className={`${styles.footerIndicator} fa-light fa-chevron-down`}>
                          <i aria-hidden="true" />
                        </Accordion.Indicator>
                      </Accordion.Trigger>
                    </Accordion.Heading>
                    <Accordion.Panel>
                      <Accordion.Body className={styles.footerGroup}>
                        <ul>
                          {group.links.map((link) => (
                            <li key={link.href}><Link href={link.href}>{link.label}</Link></li>
                          ))}
                        </ul>
                      </Accordion.Body>
                    </Accordion.Panel>
                  </Accordion.Item>
                ))}
              </Accordion>
            </nav>
          )}
          {footerDescription && <p className={styles.footerDescription}>{footerDescription}</p>}
        </div>
      }
    >
      <StandaloneDocumentPage className={styles.main}>
        <article className={styles.article}>
          <header className={styles.articleHeader}>
            <h1>{title}</h1>
          </header>
          <div className={metadata.length > 0 ? styles.readingLayout : `${styles.readingLayout} ${styles.readingLayoutWithoutMetadata}`}>
            <div className={styles.body}>{children}</div>
            {metadata.length > 0 && (
              <aside className={styles.metadata}>
                <dl>
                  {metadata.map((item) => (
                    <div className={styles.metadataRow} key={item.label}>
                      <dt>{item.label}</dt>
                      <dd>{item.value}</dd>
                    </div>
                  ))}
                </dl>
              </aside>
            )}
          </div>
        </article>
      </StandaloneDocumentPage>
    </PublicDocumentShell>
  );
}
