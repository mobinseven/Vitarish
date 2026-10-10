import { Button } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState, actionRoleClassName } from "@vitarish/ui";
import {
  formatDateTime,
  useLocalizedText,
  type MessageKey,
} from "../../localization";
const kindKeys: Record<number, MessageKey> = {
  0: "clinic.reminders.kindEmail",
  1: "clinic.reminders.kindSms",
};
export interface ReminderListItem {
  id: string;
  kind: number;
  subject?: string | null;
  recipient?: string | null;
  recipientUserId?: string | null;
  remindAt: string;
  isSent: boolean;
}
export interface ReminderListProps {
  items: readonly ReminderListItem[];
  onDelete: (id: string) => void;
  onMarkSent: (id: string) => void;
}
export function ReminderList({
  items,
  onDelete,
  onMarkSent,
}: ReminderListProps) {
  const { t, locale } = useLocalizedText();
  return (
    <ListView
      aria-label={t("clinic.reminders.title")}
      renderEmptyState={() => (
        <ClinicEmptyState title={t("clinic.reminders.empty")} />
      )}
    >
      {items.map((reminder) => {
        const subject = reminder.subject ?? t("clinic.reminders.noSubject");
        return (
          <ListView.Item key={reminder.id} id={reminder.id} textValue={subject}>
            <ListView.ItemContent>
              <ListView.Title>{subject}</ListView.Title>
              <ListView.Description>
                {t(kindKeys[reminder.kind] ?? "common.values.unknown")} /{" "}
                {reminder.recipient ??
                  reminder.recipientUserId ??
                  t("clinic.reminders.noRecipient")}
              </ListView.Description>
              <ListView.Description>
                {t("clinic.reminders.remindAt", {
                  value: formatDateTime(reminder.remindAt, locale),
                })}
              </ListView.Description>
            </ListView.ItemContent>
            <ListView.ItemAction className="flex gap-1">
              {!reminder.isSent ? (
                <Button
                  variant="ghost"
                  size="sm"
                  onPress={() => onMarkSent(reminder.id)}
                >
                  {t("clinic.reminders.markSent")}
                </Button>
              ) : null}
              <Button
                variant="ghost"
                className={`${actionRoleClassName("remove")} text-danger`}
                size="sm"
                onPress={() => onDelete(reminder.id)}
              >
                {t("common.actions.delete")}
              </Button>
            </ListView.ItemAction>
          </ListView.Item>
        );
      })}
    </ListView>
  );
}
