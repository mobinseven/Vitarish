import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ContainedPage, ImmersiveRegion, RevealHeader } from "../index";

describe("RevealHeader", () => {
  it("reveals after its boundary crosses the document activation line", async () => {
    let boundaryTop = 20;
    const { container } = render(
      <>
        <RevealHeader
          compactContent={<button type="button">Compact navigation</button>}
        />
        <div>
          <RevealHeader.Boundary
            ref={(element) => {
              if (element) {
                vi.spyOn(element, "getBoundingClientRect").mockImplementation(
                  () => ({ top: boundaryTop }) as DOMRect,
                );
              }
            }}
          />
        </div>
      </>,
    );

    const header = container.querySelector(".vitarish-reveal-header");
    expect(header).toHaveAttribute("data-state", "hidden");
    expect(header).toHaveAttribute("aria-hidden", "true");
    expect(header).toHaveAttribute("inert");

    boundaryTop = -1;
    fireEvent.scroll(window);

    await waitFor(() =>
      expect(header).toHaveAttribute("data-state", "revealed"),
    );
    expect(header).toHaveAttribute("aria-hidden", "false");
    expect(header).not.toHaveAttribute("inert");
    expect(
      screen.getByRole("button", { name: "Compact navigation" }),
    ).toBeInTheDocument();
  });

  it("uses the supplied scroll container and reserves space only in inset mode", async () => {
    const scrollRoot = document.createElement("div");
    document.body.append(scrollRoot);
    let boundaryTop = 10;
    vi.spyOn(scrollRoot, "getBoundingClientRect").mockImplementation(
      () => ({ top: 50 }) as DOMRect,
    );

    const { container, unmount } = render(
      <>
        <RevealHeader
          compactContent="Compact"
          positioning="inset"
          scrollRoot={scrollRoot}
        />
        <RevealHeader.Boundary
          ref={(element) => {
            if (element) {
              vi.spyOn(element, "getBoundingClientRect").mockImplementation(
                () => ({ top: boundaryTop }) as DOMRect,
              );
            }
          }}
        />
      </>,
    );

    expect(
      container.querySelector(".vitarish-reveal-header-inset"),
    ).toBeInTheDocument();
    boundaryTop = 49;
    fireEvent.scroll(scrollRoot);
    await waitFor(() =>
      expect(scrollRoot.style.scrollPaddingBlockStart).not.toBe(""),
    );

    unmount();
    expect(scrollRoot.style.scrollPaddingBlockStart).toBe("");
    scrollRoot.remove();
  });

  it("ports compact content into the nearest sticky layout host", async () => {
    const { container } = render(
      <ContainedPage>
        <RevealHeader compactContent="Compact" />
        <RevealHeader.Boundary />
      </ContainedPage>,
    );

    const host = container.querySelector("[data-vitarish-reveal-header-host]");
    await waitFor(() =>
      expect(host).toContainElement(
        container.querySelector(".vitarish-reveal-header"),
      ),
    );
  });

  it("uses the workspace host when the boundary belongs to an immersive region", async () => {
    const { container } = render(
      <ImmersiveRegion>
        <RevealHeader compactContent="Compact" />
        <RevealHeader.Boundary />
      </ImmersiveRegion>,
    );

    const region = container.querySelector(
      '[data-vitarish-reveal-header-region="workspace"]',
    );
    const host = region?.querySelector("[data-vitarish-reveal-header-host]");
    await waitFor(() =>
      expect(host).toContainElement(
        container.querySelector(".vitarish-reveal-header"),
      ),
    );
  });
});
