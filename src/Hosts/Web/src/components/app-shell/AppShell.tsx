import { TopNavbar } from "./TopNavbar";
import { Footer } from "./Footer";
import type { AppShellProps } from "./types";

export function AppShell({
  children,
  navItems,
  sidebar,
  hideNavbar = false,
  hideFooter = false,
  onSearch,
}: AppShellProps) {
  return (
    <div className="flex flex-col min-h-screen">
      {!hideNavbar && <TopNavbar navItems={navItems} onSearch={onSearch} />}
      <div className="flex flex-1">
        {sidebar}
        <main className="flex-1">{children}</main>
      </div>
      {!hideFooter && <Footer />}
    </div>
  );
}
