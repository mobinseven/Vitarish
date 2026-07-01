export type NavItem = { href: string; label: string };
export type SidebarLink = { href: string; label: string; icon: string };

export type AppShellProps = {
  children: React.ReactNode;
  navItems?: NavItem[];
  sidebar?: React.ReactNode;
  hideNavbar?: boolean;
  hideFooter?: boolean;
  onSearch?: (query: string) => void;
};

export type TopNavbarProps = {
  navItems?: NavItem[];
  onSearch?: (query: string) => void;
};

export type SidebarProps = {
  title: string;
  links: SidebarLink[];
  exactMatchHrefs?: string[];
};
