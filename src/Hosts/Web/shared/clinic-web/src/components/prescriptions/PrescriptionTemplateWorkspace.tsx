"use client";

import { useMemo, useState } from "react";
import type { Key } from "@heroui/react";
import { Badge, Button, Chip, Input, Label, ListBox, SearchField, Select, Tag, TagGroup, TextArea, TextField, Tooltip } from "@heroui/react";
import { EmptyState, ItemCard, ItemCardGroup, PressableFeedback } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import { useLocalizedText } from "../../localization";

type PrescriptionCategory = "drug" | "lab" | "imaging" | "service" | "referral";

type PrescriptionItem = {
  id: string;
  category: PrescriptionCategory;
  name: string;
  code: string;
  dosage: string;
  quantity: string;
  duration: string;
};

type PrescriptionTemplate = {
  id: string;
  name: string;
  description: string;
  tags: string[];
  items: PrescriptionItem[];
  status: "active" | "archived";
};

const initialTemplates: PrescriptionTemplate[] = [
  {
    id: "respiratory",
    name: "Acute respiratory infection",
    description: "Common outpatient bundle for uncomplicated respiratory symptoms.",
    tags: ["Respiratory", "Outpatient"],
    status: "active",
    items: [
      { id: "r1", category: "drug", name: "Amoxicillin 500 mg capsule", code: "RX-02154", dosage: "Every 8 hours after food", quantity: "21", duration: "7" },
      { id: "r2", category: "drug", name: "Acetaminophen 500 mg tablet", code: "RX-00411", dosage: "Every 6 hours as needed", quantity: "20", duration: "5" },
      { id: "r3", category: "lab", name: "Complete blood count (CBC)", code: "LAB-11002", dosage: "", quantity: "1", duration: "" },
    ],
  },
  {
    id: "diabetes",
    name: "Type 2 diabetes follow-up",
    description: "Baseline medication and laboratory checks for routine follow-up.",
    tags: ["Diabetes", "Follow-up"],
    status: "active",
    items: [
      { id: "d1", category: "drug", name: "Metformin 500 mg tablet", code: "RX-00872", dosage: "Twice daily with meals", quantity: "60", duration: "30" },
      { id: "d2", category: "lab", name: "HbA1c", code: "LAB-22017", dosage: "", quantity: "1", duration: "" },
      { id: "d3", category: "lab", name: "Fasting blood glucose", code: "LAB-22003", dosage: "8–12 hours fasting", quantity: "1", duration: "" },
    ],
  },
  {
    id: "imaging",
    name: "Lower back pain assessment",
    description: "Initial diagnostic workup for persistent lower back pain.",
    tags: ["Musculoskeletal", "Diagnostics"],
    status: "active",
    items: [{ id: "i1", category: "imaging", name: "Lumbar spine X-ray", code: "IMG-30109", dosage: "AP and lateral views", quantity: "1", duration: "" }],
  },
];

const categories: { key: PrescriptionCategory; icon: string }[] = [
  { key: "drug", icon: "capsules" },
  { key: "lab", icon: "flask" },
  { key: "imaging", icon: "x-ray" },
  { key: "service", icon: "stethoscope" },
  { key: "referral", icon: "arrow-up-right-from-square" },
];

