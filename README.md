# Public GitHub Pages

Edit the HTML and page styles directly. GitHub Pages serves the root of the publishing branch with Jekyll disabled. The site uses native HTML with HeroUI v3 component styles and local Vazirmatn. Font and style licenses are adjacent to their assets.

To refresh `heroui.css`, compile `heroui.source.css` with Tailwind CSS v4 and `@heroui/styles@3.2.7` installed. The stylesheet is generated and should be replaced as a whole.

From a disposable directory with those packages, use `npx @tailwindcss/cli@4 -i heroui.source.css -o heroui.css --minify`.

The Vitarish summary should be checked against the global ADR index in the main Vitarish repository whenever the decisions change. App journeys and support content are maintained by their owning documentation. Keep the former guide fragments reachable on both sites.
