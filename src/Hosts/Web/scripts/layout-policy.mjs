import { existsSync, readFileSync, readdirSync } from "node:fs";
import { relative, resolve } from "node:path";

import postcss from "postcss";
import ts from "typescript";

export const layoutRuleIds = [
  "raw-main",
  "raw-viewport",
  "direct-applayout",
  "legacy-outer-container",
  "layout-css-import",
  "hand-rolled-surface",
  "raw-color",
];

const layoutRuleIdSet = new Set(layoutRuleIds);
const layoutStylesheetImport = "@vitarish/ui/layout.css";

const ruleGuidance = {
  "raw-main": {
    reason: "Application and feature sources must not choose the main landmark directly.",
    preferred:
      "Use a Standalone*Page owner, or use a landmark-neutral region inside DashboardFrame.",
  },
  "raw-viewport": {
    reason: "Application and feature sources must not own full-viewport block sizing.",
    preferred:
      "Select StandaloneCenteredPage, StandaloneImmersivePage, DashboardFrame, or ImmersiveRegion.",
  },
  "direct-applayout": {
    reason: "HeroUI AppLayout mechanics have one Vitarish adapter owner.",
    preferred: "Use DashboardFrame through the app's DashboardShell.",
  },
  "legacy-outer-container": {
    reason: "This outer width and gutter signature duplicates the shared layout policy.",
    preferred: "Use PageContainer or ContainedPage with an explicit PageWidth.",
  },
  "layout-css-import": {
    reason: "Each app must import the shared layout stylesheet exactly once at its global root.",
    preferred: `Import ${layoutStylesheetImport} once from the app's root globals.css.`,
  },
  "hand-rolled-surface": {
    reason:
      "Content sections must not recreate a surface from rounded, border or background, and padding utilities.",
    preferred:
      'Use a section surface: <Surface className="flex flex-col gap-6 rounded-3xl p-6"> with an h2 section header (see PlatformSecurityAuditReviewPage).',
  },
  "raw-color": {
    reason:
      "Feature code must color through a registered role, not a palette color or color literal.",
    preferred:
      "Use a palette token (neutral, accent, chroma), a status token, or a button class from the owner (docs/hosts/web/color-roles.md).",
  },
};

const viewportUtilityPattern =
  /(?:^|\s)(?:(?:h|min-h|max-h|size)-(?:screen|svh|dvh|lvh)|(?:h|min-h|max-h|size)-\[[^\]]*100(?:s|d|l)?vh[^\]]*\])(?=\s|$)/;
const viewportCssPattern = /\b100(?:s|d|l)?vh\b/i;
const sectioningElements = new Set(["section", "article", "aside", "form"]);
const surfaceRoundedPattern = /^rounded(?!-none$)(?:-.+)?$/;
const surfaceBorderPattern = /^border(?:-(?![tbselrxy](?:-|$)|0$|none$|hidden$).+)?$/;
const surfaceBackgroundPattern = /^bg-(?!transparent$|none$).+$/;
const surfacePaddingPattern = /^p[xy]?-.+$/;
const paletteColorUtilityPattern =
  /^-?(?:bg|text|border(?:-[xytrblse])?|ring|ring-offset|outline|decoration|divide|fill|stroke|shadow|inset-shadow|drop-shadow|from|via|to|accent|caret|placeholder)-(?:slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose)-\d{2,3}(?:\/.+)?$/;
