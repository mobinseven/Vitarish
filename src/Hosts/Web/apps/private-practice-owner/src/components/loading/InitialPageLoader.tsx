"use client";

import { useEffect } from "react";
import { useAuthSession } from "@vitarish/auth";
import { LoadingScreen } from "./LoadingScreen";

export function InitialPageLoader() {
  const { isLoading } = useAuthSession();
  useEffect(() => {
    if (!isLoading) {
      document.getElementById("initial-page-loader")?.remove();
    }
  }, [isLoading]);

  return (
    <div
      id="initial-page-loader"
      className="fixed inset-0 z-[9999] bg-background"
    >
      <LoadingScreen />
    </div>
  );
}
