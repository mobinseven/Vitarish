import type { ReactNode } from "react";
import { FaIcon } from "@vitarish/web-ui";

export function Purpose({ quote, body, learn }: { quote: ReactNode; body: string; learn: string }) {
  return (
    <section className="purpose shell" id="purpose">
      <div className="purpose-grid reveal">
        <p>{quote}</p>
        <div><p>{body}</p><a className="link" href="#contact">{learn}<FaIcon icon="arrow-right" aria-hidden="true" /></a></div>
      </div>
    </section>
  );
}
