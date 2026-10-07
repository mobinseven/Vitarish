import React from "react";
import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PmEventAssistantSheet } from "./PmEventAssistantSheet";

const run = vi.fn();
const getAction = vi.fn();
const getConversation = vi.fn();
const deleteConversation = vi.fn();
const confirmAction = vi.fn();
const toSeaFileAttachment = vi.fn();
const uploadMutation = vi.fn();
const uploadMutationAsync = vi.fn();
const useFileUploadPolicy = vi.fn();
const checkFileForUpload = vi.fn();
const fileUploadAccept = vi.fn();

const uploadPolicy = {
  maximumContentBytes: 512 * 1024 * 1024,
  formats: [
    { mediaType: "image/png", extensions: [".png"] },
    { mediaType: "application/pdf", extensions: [".pdf"] },
  ],
};

vi.mock("@heroui/react", () => ({
  Button: ({
    children,
    onPress,
    isDisabled: _isDisabled,
    isPending: _isPending,
    ...props
  }: any) => (
    <button onClick={onPress} {...props}>
      {children}
    </button>
  ),
  Tooltip: Object.assign(({ children }: any) => <>{children}</>, {
    Content: ({ children }: any) => <>{children}</>,
  }),
  Label: ({ children }: any) => <label>{children}</label>,
  TextArea: (props: any) => <textarea {...props} />,
  TextField: ({ children, value, onChange }: any) => (
    <div onInput={(event: any) => onChange(event.target.value)}>{children}</div>
  ),
}));
vi.mock("@heroui-pro/react", () => {
  const Root = ({ children }: any) => <>{children}</>;
  return {
    Sheet: Object.assign(Root, {
      Backdrop: Root,
      Content: Root,
      Dialog: Root,
      Handle: Root,
      CloseTrigger: Root,
      Header: Root,
      Heading: Root,
      Body: Root,
      Footer: Root,
    }),
    ChatConversation: Object.assign(Root, { Content: Root }),
    ChatMessage: { User: Root, Assistant: Root, Bubble: Root, Content: Root },
  };
});
vi.mock("@vitarish/localization", () => ({
  formatFileSize: (bytes: number) => `${bytes} B`,
  formatList: (values: readonly string[]) => values.join(", "),
  useLocalizedText: () => ({ t: (key: string) => key, locale: "en" }),
}));
vi.mock("@vitarish/ui", () => ({ FaIcon: () => null }));
vi.mock("@vitarish/sea", () => {
  class FileUploadRejectedError extends Error {
    constructor(
      readonly reason: "typeNotAllowed" | "tooLarge",
      readonly maximumContentBytes: number,
    ) {
      super(reason);
      this.name = "FileUploadRejectedError";
    }
  }
  return {
    toSeaFileAttachment: (...args: unknown[]) => toSeaFileAttachment(...args),
    useCreateSeaFileAttachment: () => ({
      mutate: uploadMutation,
      mutateAsync: uploadMutationAsync,
      isPending: false,
      isError: false,
    }),
    useFileUploadPolicy: () => useFileUploadPolicy(),
    checkFileForUpload: (...args: unknown[]) => checkFileForUpload(...args),
    fileUploadAccept: (...args: unknown[]) => fileUploadAccept(...args),
    FileUploadRejectedError,
  };
});
vi.mock("@vitarish/intelligence", async () => {
  const actual = await vi.importActual<any>("@vitarish/intelligence");
  const AgentConversationComposer = ({
    value,
    onChange,
    onSubmit,
    label,
    submitLabel,
    toolbarStart,
  }: any) => (
    <div>
      {toolbarStart}
      <textarea
        aria-label={label}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
      <button
        aria-label="Dictate draft"
        onClick={() => onChange(`${value} dictated`)}
      >
        Dictate
      </button>
      <button onClick={() => onSubmit(value)}>{submitLabel}</button>
    </div>
  );
  return {
    ...actual,
    AgentConversationComposer,
    intelligenceRunAgentConversation: (...args: unknown[]) => run(...args),
  };
});
vi.mock("../api/generated/project-management/projectManagement", () => ({
  projectManagementGetEventAction: (...args: unknown[]) => getAction(...args),
  projectManagementGetEventConversation: (...args: unknown[]) =>
    getConversation(...args),
  projectManagementDeleteEventConversation: (...args: unknown[]) =>
    deleteConversation(...args),
  projectManagementConfirmEventAction: (...args: unknown[]) =>
    confirmAction(...args),
  projectManagementRejectEventAction: vi.fn(),
}));

