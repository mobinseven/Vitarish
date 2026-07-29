"use client";
import {
  ContentEditor,
  ContentList,
  createContentsClient,
} from "@vitarish/contents-web";
import type {
  ContentItemType,
  ContentStatus,
} from "@vitarish/contents-web";
import { useRouter } from "next/navigation";
import { formatDateTime, useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

const client = createContentsClient();
const contentTypeKeys = {
  Article: "contents.type.Article",
  BlogPost: "contents.type.BlogPost",
  Announcement: "contents.type.Announcement",
  Page: "contents.type.Page",
  Guide: "contents.type.Guide",
} as const satisfies Record<ContentItemType, string>;
const contentStatusKeys = {
  Draft: "contents.status.Draft",
  Scheduled: "contents.status.Scheduled",
  Published: "contents.status.Published",
  Archived: "contents.status.Archived",
} as const satisfies Record<ContentStatus, string>;

export function ContentsPageSurface({ contentId }: { contentId?: string }) {
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const date = (value: string) => formatDateTime(value, locale);
  if (contentId || contentId === "")
    return (
      <ContentEditor
        client={client}
        contentId={contentId || undefined}
        formatDate={date}
        onDone={() => router.push(Routes.Contents.Index)}
        onCancel={() => router.push(Routes.Contents.Index)}
        text={{
          createTitle: t("contents.createTitle"),
          editTitle: t("contents.editTitle"),
          title: t("contents.fields.title"),
          slug: t("contents.fields.slug"),
          path: t("contents.fields.path"),
          excerpt: t("contents.fields.excerpt"),
          body: t("contents.fields.body"),
          locale: t("contents.fields.locale"),
          type: t("contents.fields.type"),
          status: t("contents.fields.status"),
          typeValue: (value) => t(contentTypeKeys[value]),
          statusValue: (value) => t(contentStatusKeys[value]),
          save: t("common.actions.save"),
          archive: t("contents.archive"),
          cancel: t("common.actions.cancel"),
          revisions: t("contents.revisions"),
          revision: (version, value) =>
            t("contents.revision", { version, date: value }),
          required: t("contents.required"),
          loading: t("common.states.loading"),
          failed: t("common.states.error"),
          saveFailed: t("contents.saveFailed"),
        }}
      />
    );
  return (
    <ContentList
      client={client}
      formatDate={date}
      onCreate={() => router.push(Routes.Contents.New)}
      onEdit={(item) => router.push(Routes.Contents.Edit(item.id))}
      text={{
        title: t("contents.title"),
        description: t("contents.description"),
        create: t("contents.create"),
        edit: t("common.actions.edit"),
        empty: t("contents.empty"),
        loading: t("common.states.loading"),
        failed: t("common.states.error"),
        status: (value) => t(contentStatusKeys[value]),
        updated: (value) => t("contents.updated", { date: value }),
      }}
    />
  );
}
