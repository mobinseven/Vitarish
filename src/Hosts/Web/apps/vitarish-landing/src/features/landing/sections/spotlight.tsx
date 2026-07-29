import { Button, Card, Chip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";

interface SpotlightProps {
  fa: boolean;
  title: string;
  title2: string;
  body: string;
  daily: string;
  discover: string;
  series: string;
}

const spotlight = {
  en: { title: "Words that meet", title2: "you halfway.", body: "A clearer way to understand the words around you — with AI that explains, translates, and connects the dots." },
  fa: { title: "کلماتی که", title2: "به تو نزدیک‌ترند.", body: "راهی روشن‌تر برای فهمیدن کلمات اطراف؛ با هوش مصنوعی که توضیح می‌دهد، ترجمه می‌کند و ارتباط‌ها را پیدا می‌کند." },
};

export function Spotlight({ fa, title, title2, body, daily, discover, series }: SpotlightProps) {
  const item = spotlight[fa ? "fa" : "en"];
  return (
    <section className="spotlight shell" id="spotlight">
      <div className="heading reveal"><h2>{title}<br /><span>{title2}</span></h2><p>{body}</p></div>
      <Card className="spot-card reveal">
        <Card.Content className="spot-card-content">
          <div className="meta"><span><FaIcon icon="sun-bright" aria-hidden="true" /> {fa ? "امروز" : "Today"}</span><span>28.07.2026</span></div>
          <div className="spot-content">
            <div>
              <Chip color="success" variant="soft" size="sm">{daily}</Chip>
              <h3>{item.title}<br />{item.title2}</h3>
              <p>{item.body}</p>
              <Button className="button dark" variant="secondary" onPress={() => { window.location.hash = "contact"; }}>
                {discover}<FaIcon icon="arrow-up-right" aria-hidden="true" />
              </Button>
            </div>
            <div className="dictionary" aria-hidden="true"><span>meaning</span><i>/məˈniŋ/</i><b /><b /><FaIcon icon="sparkles" /></div>
          </div>
          <div className="meta"><span>{series}</span><FaIcon icon="arrow-up-right" aria-hidden="true" /></div>
        </Card.Content>
      </Card>
    </section>
  );
}