describe("PmEventAssistantSheet", () => {
  const renderSheet = () => {
    const client = new QueryClient();
    const invalidate = vi.spyOn(client, "invalidateQueries");
    return {
      client,
      invalidate,
      ...render(
        <QueryClientProvider client={client}>
          <PmEventAssistantSheet eventId="event-1" onDeleted={vi.fn()} />
        </QueryClientProvider>,
      ),
    };
  };
  const seedAction = () => {
    run.mockResolvedValue({
      conversationId: "conversation-1",
      result: {
        assistant: { messages: ["I can update this event."] },
        preparedActions: [{ actionId: "action-1", operation: "update_event" }],
      },
    });
    getAction.mockResolvedValue({
      actionId: "action-1",
      status: "Proposed",
      review: {
        operation: "update_event",
        eventId: "event-1",
        attachmentId: null,
        proposedValuesJson: '{"title":"Updated"}',
      },
    });
  };
  beforeEach(() => {
    run.mockReset();
    getAction.mockReset();
    getConversation.mockReset().mockResolvedValue({ turns: [] });
    deleteConversation.mockReset();
    confirmAction.mockReset();
    toSeaFileAttachment.mockReset();
    uploadMutation.mockReset();
    uploadMutationAsync.mockReset();
    useFileUploadPolicy.mockReset().mockReturnValue({ data: uploadPolicy });
    checkFileForUpload.mockReset().mockReturnValue(undefined);
    fileUploadAccept
      .mockReset()
      .mockReturnValue(".png,image/png,.pdf,application/pdf");
  });
  it("submits only the current event as the conversation subject and renders server action review", async () => {
    seedAction();
    renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    await userEvent.type(screen.getByRole("textbox"), "Update its title");
    await userEvent.click(
      screen.getByRole("button", { name: "pm.assistantSend" }),
    );
    expect(run).toHaveBeenCalledWith(
      expect.objectContaining({
        agentKey: "project-management.event",
        subjectKind: "event",
        subjectId: "event-1",
        message: "Update its title",
      }),
      undefined,
      expect.any(AbortSignal),
    );
    expect(await screen.findByText("update_event")).toBeInTheDocument();
    expect(screen.getByText('{"title":"Updated"}')).toBeInTheDocument();
  });

  it("confirms a server-backed action and invalidates SEA event data", async () => {
    seedAction();
    confirmAction.mockResolvedValue({
      actionId: "action-1",
      status: "Completed",
      review: {
        operation: "update_event",
        eventId: "event-1",
        attachmentId: null,
        proposedValuesJson: "{}",
      },
    });
    const { invalidate } = renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    await userEvent.type(screen.getByRole("textbox"), "Update its title");
    await userEvent.click(
      screen.getByRole("button", { name: "pm.assistantSend" }),
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "pm.assistantConfirm" }),
    );
    expect(confirmAction).toHaveBeenCalledWith("event-1", "action-1");
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ["sea"] });
  });

  it("submits a dictated draft through the existing event flow", async () => {
    run.mockResolvedValue({
      conversationId: "conversation-1",
      result: { assistant: { messages: ["Done"] }, preparedActions: [] },
    });
    renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    await userEvent.click(
      screen.getByRole("button", { name: "Dictate draft" }),
    );
    await userEvent.click(
      screen.getByRole("button", { name: "pm.assistantSend" }),
    );
    expect(run).toHaveBeenCalledWith(
      expect.objectContaining({ message: "dictated" }),
      undefined,
      expect.any(AbortSignal),
    );
  });

  it("does not append a false assistant response after a failed run", async () => {
    run.mockRejectedValue(new Error("offline"));
    renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    await userEvent.type(screen.getByRole("textbox"), "Update its title");
    await userEvent.click(
      screen.getByRole("button", { name: "pm.assistantSend" }),
    );

    expect(
      await screen.findByRole("button", { name: "pm.assistantRetry" }),
    ).toBeInTheDocument();
    expect(
      screen.queryByText("pm.assistantNoResponse"),
    ).not.toBeInTheDocument();
  });

  it("keeps selected files in browser state until the user explicitly confirms upload", async () => {
    const { unmount } = renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    const selected = new File(["contents"], "brief.pdf", {
      type: "application/pdf",
    });
    fireEvent.change(document.querySelector('input[type="file"]')!, {
      target: { files: [selected] },
    });
    expect(await screen.findByText(/brief\.pdf/)).toBeInTheDocument();
    expect(toSeaFileAttachment).not.toHaveBeenCalled();
    unmount();
    renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    expect(screen.queryByText(/brief\.pdf/)).not.toBeInTheDocument();
  });

  it("limits the picker to the formats the upload policy accepts", async () => {
    fileUploadAccept.mockReturnValue("image/png,.png,application/pdf,.pdf");
    renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    expect(document.querySelector('input[type="file"]')).toHaveAttribute(
      "accept",
      "image/png,.png,application/pdf,.pdf",
    );
    expect(fileUploadAccept).toHaveBeenCalledWith(uploadPolicy);
  });

  it("drops a selected file the upload policy rejects and names the accepted types", async () => {
    const { FileUploadRejectedError } = await import("@vitarish/sea");
    checkFileForUpload.mockReturnValue(
      new FileUploadRejectedError(
        "typeNotAllowed",
        uploadPolicy.maximumContentBytes,
      ),
    );
    renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    const rejected = new File(["MZ"], "scan.exe");
    fireEvent.change(document.querySelector('input[type="file"]')!, {
      target: { files: [rejected] },
    });
    expect(checkFileForUpload).toHaveBeenCalledWith(uploadPolicy, rejected);
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "pm.fileTypeNotAllowed",
    );
    expect(screen.queryByText(/scan\.exe/)).not.toBeInTheDocument();
    expect(uploadMutationAsync).not.toHaveBeenCalled();
  });

  it("explains a server rejection and drops the file", async () => {
    const { FileUploadRejectedError } = await import("@vitarish/sea");
    toSeaFileAttachment.mockResolvedValue({
      eventId: "event-1",
      content: "file-1",
      contentType: "image/png",
      size: 4,
      kind: 1,
      fileName: "scan.png",
    });
    uploadMutationAsync.mockRejectedValue(
      new FileUploadRejectedError("tooLarge", uploadPolicy.maximumContentBytes),
    );
    renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    const selected = new File(["png"], "scan.png", { type: "image/png" });
    fireEvent.change(document.querySelector('input[type="file"]')!, {
      target: { files: [selected] },
    });
    expect(await screen.findByText(/scan\.png/)).toBeInTheDocument();
    await userEvent.click(
      screen.getByRole("button", { name: "pm.assistantUpload" }),
    );
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "pm.fileTooLarge",
    );
    expect(screen.queryByText(/scan\.png/)).not.toBeInTheDocument();
    expect(toSeaFileAttachment).toHaveBeenCalledWith(
      "event-1",
      selected,
      uploadPolicy,
    );
    expect(uploadMutationAsync).toHaveBeenCalledTimes(1);
  });

  it("requires confirmation before deleting the event conversation", async () => {
    renderSheet();
    await userEvent.click(screen.getByRole("button", { name: "pm.assistant" }));
    await userEvent.click(
      screen.getByRole("button", { name: "pm.assistantReset" }),
    );
    expect(deleteConversation).not.toHaveBeenCalled();
    expect(
      screen.getByRole("button", { name: "pm.assistantResetConfirm" }),
    ).toBeInTheDocument();
  });
});
