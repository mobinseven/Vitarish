import type { ReactNode } from "react";
import {
  ContactForm,
  submitContactInquiry,
  type ContactFormConversation,
} from "@vitarish/communications-web";
import { communicationsMessageCatalogs } from "@vitarish/communications-web/localization";
import { LocalizationProvider } from "@vitarish/web-localization";

export interface ContactSectionProps {
  locale: "en" | "fa";
  title: ReactNode;
  body: string;
}

export function ContactSection({ locale, title, body }: ContactSectionProps) {
  const fa = locale === "fa";
  const conversation: ContactFormConversation = fa
    ? {
        prefix: "می‌خواهم",
        between: "را",
        suffix: ".",
        goalLabel: "هدف",
        needLabel: "موضوع",
        goals: [
          { id: "business", label: "کسب‌وکارم" },
          { id: "practice", label: "مطبم" },
          { id: "idea", label: "ایده‌ام" },
        ],
        needs: [
          { id: "grow", label: "رشد دهم" },
          { id: "launch", label: "راه‌اندازی کنم" },
          { id: "simplify", label: "ساده‌تر سازم" },
        ],
      }
    : {
        prefix: "I want to",
        between: "my",
        suffix: "and I need Vitarish to make it happen.",
        goalLabel: "Goal",
        needLabel: "What you want to work on",
        goals: [
          { id: "grow", label: "grow" },
          { id: "launch", label: "launch" },
          { id: "simplify", label: "simplify" },
        ],
        needs: [
          { id: "business", label: "business" },
          { id: "practice", label: "practice" },
          { id: "idea", label: "idea" },
        ],
      };

  return (
    <section className="contact shell" id="contact">
      <div className="contact-intro reveal">
        <p>{fa ? "یک گفت‌وگو" : "A conversation"}</p>
        <h2>{title}</h2>
        <span>{body}</span>
      </div>
      <LocalizationProvider
        locale={locale}
        messages={communicationsMessageCatalogs[locale]}
      >
        <ContactForm
          className="contact-request reveal"
          context="vitarish-landing"
          conversation={conversation}
          onSubmit={submitContactInquiry}
        />
      </LocalizationProvider>
    </section>
  );
}
