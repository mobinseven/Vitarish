"use client";

import { useState, useEffect, useRef } from "react";
import { Kbd, TextField, InputGroup } from "@heroui/react";
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
            <svg className="text-base text-muted pointer-events-none flex-shrink-0" aria-hidden="true" fill="none" focusable="false" height="1em" role="presentation" viewBox="0 0 24 24" width="1em">
              <path d="M11.5 21C16.7467 21 21 16.7467 21 11.5C21 6.25329 16.7467 2 11.5 2C6.25329 2 2 6.25329 2 11.5C2 16.7467 6.25329 21 11.5 21Z" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" />
              <path d="M22 22L20 20" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" />
            </svg>
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
