"use client";
import { Button, Tooltip } from "@heroui/react";
import { useContentItems } from "./useContents";
import type { ContentItem, ContentStatus, ContentsClient } from "./types";

export interface ContentListText {
  title: string;
  description: string;
  create: string;
  edit: string;
  empty: string;
  loading: string;
  failed: string;
  status: (value: ContentStatus) => string;
  updated: (value: string) => string;
}
export function ContentList({
  client,
  text,
  formatDate,
  onCreate,
  onEdit,
}: {
  client: ContentsClient;
  text: ContentListText;
  formatDate: (value: string) => string;
  onCreate: () => void;
  onEdit: (item: ContentItem) => void;
}) {
  const query = useContentItems(client);
  return (
    <section className="flex flex-col gap-6" aria-labelledby="contents-title">
      <header className="flex items-start justify-between gap-4">
        <div>
          <h1 id="contents-title" className="text-2xl font-semibold">
            {text.title}
          </h1>
          <p className="text-muted">{text.description}</p>
        </div>
        <Tooltip>
          <Button onPress={onCreate} aria-label={text.create}>
            <i className="fa-light fa-plus" aria-hidden="true" />
            <span>{text.create}</span>
          </Button>
          <Tooltip.Content arrowBoundaryOffset={0}>{text.create}</Tooltip.Content>
        </Tooltip>
      </header>
      {query.isLoading ? (
        <p role="status">{text.loading}</p>
      ) : query.isError ? (
        <p role="alert">{text.failed}</p>
      ) : query.data?.length === 0 ? (
        <p>{text.empty}</p>
      ) : (
        <div className="grid gap-3">
          {query.data?.map((item) => (
            <article
              key={item.id}
              className="rounded-xl border border-default-200 p-4"
            >
              <div className="flex items-center justify-between gap-4">
                <div>
                  <h2 className="font-semibold">{item.title}</h2>
                  <p className="text-sm text-muted">
                    {item.canonicalPath} · {text.status(item.status)} ·{" "}
                    {text.updated(formatDate(item.updatedAt ?? item.createdAt))}
                  </p>
                </div>
                <Tooltip>
                  <Button
                    variant="secondary"
                    onPress={() => onEdit(item)}
                    aria-label={text.edit}
                  >
                    <i
                      className="fa-light fa-pen-to-square"
                      aria-hidden="true"
                    />
                    <span>{text.edit}</span>
                  </Button>
                  <Tooltip.Content arrowBoundaryOffset={0}>{text.edit}</Tooltip.Content>
                </Tooltip>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
