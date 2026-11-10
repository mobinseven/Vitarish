import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { StudioSelect } from "../studio/StudioControls";

const options = [{ id: "", label: "All types" }, { id: "page", label: "Page" }];

// The visible trigger value; the hidden native select lists every option, so assertions target this.
function displayedValue(markup: string) {
  const match = /class="select__value"[^>]*>([^<]*)</.exec(markup);
  return { text: match?.[1], isPlaceholder: Boolean(match?.[0].includes("data-placeholder")) };
}

describe("studio filter select", () => {
  it("shows an empty-valued option such as All as the current choice instead of the placeholder", () => {
    const shown = displayedValue(renderToStaticMarkup(<StudioSelect label="Type" value="" options={options} onChange={() => undefined} />));

    expect(shown).toEqual({ text: "All types", isPlaceholder: false });
  });

  it("falls back to the placeholder when the value matches no option", () => {
    const shown = displayedValue(renderToStaticMarkup(<StudioSelect label="Type" value="missing" options={options} onChange={() => undefined} />));

    expect(shown.isPlaceholder).toBe(true);
  });
});
