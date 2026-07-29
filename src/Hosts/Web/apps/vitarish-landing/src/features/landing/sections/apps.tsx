import type { ReactNode } from "react";
import { PurposeDashboard } from "./purpose-dashboard";
import { PurposeShowcase } from "./purpose-showcase";

export function Apps({ fa, title, body }: { fa: boolean; title: ReactNode; body: string }) {
  return (
    <>
      <section className="apps shell" id="apps">
        <div className="heading reveal"><h2>{title}</h2><p>{body}</p></div>
        <PurposeShowcase />
      </section>
      <section className="purpose-dashboard shell" id="purpose-dashboard"><PurposeDashboard fa={fa} /></section>
    </>
  );
}
