import type { ReactNode } from "react";
import {
  cleanup,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/web-localization";
import { ContactForm } from "../components/ContactForm";
import { communicationsMessageCatalogs } from "../localization/catalogs";

afterEach(cleanup);

function renderForm(
  onSubmit: Parameters<typeof ContactForm>[0]["onSubmit"],
  {
    locale = "en",
    context,
  }: {
    locale?: "en" | "fa";
    context?: string;
  } = {},
) {
  const messages = communicationsMessageCatalogs[locale];
  return render(
    <LocalizationProvider locale={locale} messages={messages}>
      <ContactForm context={context} onSubmit={onSubmit} />
    </LocalizationProvider>,
  );
}

async function fillValidForm() {
  const user = userEvent.setup();
  await user.type(screen.getByRole("textbox", { name: "Name" }), "Sam");
  await user.type(
    screen.getByRole("textbox", { name: "Email" }),
    "sam@example.com",
  );
  await user.type(
    screen.getByRole("textbox", { name: "Message" }),
    "Please contact me.",
  );
  return user;
}

function deferred() {
  let resolve!: () => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<void>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise;
    reject = rejectPromise;
  });
  return { promise, resolve, reject };
}

describe("ContactForm", () => {
  it("uses React Aria validation for required fields and email shape", async () => {
    const onSubmit = vi.fn(async () => undefined);
    const user = userEvent.setup();
    renderForm(onSubmit);

    await user.click(screen.getByRole("button", { name: "Send message" }));
    expect(onSubmit).not.toHaveBeenCalled();
    expect(await screen.findAllByText("This field is required.")).toHaveLength(3);

    await user.type(screen.getByRole("textbox", { name: "Name" }), "Sam");
    await user.type(screen.getByRole("textbox", { name: "Email" }), "invalid");
    await user.type(screen.getByRole("textbox", { name: "Message" }), "Hello");
    await user.click(screen.getByRole("button", { name: "Send message" }));

    expect(onSubmit).not.toHaveBeenCalled();
    expect(
      await screen.findByText("Enter a valid email address."),
    ).toBeInTheDocument();
  });

  it("renders English and Persian catalogs", () => {
    const onSubmit = vi.fn(async () => undefined);
    const { unmount } = renderForm(onSubmit);

    expect(screen.getByRole("form", { name: "Contact form" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Message" })).toBeInTheDocument();

    unmount();
    renderForm(onSubmit, { locale: "fa" });

    expect(screen.getByRole("form", { name: "فرم تماس" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "پیام" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "ارسال پیام" })).toBeInTheDocument();
  });

  it("disables fields while pending and prevents duplicate submissions", async () => {
    const pending = deferred();
    const onSubmit = vi.fn(() => pending.promise);
    const user = await fillValidFormAfterRender(onSubmit);
    const submit = screen.getByRole("button", { name: "Send message" });

    await user.dblClick(submit);

    expect(onSubmit).toHaveBeenCalledOnce();
    expect(screen.getByRole("button", { name: "Sending message" })).toBeDisabled();
    expect(screen.getByRole("textbox", { name: "Name" })).toBeDisabled();

    pending.resolve();
    await screen.findByText("Message sent");
  });

  it("passes fixed context and clears editable values after success", async () => {
    const onSubmit = vi.fn(async () => undefined);
    const user = await fillValidFormAfterRender(onSubmit, "landing-contact");

    await user.click(screen.getByRole("button", { name: "Send message" }));

    await screen.findByText("Message sent");
    expect(onSubmit).toHaveBeenCalledWith(
      {
        name: "Sam",
        email: "sam@example.com",
        message: "Please contact me.",
        context: "landing-contact",
      },
      { signal: expect.any(AbortSignal) },
    );
    expect(screen.getByRole("textbox", { name: "Name" })).toHaveValue("");
    expect(screen.getByRole("textbox", { name: "Email" })).toHaveValue("");
    expect(screen.getByRole("textbox", { name: "Message" })).toHaveValue("");
  });

  it("preserves edits after failure and retries with the latest values", async () => {
    const onSubmit = vi
      .fn<Parameters<typeof ContactForm>[0]["onSubmit"]>()
      .mockRejectedValueOnce(new Error("provider detail"))
      .mockResolvedValueOnce(undefined);
    const user = await fillValidFormAfterRender(onSubmit);

    await user.click(screen.getByRole("button", { name: "Send message" }));

    expect(await screen.findByText("Message not sent")).toBeInTheDocument();
    expect(screen.queryByText("provider detail")).not.toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Message" })).toHaveValue(
      "Please contact me.",
    );

    await user.type(
      screen.getByRole("textbox", { name: "Message" }),
      " Updated.",
    );
    await user.click(screen.getByRole("button", { name: "Send message" }));

    await screen.findByText("Message sent");
    expect(onSubmit).toHaveBeenCalledTimes(2);
    expect(onSubmit.mock.calls[1][0].message).toBe(
      "Please contact me. Updated.",
    );
  });

  it("aborts pending submission on unmount and ignores its completion", async () => {
    const pending = deferred();
    let signal: AbortSignal | undefined;
    const onSubmit = vi.fn(
      (_values, options) => {
        signal = options.signal;
        return pending.promise;
      },
    );
    const user = await fillValidFormAfterRender(onSubmit);
    await user.click(screen.getByRole("button", { name: "Send message" }));

    const view = screen.getByRole("form", { name: "Contact form" });
    expect(signal?.aborted).toBe(false);
    view.parentElement?.remove();
    cleanup();
    expect(signal?.aborted).toBe(true);

    pending.resolve();
    await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
  });
});

async function fillValidFormAfterRender(
  onSubmit: Parameters<typeof ContactForm>[0]["onSubmit"],
  context?: string,
) {
  renderForm(onSubmit, { context });
  return fillValidForm();
}
