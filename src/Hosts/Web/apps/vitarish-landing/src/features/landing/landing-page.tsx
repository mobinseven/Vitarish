"use client";

import { useEffect, useState } from "react";
import {
  PublicDocumentShell,
  StandaloneDocumentPage,
} from "@vitarish/web-ui/layout";
import { Apps } from "./sections/apps";
import { ContactSection } from "./sections/contact-section";
import { Footer } from "./sections/footer";
import { Header } from "./sections/header";
import { Hero } from "./sections/hero";
import { Purpose } from "./sections/purpose";
import { Spotlight } from "./sections/spotlight";

const content = {
  fa: {
    nav: { spotlight: "سرسرا", apps: "برنامه‌ها", purpose: "هدف ما" },
    hero: {
      title: <>برای<br /><em>مهم‌ترین‌ها.</em></>,
      body: "ویتاریش خدمات هوش مصنوعی هدف‌محور برای انسان‌ها و کسب‌وکارهای کوچکی می‌سازد که جهان را پیش می‌برند.",
      explore: "ویژه امروز را ببینید",
      seeApps: "اپ‌های موجود",
    },
    spotlight: {
      title: "ایده‌ای کاربردی،", title2: "درست به‌موقع.",
      body: "هر روز یک پیشنهاد کوچک برای کشف چیزی ارزشمند.",
      daily: "ویژه امروز", discover: "کشف ابزار", series: "بخشی از مجموعه ابزارهای عمومی ویتاریش",
    },
    apps: {
      title: <>ساخته‌شده برای کاری که<br /><span>همین حالا انجام می‌دهی.</span></>,
      body: "ابزارهایی متمرکز برای کسب‌وکارهای کوچک، با مقصدهای کاربردی بیشتر در آینده.",
    },
    purpose: {
      quote: <>هدف، مقصد نیست.<br /><span>جهت حرکت است.</span></>,
      body: "ما فناوری‌ای می‌سازیم که از سر راه کاری که برایش اینجایی کنار برود. برای یک مطب، یک کسب‌وکار یا یک ذهن کنجکاو — بخش کاربردی همیشه اصل است.",
      learn: "بیشتر درباره ویتاریش",
    },
    contact: { title: <>بیایید<br /><em>چیزی کاربردی بسازیم.</em></>, body: "هدفی در ذهن دارید؟ دوست داریم درباره‌اش بشنویم." },
    footer: {
      tagline: "شکوه هدف.", status: "پلتفرمی در حال رشد", exploreLabel: "کشف", productsLabel: "محصولات", companyLabel: "ویتاریش",
      spotlightLink: "ویژه", appsLink: "برنامه‌ها", purposeLink: "هدف ما", contactLink: "ارتباط",
      clinicLink: "مطب و کلینیک", salonLink: "سرای زیبایی", shopLink: "فروشگاه آنلاین", aboutLink: "درباره ویتاریش", top: "بازگشت به بالا ↑",
    },
  },
  en: {
    nav: { spotlight: "Spotlight", apps: "Apps", purpose: "Our purpose" },
    hero: {
      title: <>Make room for<br /><em>what matters.</em></>,
      body: "Vitarish creates purpose-oriented AI services for the people and small businesses moving the world forward.",
      explore: "Explore today’s spotlight", seeApps: "See available apps",
    },
    spotlight: {
      title: "A useful idea,", title2: "right on time.",
      body: "A small spotlight for something worth discovering. Updated daily, with purpose.",
      daily: "Daily spotlight", discover: "Discover the tool", series: "Part of the Vitarish common tools series",
    },
    apps: {
      title: <>Built for the work<br /><span>you already do.</span></>,
      body: "Focused tools for small businesses, with more useful places to go next.",
    },
    purpose: {
      quote: <>Purpose is not a destination.<br /><span>It is the direction.</span></>,
      body: "We build technology that gets out of the way of the thing you are here to do. For a practice, a business, a curious mind — the useful part is always the point.",
      learn: "Learn more about Vitarish",
    },
    contact: { title: <>Let’s make<br /><em>something useful.</em></>, body: "Have a purpose in mind? We would like to hear about it." },
    footer: {
      tagline: "The glory of purpose.", status: "Platform in progress", exploreLabel: "Explore", productsLabel: "Products", companyLabel: "Vitarish",
      spotlightLink: "Spotlight", appsLink: "Apps", purposeLink: "Our purpose", contactLink: "Contact",
      clinicLink: "Private practice", salonLink: "Salon & beauty", shopLink: "E-shop", aboutLink: "About Vitarish", top: "Back to top ↑",
    },
  },
};

export function LandingPage() {
  const [locale, setLocale] = useState<keyof typeof content>("fa");
  const fa = locale === "fa";
  const copy = content[locale];

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = fa ? "rtl" : "ltr";
  }, [fa, locale]);

  return (
    <PublicDocumentShell
      className="bg-background text-foreground"
      header={<Header fa={fa} {...copy.nav} onLocaleChange={() => setLocale(fa ? "en" : "fa")} />}
      footer={<Footer fa={fa} {...copy.footer} />}
    >
      <StandaloneDocumentPage id="top">
        <div className="landing" dir={fa ? "rtl" : "ltr"}>
          <Hero fa={fa} {...copy.hero} />
          <Spotlight fa={fa} {...copy.spotlight} />
          <Apps fa={fa} {...copy.apps} />
          <Purpose {...copy.purpose} />
          <ContactSection locale={locale} {...copy.contact} />
        </div>
      </StandaloneDocumentPage>
    </PublicDocumentShell>
  );
}
