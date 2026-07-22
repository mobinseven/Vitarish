import { Button } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import { useLocalizedText } from "../../localization/index";

export interface ServiceTypeListItem {
  id: string;
  name: string;
  description?: string | null;
}
export interface ServiceTypeListProps {
  items: readonly ServiceTypeListItem[];
  onDelete: (serviceTypeId: string) => void;
}

export function ServiceTypeList({ items, onDelete }: ServiceTypeListProps) {
  const { t } = useLocalizedText();
  return (
    <ListView
      aria-label={t("clinic.serviceTypes.title")}
      renderEmptyState={() => (
        <ClinicEmptyState title={t("clinic.serviceTypes.empty")} />
      )}
    >
      {items.map((item) => (
        <ListView.Item key={item.id} id={item.id} textValue={item.name}>
          <ListView.ItemContent>
            <ListView.Title>{item.name}</ListView.Title>
            {item.description ? (
              <ListView.Description>{item.description}</ListView.Description>
            ) : null}
          </ListView.ItemContent>
          <ListView.ItemAction>
            <Button
              variant="ghost"
              className="text-danger"
              onPress={() => onDelete(item.id)}
            >
              {t("common.actions.delete")}
            </Button>
          </ListView.ItemAction>
        </ListView.Item>
      ))}
    </ListView>
  );
}
