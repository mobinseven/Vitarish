# Editorial content display

`@vitarish/contents/editorial` provides a public reading layout with a centered title, body, metadata rail, responsive navigation and an expandable footer. It uses the host's HeroUI theme, typography and document direction. At narrow widths the metadata moves above the body.

The host supplies the brand, localized labels and copy, destination links, and any language control. `navigationLinks` appear directly on wide screens; optional `navigationGroups` provide dropdowns. On narrow screens both appear in one menu. Optional `footerGroups` provide separately expandable sections; otherwise `footerLinks` form one section. Group labels must be unique within each list, and their tooltips should be localized. Pass ordinary React content as children; headings, paragraphs, lists, links and media remain semantic HTML. Metadata values may be React nodes, for example a localized `time` element. Format dates using the host's shared localization conventions before passing them in.

The host also owns which content is safe to publish and must validate any external data and URLs before rendering them. This component does not fetch content, render raw HTML, decide publication or tenant access, or provide a CMS. A host can adapt the header and footer through the `--editorial-header-background`, `--editorial-header-foreground`, `--editorial-footer-background` and `--editorial-footer-foreground` theme variables. These should refer to that host's existing semantic tokens.

The Vitarish purpose page and Darman overview are examples of app-owned copy and navigation using the same layout.
