import type { ReactNode } from "react";

interface FooterProps {
  fa: boolean;
  tagline: string;
  status: string;
  exploreLabel: string;
  productsLabel: string;
  companyLabel: string;
  spotlightLink: string;
  appsLink: string;
  purposeLink: string;
  contactLink: string;
  clinicLink: string;
  salonLink: string;
  shopLink: string;
  aboutLink: string;
  top: string;
}

export function Footer(props: FooterProps) {
  return (
    <div className="footer shell">
      <div className="footer-grid">
        <div className="footer-brand">
          <a className="brand" href="#top" aria-label="Vitarish home">
            <img src="/transparent.webp" alt="" />
            <span>vitarish</span>
          </a>
          <p>{props.tagline}</p>
          <div className="footer-status">
            <span />
            <span>{props.status}</span>
          </div>
        </div>
        <FooterColumn title={props.exploreLabel} links={[
          [props.spotlightLink, "#spotlight"],
          [props.appsLink, "#apps"],
          [props.purposeLink, "#purpose"],
          [props.contactLink, "#contact"],
        ]} />
        <FooterColumn title={props.productsLabel} links={[
          [props.clinicLink, "#apps"],
          [props.salonLink, "#apps"],
          [props.shopLink, "#apps"],
        ]} />
        <FooterColumn title={props.companyLabel} links={[
          [props.aboutLink, "#purpose"],
          [props.contactLink, "#contact"],
          [props.top, "#top"],
        ]} />
      </div>
      <div className="footer-meta">
        <span>© Vitarish</span>
        <span>{props.fa ? "شکوه هدف // ویتاریش" : "The glory of purpose // Vitarish"}</span>
      </div>
    </div>
  );
}

function FooterColumn({ title, links }: { title: ReactNode; links: [ReactNode, string][] }) {
  return (
    <div className="footer-column">
      <p>{title}</p>
      <ul>
        {links.map(([label, href]) => (
          <li key={href + String(label)}>
            <a href={href}>{label}</a>
          </li>
        ))}
      </ul>
    </div>
  );
}
