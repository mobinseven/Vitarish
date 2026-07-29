import type { ReactNode } from "react";
import { Button } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";

interface HeroProps {
  fa: boolean;
  title: ReactNode;
  body: string;
  explore: string;
  seeApps: string;
}

export function Hero({ fa, title, body, explore, seeApps }: HeroProps) {
  return (
    <>
      <section className="hero shell">
        <div className="hero-copy reveal">
          <div className="dots"><i /><i /><i /></div>
          <h1>{title}</h1>
          <p>{body}</p>
          <div className="hero-buttons">
            <Button className="button primary" variant="primary" onPress={() => { window.location.hash = "spotlight"; }}>
              {explore}<FaIcon icon="arrow-down-right" aria-hidden="true" />
            </Button>
            <a className="link" href="#apps">
              {seeApps}<FaIcon icon="arrow-right" aria-hidden="true" />
            </a>
          </div>
        </div>
        <div className="map reveal late" aria-label={fa ? "شکل انتزاعی هدف در حرکت" : "Abstract purpose in motion"}>
          <div className="purpose-shape" aria-hidden="true"><span /><span /><span /><i /></div>
          <small><b />{fa ? "هدف در حرکت" : "Purpose in motion"}</small>
        </div>
      </section>
      <div className="platform-strip shell" aria-label={fa ? "ویژگی‌های پلتفرم" : "Platform characteristics"}>
        <span>{fa ? "هوش مصنوعی هدف‌محور" : "Purpose-oriented AI"}</span>
        <span>{fa ? "برای مردم و کسب‌وکارها" : "People + business"}</span>
        <span>{fa ? "فارسی + انگلیسی" : "Persian + English"}</span>
      </div>
    </>
  );
}
