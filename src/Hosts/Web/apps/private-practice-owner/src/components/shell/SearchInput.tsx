"use client";

import { useState, useEffect, useRef } from "react";
import { Kbd, TextField, InputGroup } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useLocalizedText } from "@/localization";

export interface SearchInputProps {
  onSearch?: (query: string) => void;
}

export function SearchInput({ onSearch }: SearchInputProps) {
  const [query, setQuery] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const { t } = useLocalizedText();

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") {
        e.preventDefault();
        inputRef.current?.focus();
      }
    }
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    onSearch?.(query.trim());
  }

  return (
    <form onSubmit={handleSubmit} className="hidden lg:flex">
      <TextField aria-label={t("appShell.search.label")} type="search">
        <InputGroup>
          <InputGroup.Prefix>
            <FaIcon
              aria-hidden="true"
              className="pointer-events-none flex-shrink-0 text-base text-muted"
              icon="magnifying-glass"
            />
          </InputGroup.Prefix>
          <InputGroup.Input
            ref={inputRef}
            className="text-sm"
            placeholder={t("appShell.search.placeholder")}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          <InputGroup.Suffix>
            <Kbd className="hidden lg:inline-flex">
              <Kbd.Abbr keyValue="command" />
              <Kbd.Content>K</Kbd.Content>
            </Kbd>
          </InputGroup.Suffix>
        </InputGroup>
      </TextField>
    </form>
  );
}
