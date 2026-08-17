export function LandingFooter() {
  return (
    <footer className="flex flex-col gap-8 px-6 py-12 text-xs text-foreground/60 sm:flex-row sm:items-center sm:justify-between sm:px-10 lg:px-16">
      <span className="font-medium uppercase tracking-[0.3em] text-foreground">
        VITARISH
      </span>
      <nav aria-label="Footer navigation" className="flex gap-6">
        <a className="transition-colors hover:text-foreground" href="#">
          About
        </a>
        <a className="transition-colors hover:text-foreground" href="#">
          Contact
        </a>
        <a className="transition-colors hover:text-foreground" href="#">
          Privacy
        </a>
      </nav>
      <span>© 2026 Vitarish</span>
    </footer>
  );
}
