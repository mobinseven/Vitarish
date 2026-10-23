import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { RememberedNotes, UserMemorySection } from "../components/UserMemory";

const forget = { mutate: vi.fn(), isPending: false, isError: false, variables: undefined as string | undefined };
const forgetAll = { mutate: vi.fn(), isPending: false, isError: false };
const update = { mutate: vi.fn(), isPending: false, isError: false };
const memory = {
  isPending: false,
  isError: false,
  data: { items: [{ id: "a", text: "Runs a dental clinic", source: "Landing", rememberedAt: "2026-10-23T09:00:00Z" }] },
};

vi.mock("../hooks/useUserMemory", () => ({
  useUserMemory: () => ({ memory, update, forget, forgetAll }),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("RememberedNotes", () => {
  it("forgets a remembered fact on undo and says so", () => {
    forget.mutate.mockImplementation((_id: string, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
    render(<RememberedNotes items={[{ id: "a", text: "Runs a dental clinic" }]} />);

    fireEvent.click(screen.getByRole("button", { name: /intelligence.memory.undo/ }));

    expect(forget.mutate).toHaveBeenCalledWith("a", expect.anything());
    expect(screen.getByText("intelligence.memory.forgotten")).toBeVisible();
  });
});

describe("UserMemorySection", () => {
  it("asks once more before forgetting everything", () => {
    render(<UserMemorySection />);

    fireEvent.click(screen.getByRole("button", { name: /intelligence.memory.forgetAll/ }));
    expect(forgetAll.mutate).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: /intelligence.memory.confirmForgetAll/ }));
    expect(forgetAll.mutate).toHaveBeenCalledTimes(1);
  });
});