export function PrescriptionTemplateWorkspace() {
  const { t, direction } = useLocalizedText();
  const [templates, setTemplates] = useState(initialTemplates);
  const [selectedId, setSelectedId] = useState(initialTemplates[0].id);
  const [query, setQuery] = useState("");
  const [activeTag, setActiveTag] = useState<string | null>(null);
  const [expandedItem, setExpandedItem] = useState<string | null>("r1");
  const [saved, setSaved] = useState(false);

  const selected = templates.find((template) => template.id === selectedId) ?? templates[0];
  const tags = [...new Set(templates.flatMap((template) => template.tags))];
  const filteredTemplates = useMemo(() => templates.filter((template) => {
    const haystack = `${template.name} ${template.description} ${template.tags.join(" ")} ${template.items.map((item) => item.name).join(" ")}`.toLowerCase();
    return haystack.includes(query.toLowerCase()) && (!activeTag || template.tags.includes(activeTag));
  }), [activeTag, query, templates]);

  function updateSelected(patch: Partial<PrescriptionTemplate>) {
    setTemplates((current) => current.map((template) => template.id === selected.id ? { ...template, ...patch } : template));
    setSaved(false);
  }

  function updateItem(itemId: string, patch: Partial<PrescriptionItem>) {
    updateSelected({ items: selected.items.map((item) => item.id === itemId ? { ...item, ...patch } : item) });
  }

  function createTemplate() {
    const id = `template-${Date.now()}`;
    const template: PrescriptionTemplate = { id, name: t("prescriptionTemplates.newTemplateName"), description: "", tags: [], items: [], status: "active" };
    setTemplates((current) => [template, ...current]);
    setSelectedId(id);
    setExpandedItem(null);
  }

  function addItem() {
    const id = `item-${Date.now()}`;
    updateSelected({ items: [...selected.items, { id, category: "drug", name: "", code: "", dosage: "", quantity: "1", duration: "" }] });
    setExpandedItem(id);
  }

  return (
    <main className="mx-auto flex h-full min-h-0 w-full max-w-[1500px] flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8" dir={direction}>
      <header className="flex flex-col justify-between gap-4 pb-5 lg:flex-row lg:items-end">
        <div>
          <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-accent">{t("prescriptionTemplates.settingsLabel")}</p>
          <h1 className="text-3xl font-semibold tracking-[-0.03em] text-foreground">{t("prescriptionTemplates.title")}</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">{t("prescriptionTemplates.description")}</p>
        </div>
        <Button variant="primary" onPress={createTemplate}>
          <FaIcon icon="plus" aria-hidden="true" />
          {t("prescriptionTemplates.newTemplate")}
        </Button>
      </header>

      <div className="grid min-h-0 flex-1 gap-4 lg:grid-cols-[minmax(18rem,22rem)_minmax(0,1fr)]">
        <aside className="flex min-h-0 flex-col overflow-hidden rounded-2xl border border-separator bg-surface/70 shadow-[0_16px_40px_oklch(20%_0.03_240/10%)]">
          <div className=" p-4">
            <SearchField value={query} onChange={setQuery} variant="secondary">
              <Label className="sr-only">{t("prescriptionTemplates.search")}</Label>
              <SearchField.Group>
                <SearchField.SearchIcon />
                <SearchField.Input placeholder={t("prescriptionTemplates.searchPlaceholder")} />
                <SearchField.ClearButton />
              </SearchField.Group>
            </SearchField>
            <div className="mt-3 flex flex-wrap gap-2" aria-label={t("prescriptionTemplates.tags")}>
              <Tooltip><Button isIconOnly size="sm" variant={activeTag === null ? "primary" : "secondary"} aria-label={t("prescriptionTemplates.allTemplates")} onPress={() => setActiveTag(null)}><FaIcon icon="layer-group" aria-hidden="true" /></Button><Tooltip.Content>{t("prescriptionTemplates.allTemplates")}</Tooltip.Content></Tooltip>
              {tags.map((tag) => <Tooltip key={tag}><Button isIconOnly size="sm" variant={activeTag === tag ? "primary" : "secondary"} aria-label={tag} onPress={() => setActiveTag(tag)}><FaIcon icon="tag" aria-hidden="true" /></Button><Tooltip.Content>{tag}</Tooltip.Content></Tooltip>)}
            </div>
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto p-2">
            {filteredTemplates.length === 0 ? (
              <EmptyState size="sm" className="m-2">
                <EmptyState.Header>
                  <EmptyState.Media variant="icon">
                    <FaIcon icon="folder-open" aria-hidden="true" />
                  </EmptyState.Media>
                  <EmptyState.Title>{t("prescriptionTemplates.noResults")}</EmptyState.Title>
                  <EmptyState.Description>{t("prescriptionTemplates.noResultsDescription")}</EmptyState.Description>
                </EmptyState.Header>
              </EmptyState>
            ) : filteredTemplates.map((template) => (
              <button key={template.id} type="button" onClick={() => { setSelectedId(template.id); setExpandedItem(template.items[0]?.id ?? null); }} className={`mb-1 w-full cursor-pointer text-start ${template.id === selected.id ? "rounded-xl ring-2 ring-accent/40" : ""}`}>
                <ItemCard variant={template.id === selected.id ? "secondary" : "transparent"} className="relative overflow-hidden">
                  <PressableFeedback.Highlight />
                  <ItemCard.Content>
                    <ItemCard.Title className="flex items-center gap-2">
                      <span className="line-clamp-2 text-sm font-semibold">{template.name}</span>
                      <Badge variant="primary">{template.items.length}</Badge>
                    </ItemCard.Title>
                    <ItemCard.Description className="line-clamp-2">{template.description || t("prescriptionTemplates.noDescription")}</ItemCard.Description>
                    <div className="mt-2 flex flex-wrap gap-1">{template.tags.map((tag) => <Chip key={tag} size="sm" variant="secondary">{tag}</Chip>)}</div>
                  </ItemCard.Content>
                </ItemCard>
              </button>
            ))}
          </div>
        </aside>

        <section className="min-h-0 overflow-y-auto bg-surface/70 shadow-[0_16px_40px_oklch(20%_0.03_240/10%)]">
          <div className="p-5 sm:p-6">
            <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
              <div className="min-w-0 flex-1">
                <TextField value={selected.name} onChange={(value) => updateSelected({ name: value })}>
                  <Label className="text-xs font-semibold uppercase tracking-[0.14em] text-muted">{t("prescriptionTemplates.name")}</Label>
                  <Input className="mt-1 text-xl font-semibold" />
                </TextField>
                <TextField value={selected.description} onChange={(value) => updateSelected({ description: value })}>
                  <Label className="mt-4 text-xs font-semibold uppercase tracking-[0.14em] text-muted">{t("prescriptionTemplates.descriptionLabel")}</Label>
                  <TextArea className="mt-1" placeholder={t("prescriptionTemplates.descriptionPlaceholder")} />
                </TextField>
              </div>
              <div className="flex shrink-0 items-center gap-2">
                <Chip color="success" variant="soft">{t("prescriptionTemplates.active")}</Chip>
                <Tooltip><Button isIconOnly variant="primary" aria-label={t("prescriptionTemplates.save")} onPress={() => setSaved(true)}><FaIcon icon={saved ? "check" : "floppy-disk"} aria-hidden="true" /></Button><Tooltip.Content>{saved ? t("prescriptionTemplates.saved") : t("prescriptionTemplates.saveTooltip")}</Tooltip.Content></Tooltip>
              </div>
            </div>
            <div className="mt-5 flex flex-wrap items-center gap-2">
              <span className="text-xs font-semibold text-muted">{t("prescriptionTemplates.tags")}</span>
              <TagGroup
                selectionMode="none"
                size="sm"
                onRemove={(keys: Set<Key>) => updateSelected({ tags: selected.tags.filter((tag) => !keys.has(tag)) })}
              >
                <TagGroup.List>
                  {selected.tags.map((tag) => (
                    <Tag key={tag} id={tag} textValue={tag}>{tag}</Tag>
                  ))}
                </TagGroup.List>
              </TagGroup>
              <Tooltip><Button isIconOnly variant="ghost" size="sm" aria-label={t("prescriptionTemplates.addTag")} onPress={() => updateSelected({ tags: [...selected.tags, t("prescriptionTemplates.newTag")] })} className="rounded-md border border-dashed text-xs font-medium text-muted hover:border-accent hover:text-accent"><FaIcon icon="plus" aria-hidden="true" /></Button><Tooltip.Content>{t("prescriptionTemplates.addTagTooltip")}</Tooltip.Content></Tooltip>
            </div>
          </div>

          <div className="p-5 sm:p-6">
            {selected.items.length === 0 ? (
              <EmptyState className="rounded-xl border border-dashed p-10">
                <EmptyState.Header>
                  <EmptyState.Media variant="icon"><FaIcon icon="prescription-bottle-medical" aria-hidden="true" /></EmptyState.Media>
                  <EmptyState.Title>{t("prescriptionTemplates.emptyItems")}</EmptyState.Title>
                  <EmptyState.Description>{t("prescriptionTemplates.emptyItemsDescription")}</EmptyState.Description>
                </EmptyState.Header>
                <EmptyState.Content>
                  <Button variant="secondary" onPress={addItem}><FaIcon icon="plus" aria-hidden="true" />{t("prescriptionTemplates.addItem")}</Button>
                </EmptyState.Content>
              </EmptyState>
            ) : (
              <ItemCardGroup variant="transparent">
                <ItemCardGroup.Header className="flex items-end justify-between gap-3">
                  <div>
                    <ItemCardGroup.Title>{t("prescriptionTemplates.itemsTitle")}</ItemCardGroup.Title>
                    <ItemCardGroup.Description>{t("prescriptionTemplates.itemsDescription")}</ItemCardGroup.Description>
                  </div>
                  <Button variant="secondary" onPress={addItem}><FaIcon icon="plus" aria-hidden="true" />{t("prescriptionTemplates.addItem")}</Button>
                </ItemCardGroup.Header>
                {selected.items.map((item, index) => {
                    const category = categories.find((candidate) => candidate.key === item.category);
                    const isOpen = expandedItem === item.id;
                    return (
                      <div key={item.id}>
                        <ItemCard<"button">
                          className={`relative w-full cursor-pointer overflow-hidden ${isOpen ? "bg-accent/[0.035]" : ""}`}
                          onClick={() => setExpandedItem(isOpen ? null : item.id)}
                          render={(props) => <button type="button" {...props} />}
                        >
                          <PressableFeedback.Ripple />
                          <ItemCard.Icon>
                            <span className="flex size-8 items-center justify-center rounded-lg bg-default text-xs font-semibold text-default-foreground">{index + 1}</span>
                          </ItemCard.Icon>
                          <ItemCard.Content>
                            <ItemCard.Title className="flex items-center gap-2">
                              <FaIcon icon={category?.icon ?? "pills"} aria-hidden="true" className="text-accent" />
                              <span className="truncate">{item.name || t("prescriptionTemplates.unnamedItem")}</span>
                            </ItemCard.Title>
                            <ItemCard.Description className="truncate">
                              {item.code || t("prescriptionTemplates.referencePending")} {item.quantity ? `· ${t("prescriptionTemplates.quantityShort")} ${item.quantity}` : ""}
                            </ItemCard.Description>
                          </ItemCard.Content>
                          <ItemCard.Action>
                            <FaIcon icon={isOpen ? "angle-up" : "angle-down"} className="text-muted" aria-hidden="true" />
                          </ItemCard.Action>
                        </ItemCard>
                        {isOpen && (
                          <div className="grid gap-4 p-4 sm:grid-cols-2 lg:grid-cols-3">
                            <TextField value={item.name} onChange={(value) => updateItem(item.id, { name: value })}><Label>{t("prescriptionTemplates.itemName")}</Label><Input placeholder={t("prescriptionTemplates.itemNamePlaceholder")} /></TextField>
                            <TextField value={item.code} onChange={(value) => updateItem(item.id, { code: value })}><Label>{t("prescriptionTemplates.referenceCode")}</Label><Input placeholder={t("prescriptionTemplates.referencePlaceholder")} /></TextField>
                            <div>
                              <Label>{t("prescriptionTemplates.category")}</Label>
                              <Select
                                className="mt-1"
                                value={item.category}
                                onChange={(value) => updateItem(item.id, { category: value as PrescriptionCategory })}
                              >
                                <Select.Trigger>
                                  <Select.Value />
                                  <Select.Indicator />
                                </Select.Trigger>
                                <Select.Popover>
                                  <ListBox>
                                    {categories.map((cat) => (
                                      <ListBox.Item key={cat.key} id={cat.key} textValue={t(`prescriptionTemplates.categories.${cat.key}`)}>
                                        {t(`prescriptionTemplates.categories.${cat.key}`)}
                                        <ListBox.ItemIndicator />
                                      </ListBox.Item>
                                    ))}
                                  </ListBox>
                                </Select.Popover>
                              </Select>
                            </div>
                            <TextField value={item.dosage} onChange={(value) => updateItem(item.id, { dosage: value })} className="sm:col-span-2"><Label>{t("prescriptionTemplates.dosage")}</Label><Input placeholder={t("prescriptionTemplates.dosagePlaceholder")} /></TextField>
                            <div className="grid grid-cols-2 gap-3"><TextField value={item.quantity} onChange={(value) => updateItem(item.id, { quantity: value })}><Label>{t("prescriptionTemplates.quantity")}</Label><Input inputMode="numeric" /></TextField><TextField value={item.duration} onChange={(value) => updateItem(item.id, { duration: value })}><Label>{t("prescriptionTemplates.duration")}</Label><Input inputMode="numeric" placeholder="—" /></TextField></div>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </ItemCardGroup>
              )}
          </div>
        </section>
      </div>
    </main>
  );
}