const arbitraryColorUtilityPattern =
  /\[(?:[^\]]*[_(,])?(?:#[0-9a-f]{3,8}|(?:rgba?|hsla?|oklch|oklab|lab|lch|hwb|color)\()/i;
const cssColorLiteralPattern = /#[0-9a-f]{3,8}\b|\b(?:rgba?|hsla?|oklch|oklab|lab|lch|hwb|color)\(/i;
const legacyContainerTokenSets = [
  ["mx-auto", "max-w-[1200px]", "px-6"],
  ["mx-auto", "max-w-7xl", "px-5", "pb-10", "pt-4"],
];

export function createProductionLayoutPolicyConfig(webRoot) {
  return {
    rootDir: webRoot,
    surfaceIncludeRoots: discoverPackageSourceRoots(webRoot),
    surfaceExcludePrefixes: [
      // Layout geometry harness for the contextual-aside visual snapshot, not product UI.
      "apps/private-practice-owner/src/app/layout-fixture/",
    ],
    includeRoots: [
      "apps/private-practice-owner/src",
      "apps/knowledge-qa-demo/src",
      "shared/auth/src",
      "shared/clinic/src",
      "shared/journey/src",
      "shared/knowledge/src",
      "shared/sea/src",
      "shared/ui/src",
    ],
    excludePrefixes: [
      "shared/ui/src/__tests__/",
    ],
    // Packages migrated to the color roles (ADR-0014); add a package when it migrates.
    colorIncludeRoots: [
      "apps/private-practice-owner/src",
      "shared/clinic/src",
    ],
    colorExcludePrefixes: [
      // Scroll-reveal harness with a drawn device mockup, not product UI.
      "apps/private-practice-owner/src/app/viewport-reveal-test/",
    ],
    owners: {
      "raw-main": ["shared/ui/src/layout/LayoutPrimitives.tsx"],
      "raw-viewport": ["shared/ui/src/layout.css"],
      "direct-applayout": ["shared/ui/src/shell/DashboardFrame.tsx"],
      "legacy-outer-container": [],
      "layout-css-import": [],
      "hand-rolled-surface": [],
      // Host theme files own the theme's token values.
      "raw-color": [
        "apps/private-practice-owner/src/app/globals.css",
        "apps/private-practice-owner/src/app/theme.css",
      ],
    },
    expectedGlobalStylesheets: [
      "apps/private-practice-owner/src/app/globals.css",
      "apps/knowledge-qa-demo/src/app/globals.css",
    ],
    exceptionsPath: "scripts/layout-lint-exceptions.json",
  };
}

export function validateLayoutExceptions(exceptions, now = new Date()) {
  if (!Array.isArray(exceptions)) {
    return ["The exception document must be a JSON array."];
  }

  const requiredFields = [
    "rule",
    "path",
    "owner",
    "reason",
    "trackingIssue",
    "expiresOn",
  ];
  const problems = [];

  exceptions.forEach((exception, index) => {
    if (!exception || typeof exception !== "object" || Array.isArray(exception)) {
      problems.push(`Exception ${index + 1} must be an object.`);
      return;
    }

    for (const field of requiredFields) {
      if (typeof exception[field] !== "string" || exception[field].trim() === "") {
        problems.push(`Exception ${index + 1} requires a non-empty ${field}.`);
      }
    }

    if (typeof exception.rule === "string" && !layoutRuleIdSet.has(exception.rule)) {
      problems.push(`Exception ${index + 1} uses unknown rule ${exception.rule}.`);
    }

    if (
      typeof exception.path === "string" &&
      (exception.path.includes("*") || exception.path.includes("\\"))
    ) {
      problems.push(`Exception ${index + 1} path must be an exact normalized path.`);
    }

    if (typeof exception.expiresOn === "string") {
      const expiry = /^\d{4}-\d{2}-\d{2}$/.test(exception.expiresOn)
        ? new Date(`${exception.expiresOn}T23:59:59.999Z`)
        : null;
      if (!expiry || Number.isNaN(expiry.valueOf())) {
        problems.push(`Exception ${index + 1} expiresOn must use YYYY-MM-DD.`);
      } else if (expiry < now) {
        problems.push(`Exception ${index + 1} expired on ${exception.expiresOn}.`);
      }
    }
  });

  return problems;
}

export function lintLayoutPolicy(config) {
  const rootDir = resolve(config.rootDir);
  const owners = normalizeOwners(config.owners ?? {});
  const excluded = (config.excludePrefixes ?? []).map(normalizePath);
  const files = collectSourceFiles(rootDir, config.includeRoots, excluded);
  const surfaceFiles = collectSourceFiles(rootDir, config.surfaceIncludeRoots ?? [], [
    ...excluded,
    ...(config.surfaceExcludePrefixes ?? []).map(normalizePath),
  ]).filter(isSurfacePolicySource);
  const diagnostics = [];
  const stylesheetImports = new Map();
  const colorRoots = (config.colorIncludeRoots ?? []).map(normalizePath);
  const colorExcluded = (config.colorExcludePrefixes ?? []).map(normalizePath);
  const isColorPolicyPath = (path) =>
    !isOwner(owners, "raw-color", path) &&
    colorRoots.some((root) => path.startsWith(`${root}/`)) &&
    !colorExcluded.some((prefix) => path.startsWith(prefix));

  for (const file of files) {
    const path = normalizePath(relative(rootDir, file));
    const source = readFileSync(file, "utf8");
    const checkColor = isColorPolicyPath(path);

    if (file.endsWith(".tsx") || file.endsWith(".ts")) {
      lintTypeScriptSource({ checkColor, diagnostics, owners, path, source });
    } else if (file.endsWith(".css")) {
      lintCssSource({ checkColor, diagnostics, owners, path, source, stylesheetImports });
    }
  }

  for (const file of surfaceFiles) {
    const path = normalizePath(relative(rootDir, file));
    if (isOwner(owners, "hand-rolled-surface", path)) continue;
    lintSurfaceSource({ diagnostics, path, source: readFileSync(file, "utf8") });
  }

  lintStylesheetOwnership({
    diagnostics,
    expectedGlobalStylesheets: (config.expectedGlobalStylesheets ?? []).map(normalizePath),
    stylesheetImports,
  });

  const exceptions = readExceptions(rootDir, config.exceptionsPath);
  const exceptionProblems = validateLayoutExceptions(exceptions, config.now ?? new Date());
  if (exceptionProblems.length > 0) {
    throw new Error(exceptionProblems.join("\n"));
  }

  const suppressed = [];
  const activeDiagnostics = diagnostics.filter((diagnostic) => {
    const exception = exceptions.find(
      (candidate) => candidate.rule === diagnostic.rule && candidate.path === diagnostic.path
    );
    if (!exception) return true;
    suppressed.push({ diagnostic, exception });
    return false;
  });

  return {
    diagnostics: activeDiagnostics.sort(compareDiagnostics),
    scannedFiles: new Set([...files, ...surfaceFiles]).size,
    suppressed,
  };
}

function lintTypeScriptSource({ checkColor, diagnostics, owners, path, source }) {
  const sourceFile = ts.createSourceFile(
    path,
    source,
    ts.ScriptTarget.Latest,
    true,
    path.endsWith(".tsx") ? ts.ScriptKind.TSX : ts.ScriptKind.TS
  );

  function visit(node) {
    if (
      (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) &&
      ts.isIdentifier(node.tagName) &&
      node.tagName.text === "main" &&
      !isOwner(owners, "raw-main", path)
    ) {
      addTypeScriptDiagnostic(diagnostics, sourceFile, path, "raw-main", node.tagName);
    }

    if (
      ts.isImportDeclaration(node) &&
      isDirectAppLayoutImport(node) &&
      !isOwner(owners, "direct-applayout", path)
    ) {
      addTypeScriptDiagnostic(
        diagnostics,
        sourceFile,
        path,
        "direct-applayout",
        node.moduleSpecifier
      );
    }

    if (isClassValue(node)) {
      const value = getLiteralText(node);
      if (value !== null) {
        if (viewportUtilityPattern.test(value) && !isOwner(owners, "raw-viewport", path)) {
          addTypeScriptDiagnostic(diagnostics, sourceFile, path, "raw-viewport", node);
        }

        const tokens = new Set(value.split(/\s+/).filter(Boolean));
        if (checkColor && [...tokens].some(isRawColorUtility)) {
          addTypeScriptDiagnostic(diagnostics, sourceFile, path, "raw-color", node);
        }

        if (
          legacyContainerTokenSets.some((required) =>
            required.every((token) => tokens.has(token))
          ) &&
          !isOwner(owners, "legacy-outer-container", path)
        ) {
          addTypeScriptDiagnostic(
            diagnostics,
            sourceFile,
            path,
            "legacy-outer-container",
            node
          );
        }
      }
    }

    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
}

function lintSurfaceSource({ diagnostics, path, source }) {
  const sourceFile = ts.createSourceFile(
    path,
    source,
    ts.ScriptTarget.Latest,
    true,
    ts.ScriptKind.TSX
  );

  function visit(node) {
    if (
      (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) &&
      ts.isIdentifier(node.tagName) &&
      sectioningElements.has(node.tagName.text)
    ) {
      const classAttribute = node.attributes.properties.find(
        (attribute) =>
          ts.isJsxAttribute(attribute) && attribute.name.getText() === "className"
      );
      if (
        classAttribute?.initializer &&
        isHandRolledSurface(collectClassTokens(classAttribute.initializer))
      ) {
        addTypeScriptDiagnostic(
          diagnostics,
          sourceFile,
          path,
          "hand-rolled-surface",
          node.tagName
        );
      }
    }

    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
}

function isRawColorUtility(token) {
  const utility = token.slice(token.lastIndexOf(":") + 1).replace(/^!/, "");
  return paletteColorUtilityPattern.test(utility) || arbitraryColorUtilityPattern.test(utility);
}

function collectClassTokens(initializer) {
  const tokens = new Set();

  function visit(node) {
    const value = getLiteralText(node);
    if (value !== null) {
      for (const token of value.split(/\s+/)) {
        if (token) tokens.add(token.slice(token.lastIndexOf(":") + 1));
      }
    }
    ts.forEachChild(node, visit);
  }

  visit(initializer);
  return tokens;
}

function isHandRolledSurface(tokens) {
  const values = [...tokens];
  return (
    values.some((token) => surfaceRoundedPattern.test(token)) &&
    values.some((token) => surfacePaddingPattern.test(token)) &&
    values.some(
      (token) => surfaceBorderPattern.test(token) || surfaceBackgroundPattern.test(token)
    )
  );
}

function lintCssSource({ checkColor, diagnostics, owners, path, source, stylesheetImports }) {
  const root = postcss.parse(source, { from: path });

  root.walkDecls((declaration) => {
    if (viewportCssPattern.test(declaration.value) && !isOwner(owners, "raw-viewport", path)) {
      addCssDiagnostic(diagnostics, path, "raw-viewport", declaration);
    }
    if (checkColor && cssColorLiteralPattern.test(declaration.value)) {
      addCssDiagnostic(diagnostics, path, "raw-color", declaration);
    }
  });

  root.walkAtRules("import", (atRule) => {
    if (!isLayoutStylesheetImport(atRule.params)) return;
    const location = atRule.source?.start ?? { line: 1, column: 1 };
    const imports = stylesheetImports.get(path) ?? [];
    imports.push(location);
    stylesheetImports.set(path, imports);
  });
}

function lintStylesheetOwnership({
  diagnostics,
  expectedGlobalStylesheets,
  stylesheetImports,
}) {
  const expected = new Set(expectedGlobalStylesheets);

  for (const path of expectedGlobalStylesheets) {
    const imports = stylesheetImports.get(path) ?? [];
    if (imports.length === 1) continue;

    const location = imports[1] ?? imports[0] ?? { line: 1, column: 1 };
    diagnostics.push(createDiagnostic("layout-css-import", path, location.line, location.column));
  }

  for (const [path, imports] of stylesheetImports) {
    if (expected.has(path)) continue;
    for (const location of imports) {
      diagnostics.push(createDiagnostic("layout-css-import", path, location.line, location.column));
    }
  }
}

function addTypeScriptDiagnostic(diagnostics, sourceFile, path, rule, node) {
  const location = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
  diagnostics.push(createDiagnostic(rule, path, location.line + 1, location.character + 1));
}

function addCssDiagnostic(diagnostics, path, rule, node) {
  const location = node.source?.start ?? { line: 1, column: 1 };
  diagnostics.push(createDiagnostic(rule, path, location.line, location.column));
}

function createDiagnostic(rule, path, line, column) {
  return {
    rule,
    path,
    line,
    column,
    reason: ruleGuidance[rule].reason,
    preferred: ruleGuidance[rule].preferred,
  };
}

function isDirectAppLayoutImport(node) {
  if (!ts.isStringLiteral(node.moduleSpecifier)) return false;
  const moduleName = node.moduleSpecifier.text;
  if (moduleName === "@heroui-pro/react/app-layout") return true;
  if (moduleName !== "@heroui-pro/react") return false;

  const bindings = node.importClause?.namedBindings;
  return (
    bindings !== undefined &&
    ts.isNamedImports(bindings) &&
    bindings.elements.some(
      (element) => (element.propertyName?.text ?? element.name.text) === "AppLayout"
    )
  );
}

function isClassValue(node) {
  if (
    !ts.isStringLiteralLike(node) &&
    !ts.isTemplateHead(node) &&
    !ts.isTemplateMiddle(node) &&
    !ts.isTemplateTail(node)
  ) {
    return false;
  }

  let parent = node.parent;
  while (
    parent &&
    (ts.isTemplateExpression(parent) ||
      ts.isTemplateSpan(parent) ||
      ts.isJsxExpression(parent) ||
      ts.isParenthesizedExpression(parent))
  ) {
    parent = parent.parent;
  }

  if (parent && ts.isJsxAttribute(parent)) {
    return isClassNameProperty(parent.name.getText());
  }

  if (parent && ts.isPropertyAssignment(parent)) {
    return isClassNameProperty(getPropertyName(parent.name));
  }

  if (parent && ts.isCallExpression(parent)) {
    const callee = parent.expression.getText();
    return ["cn", "clsx", "cva", "tv", "twMerge"].includes(callee);
  }

  return false;
}

function getLiteralText(node) {
  if (ts.isStringLiteral(node) || ts.isNoSubstitutionTemplateLiteral(node)) {
    return node.text;
  }
  if (ts.isTemplateHead(node) || ts.isTemplateMiddle(node) || ts.isTemplateTail(node)) {
    return node.text;
  }
  return null;
}

function getPropertyName(name) {
  if (ts.isIdentifier(name) || ts.isStringLiteral(name)) return name.text;
  return null;
}

function isClassNameProperty(name) {
  return typeof name === "string" && (name === "className" || name.endsWith("ClassName"));
}

function collectSourceFiles(rootDir, includeRoots, excluded) {
  const files = [];
  for (const includeRoot of includeRoots) {
    const absoluteRoot = resolve(rootDir, includeRoot);
    if (!existsSync(absoluteRoot)) continue;
    walk(absoluteRoot);
  }
  return files.sort();

  function walk(directory) {
    const entries = readdirSync(directory, { withFileTypes: true }).sort((left, right) =>
      left.name.localeCompare(right.name)
    );

    for (const entry of entries) {
      const absolutePath = resolve(directory, entry.name);
      const path = normalizePath(relative(rootDir, absolutePath));
      if (excluded.some((prefix) => path === prefix || path.startsWith(prefix))) continue;

      if (entry.isDirectory()) {
        walk(absolutePath);
      } else if (entry.isFile() && /\.(?:css|ts|tsx)$/.test(entry.name)) {
        files.push(absolutePath);
      }
    }
  }
}

function discoverPackageSourceRoots(webRoot) {
  return ["apps", "shared"].flatMap((group) => {
    const groupRoot = resolve(webRoot, group);
    if (!existsSync(groupRoot)) return [];
    return readdirSync(groupRoot, { withFileTypes: true })
      .filter((entry) => entry.isDirectory())
      .map((entry) => `${group}/${entry.name}/src`)
      .filter((sourceRoot) => existsSync(resolve(webRoot, sourceRoot)))
      .sort();
  });
}

function isSurfacePolicySource(file) {
  const path = normalizePath(file);
  return (
    path.endsWith(".tsx") &&
    !path.includes("/__tests__/") &&
    !/\.(?:test|spec|stories)\.tsx$/.test(path)
  );
}

function readExceptions(rootDir, exceptionsPath) {
  if (!exceptionsPath) return [];
  const path = resolve(rootDir, exceptionsPath);
  if (!existsSync(path)) return [];
  return JSON.parse(readFileSync(path, "utf8"));
}

function normalizeOwners(owners) {
  return Object.fromEntries(
    Object.entries(owners).map(([rule, paths]) => [rule, new Set(paths.map(normalizePath))])
  );
}

function isOwner(owners, rule, path) {
  return owners[rule]?.has(path) ?? false;
}

function isLayoutStylesheetImport(params) {
  return new RegExp(`^["']${escapeRegex(layoutStylesheetImport)}["'](?:\\s|$)`).test(
    params.trim()
  );
}

function normalizePath(path) {
  return path.replaceAll("\\", "/");
}

function escapeRegex(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function compareDiagnostics(left, right) {
  return (
    left.path.localeCompare(right.path) ||
    left.line - right.line ||
    left.column - right.column ||
    left.rule.localeCompare(right.rule)
  );
}
