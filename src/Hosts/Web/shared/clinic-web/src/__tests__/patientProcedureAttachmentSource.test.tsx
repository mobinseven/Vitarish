import { renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { usePatientProcedureAttachmentSource } from "../hooks/patients/procedures/usePatientProcedureAttachmentSource";

const storedContent = vi.hoisted(() => ({
  load: vi.fn<(content: string, signal?: AbortSignal) => Promise<Blob>>(),
}));

vi.mock("@vitarish/sea-web", () => ({
  isStoredSeaAttachmentContent: (content: string) =>
    content.startsWith("stored:"),
  loadStoredSeaAttachmentContent: storedContent.load,
}));

afterEach(() => {
  vi.restoreAllMocks();
  storedContent.load.mockReset();
});

describe("patient procedure attachment source", () => {
  it("aborts replaced loads and revokes the resolved object URL on cleanup", async () => {
    let resolveSecond: ((blob: Blob) => void) | undefined;
    storedContent.load
      .mockImplementationOnce(() => new Promise(() => undefined))
      .mockImplementationOnce(
        () =>
          new Promise((resolve) => {
            resolveSecond = resolve;
          }),
      );
    const createObjectUrl = vi
      .spyOn(URL, "createObjectURL")
      .mockReturnValue("blob:attachment");
    const revokeObjectUrl = vi
      .spyOn(URL, "revokeObjectURL")
      .mockImplementation(() => undefined);

    const { rerender, result, unmount } = renderHook(
      ({ content }) => usePatientProcedureAttachmentSource(content, true),
      { initialProps: { content: "stored:first" } },
    );
    const firstSignal = storedContent.load.mock.calls[0][1];
    rerender({ content: "stored:second" });
    expect(firstSignal?.aborted).toBe(true);

    resolveSecond?.(new Blob(["file"]));
    await waitFor(() => expect(result.current.url).toBe("blob:attachment"));
    expect(createObjectUrl).toHaveBeenCalledOnce();

    unmount();
    expect(revokeObjectUrl).toHaveBeenCalledWith("blob:attachment");
  });

  it("exposes stored-content failures without leaking the identifier as a URL", async () => {
    storedContent.load.mockRejectedValueOnce(new Error("unavailable"));
    const { result } = renderHook(() =>
      usePatientProcedureAttachmentSource("stored:unavailable", true),
    );

    await waitFor(() => expect(result.current.error).toBe(true));
    expect(result.current.url).toBeUndefined();
  });
});
