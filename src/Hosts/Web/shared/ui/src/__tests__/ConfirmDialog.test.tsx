import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ConfirmDialog } from "../feedback/ConfirmDialog";

function renderDialog(overrides: Partial<React.ComponentProps<typeof ConfirmDialog>> = {}) {
  const onCancel = vi.fn();
  const onConfirm = vi.fn();
  render(
    <ConfirmDialog
      isOpen
      title="Revoke role"
      message="Revoke this tenant role?"
      confirmLabel="Revoke"
      cancelLabel="Cancel"
      confirmIcon="user-minus"
      onCancel={onCancel}
      onConfirm={onConfirm}
      {...overrides}
    />,
  );
  return { onCancel, onConfirm };
}

describe("confirm dialog", () => {
  afterEach(cleanup);

  it("renders nothing while closed", () => {
    renderDialog({ isOpen: false });

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("asks with the given title and message and runs only the button the person presses", async () => {
    const user = userEvent.setup();
    const { onCancel, onConfirm } = renderDialog();
    const dialog = screen.getByRole("dialog");

    expect(within(dialog).getByText("Revoke role")).toBeInTheDocument();
    expect(within(dialog).getByText("Revoke this tenant role?")).toBeInTheDocument();
    expect(onConfirm).not.toHaveBeenCalled();

    await user.click(within(dialog).getByRole("button", { name: "Revoke" }));
    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(onCancel).not.toHaveBeenCalled();
  });

  it("cancels from the Cancel button without confirming", async () => {
    const user = userEvent.setup();
    const { onCancel, onConfirm } = renderDialog();

    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Cancel" }));

    expect(onCancel).toHaveBeenCalledTimes(1);
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("cannot be cancelled while the confirmed action is running", async () => {
    const user = userEvent.setup();
    const { onCancel } = renderDialog({ isPending: true });

    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Cancel" }));
    await user.keyboard("{Escape}");

    expect(onCancel).not.toHaveBeenCalled();
  });
});
