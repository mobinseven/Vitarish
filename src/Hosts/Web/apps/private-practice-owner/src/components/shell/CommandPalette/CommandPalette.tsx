"use client";

import { Command, EmptyState } from "@heroui-pro/react";
import { Kbd, Button } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { useCommandPalette } from "./hooks/useCommandPalette";
import { useLocalizedText } from "@/localization";

export function CommandPalette() {
  const { isOpen, setOpen, commandGroups, executeCommand } =
    useCommandPalette();
  const { t } = useLocalizedText();

  return (
    <>
      <Button variant="secondary" onPress={() => setOpen(true)}>
        {t("commandPalette.open")}{" "}
        <Kbd>
          <Kbd.Abbr keyValue="command">
            <FaIcon icon="command" />
          </Kbd.Abbr>
          <Kbd.Content>
            <span className="text-[0.875rem]">K</span>
          </Kbd.Content>
        </Kbd>
      </Button>
      <Command>
        <Command.Backdrop isOpen={isOpen} onOpenChange={setOpen}>
          <Command.Container size="md">
            <Command.Dialog>
              <Command.InputGroup>
                <Command.InputGroup.Prefix>
                  <FaIcon
                    icon="magnifying-glass"
                    aria-hidden="true"
                    className="text-muted"
                  />
                </Command.InputGroup.Prefix>
                <Command.InputGroup.Input
                  placeholder={t("commandPalette.placeholder")}
                />
                <Command.InputGroup.ClearButton />
                <Command.InputGroup.Suffix>
                  <Kbd className="text-xs">
                    <Kbd.Content>Esc</Kbd.Content>
                  </Kbd>
                </Command.InputGroup.Suffix>
              </Command.InputGroup>
              <Command.List
                renderEmptyState={() => (
                  <EmptyState size="sm">
                    <EmptyState.Header>
                      <EmptyState.Media variant="icon">
                        <FaIcon icon="magnifying-glass" />
                      </EmptyState.Media>
                      <EmptyState.Title>
                        {t("commandPalette.noResults")}
                      </EmptyState.Title>
                    </EmptyState.Header>
                  </EmptyState>
                )}
              >
                {commandGroups.map((group) => (
                  <Command.Group key={group.id} heading={t(group.labelKey)}>
                    {group.commands.map((cmd) => (
                      <Command.Item
                        key={cmd.id}
                        id={cmd.id}
                        textValue={t(cmd.labelKey)}
                        onAction={() => executeCommand(cmd)}
                      >
                        <FaIcon icon={cmd.icon} />
                        <span>{t(cmd.labelKey)}</span>
                        {cmd.shortcut && (
                          <div className="ml-auto flex gap-1">
                            {cmd.shortcut.split(" ").map((key, index) => (
                              <kbd
                                key={`${key}-${index}`}
                                className="bg-default flex size-5 items-center justify-center rounded text-[11px] text-zinc-500 dark:text-zinc-400"
                              >
                                {key}
                              </kbd>
                            ))}
                          </div>
                        )}
                      </Command.Item>
                    ))}
                  </Command.Group>
                ))}
              </Command.List>
              <Command.Footer className="justify-between [&_kbd]:h-5 [&_kbd]:text-xs">
                <div className="flex items-center gap-3">
                  <div className="flex items-center gap-2">
                    <div className="flex items-center gap-0.5">
                      <Kbd className="text-xs">
                        <Kbd.Abbr keyValue="up" />
                      </Kbd>
                      <Kbd className="text-xs">
                        <Kbd.Abbr keyValue="down" />
                      </Kbd>
                    </div>
                    <span>{t("commandPalette.navigate")}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Kbd>
                      <Kbd.Abbr keyValue="enter" />
                    </Kbd>
                    <span>{t("commandPalette.select")}</span>
                  </div>
                </div>
              </Command.Footer>
            </Command.Dialog>
          </Command.Container>
        </Command.Backdrop>
      </Command>
    </>
  );
}
