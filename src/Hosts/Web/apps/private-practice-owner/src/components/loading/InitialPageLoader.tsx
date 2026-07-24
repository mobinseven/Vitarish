"use client";

import { useEffect } from "react";
import { LoadingSpinner } from "./LoadingSpinner";

export function InitialPageLoader() {
  useEffect(() => {
    document.getElementById("initial-page-loader")?.remove();
  }, []);

  return (
    <div
      id="initial-page-loader"
      className="fixed inset-0 z-[9999] flex items-center justify-center bg-background"
      aria-label="Loading"
      role="status"
    >
      <LoadingSpinner />
    </div>
  );
}
