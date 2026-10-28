import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { EditorialContentPage } from "../EditorialContentPage";

describe("editorial content display", () => {
  it("renders plain text safely without empty navigation or metadata landmarks", () => {
    const markup = renderToStaticMarkup(
      <EditorialContentPage
        brand="Example"
        homeHref="/"
        homeLabel="Home"
        navigationLabel="Explore"
        navigationTooltip="Explore"
        navigationLinks={[]}
        title="An article"
        footerNavigationLabel="Explore"
        footerLinks={[]}
      >
        <p>{"<script>untrusted text</script>"}</p>
      </EditorialContentPage>,
    );

    expect(markup).toContain("&lt;script&gt;untrusted text&lt;/script&gt;");
    expect(markup).not.toContain("<script>");
    expect(markup).not.toContain("<nav");
    expect(markup).not.toContain("<aside");
  });

  it("renders supplied footer groups and ignores empty groups instead of falling back to flat links", () => {
    const markup = renderToStaticMarkup(
      <EditorialContentPage
        brand="Example"
        homeHref="/"
        homeLabel="Home"
        navigationLabel="Explore"
        navigationTooltip="Explore"
        navigationLinks={[]}
        title="An article"
        footerNavigationLabel="Footer"
        footerLinks={[{ href: "/fallback", label: "Fallback" }]}
        footerGroups={[
          { label: "Research", links: [{ href: "/research", label: "Research home" }] },
          { label: "Company", links: [{ href: "/company", label: "Company home" }] },
          { label: "Empty", links: [] },
        ]}
      >
        <p>Article text</p>
      </EditorialContentPage>,
    );

    expect(markup).toContain("Research");
    expect(markup).toContain("Company");
    expect(markup).not.toContain("Empty");
    expect(markup).not.toContain("Fallback");
    expect(markup.match(/aria-expanded="false"/g)).toHaveLength(2);
  });
});
