import {useState} from "react";
import Image from "next/image";
import {Tabs} from "@heroui/react";

import {FaIcon} from "@vitarish/web-ui";
import {
  figures,
  figureTitles,
  formatFigureLabel,
  formatFigureTitleLabel,
} from "@/features/landing/data/figures";
import {useLocalizedText} from "@/localization";

const snippets: Record<string, {lang: string; lines: string[]; raw: string}> = {
  python: {
    lang: "python",
    raw: 'from axon import Grid\n\ngrid = Grid(region="us-east-1")\n\n# deploy a model endpoint\nep = grid.deploy(\n    model="llama-3.1-70b",\n    replicas="auto",   # scales 0 -> 10k\n    accelerator="h100",\n)\n\nprint(ep.url)',
    lines: [
      "from axon import Grid",
      "",
      'grid = Grid(region="us-east-1")',
      "",
      "# deploy a model endpoint",
      "ep = grid.deploy(",
      '    model="llama-3.1-70b",',
      '    replicas="auto",   # scales 0 -> 10k',
      '    accelerator="h100",',
      ")",
      "",
      "print(ep.url)",
    ],
  },
  typescript: {
    lang: "typescript",
    raw: 'import { Grid } from "@axon/grid";\n\nconst grid = new Grid({ region: "us-east-1" });\n\n// deploy a model endpoint\nconst ep = await grid.deploy({\n  model: "llama-3.1-70b",\n  replicas: "auto",\n  accelerator: "h100",\n});\n\nconsole.log(ep.url);',
    lines: [
      'import { Grid } from "@axon/grid";',
      "",
      'const grid = new Grid({ region: "us-east-1" });',
      "",
      "// deploy a model endpoint",
      "const ep = await grid.deploy({",
      '  model: "llama-3.1-70b",',
      '  replicas: "auto",',
      '  accelerator: "h100",',
      "});",
      "",
      "console.log(ep.url);",
    ],
  },
  curl: {
    lang: "bash",
    raw: 'curl https://api.axongrid.dev/v1/deploy \\\n  -H "Authorization: Bearer $AXON_KEY" \\\n  -d model="llama-3.1-70b" \\\n  -d replicas="auto" \\\n  -d accelerator="h100"',
    lines: [
      "curl https://api.axongrid.dev/v1/deploy \\",
      '  -H "Authorization: Bearer $AXON_KEY" \\',
      '  -d model="llama-3.1-70b" \\',
      '  -d replicas="auto" \\',
      '  -d accelerator="h100"',
    ],
  },
};

function isComment(line: string) {
  return line.trim().startsWith("#") || line.trim().startsWith("//");
}

export function CodePreview() {
  const {t} = useLocalizedText();
  const [active, setActive] = useState<string>("python");
  const [copied, setCopied] = useState(false);

  const current = snippets[active];

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(current.raw);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1600);
    } catch {
      setCopied(false);
    }
  };

  return (
    <section className="relative scroll-mt-24 py-20 lg:py-28" id="api">
      <div className="mx-auto max-w-[1200px] px-6">
        <div className="max-w-2xl">
          <div className="flex items-center gap-3">
            <span className="bg-accent inline-block size-1.5" />
            <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">
              <span className="text-muted mr-2">{formatFigureLabel("08")}</span>
              {t("landing.codePreview.sectionLabel")}
            </span>
          </div>
          <h2 className="mt-6 text-4xl font-semibold tracking-tight sm:text-5xl">
            {t("landing.codePreview.heading")}
          </h2>
        </div>

        <div className="mt-12 grid items-center gap-10 lg:grid-cols-[1.1fr_0.9fr]">
          <div>
            <div className="border-border bg-surface relative overflow-hidden border">
              <div className="border-border flex items-center justify-between border-b px-4 py-3">
                <div className="flex items-center gap-1.5">
                  <span className="border-border size-2.5 rounded-full border" />
                  <span className="border-border size-2.5 rounded-full border" />
                  <span className="border-border size-2.5 rounded-full border" />
                  <span className="ml-3 font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">~/axon</span>
                </div>
                <button
                  aria-label={t("landing.codePreview.copyCode")}
                  className="text-muted hover:text-accent flex items-center gap-1.5 transition-colors"
                  onClick={copy}
                  type="button"
                >
                  <FaIcon icon={copied ? "circle-check" : "copy"} className="size-3.5" />
                  <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">{copied ? t("landing.codePreview.copied") : t("landing.codePreview.copy")}</span>
                </button>
              </div>

              <Tabs
                aria-label={t("landing.codePreview.language")}
                selectedKey={active}
                variant="secondary"
                onSelectionChange={(k) => setActive(String(k))}
              >
                <Tabs.ListContainer className="border-border border-b px-2">
                  <Tabs.List aria-label={t("landing.codePreview.language")}>
                    {Object.keys(snippets).map((key) => (
                      <Tabs.Tab key={key} id={key} className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88 px-3 py-2.5">
                        {key}
                        <Tabs.Indicator />
                      </Tabs.Tab>
                    ))}
                  </Tabs.List>
                </Tabs.ListContainer>

                {Object.entries(snippets).map(([key, snip]) => (
                  <Tabs.Panel key={key} id={key} className="p-0">
                    <pre className="overflow-x-auto px-4 py-5 font-mono text-[0.8rem] leading-relaxed">
                      <code>
                        {snip.lines.map((line, idx) => (
                          <div key={idx} className="flex">
                            <span className="text-muted/50 mr-4 w-5 shrink-0 select-none text-right">
                              {idx + 1}
                            </span>
                            <span className={isComment(line) ? "text-muted" : "text-foreground"}>
                              {line || "\u00A0"}
                            </span>
                          </div>
                        ))}
                      </code>
                    </pre>
                  </Tabs.Panel>
                ))}
              </Tabs>
            </div>
          </div>

          <div className="relative">
            <div className="border-border bg-surface relative overflow-hidden border">
              <div className="absolute left-3 top-3 z-10">
                <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">
                  {formatFigureTitleLabel("08", figureTitles.computeBoard)}
                </span>
              </div>
              <Image
                alt={figures.codeBoard.alt}
                className="relative z-[1] w-full object-cover"
                src={figures.codeBoard.src}
                width={800}
                height={600}
                unoptimized
              />
            </div>
            <div className="mt-3 flex items-center justify-between">
              <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">{t("landing.codePreview.revision")}</span>
              <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">{t("landing.codePreview.dimensions")}</span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
