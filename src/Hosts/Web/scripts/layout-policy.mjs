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
];

const layoutRuleIdSet = new Set(layoutRuleIds);
const layoutStylesheetImport = "@vitarish/web-ui/layout.css";

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
};

const viewportUtilityPattern =
  /(?:^|\s)(?:(?:h|min-h|max-h|size)-(?:screen|svh|dvh|lvh)|(?:h|min-h|max-h|size)-\[[^\]]*100(?:s|d|l)?vh[^\]]*\])(?=\s|$)/;
const viewportCssPattern = /\b100(?:s|d|l)?vh\b/i;
const legacyContainerTokenSets = [
  ["mx-auto", "max-w-[1200px]", "px-6"],
  ["mx-auto", "max-w-7xl", "px-5", "pb-10", "pt-4"],
];

export function createProductionLayoutPolicyConfig(webRoot) {
  return {
    rootDir: webRoot,
    includeRoots: [
      "apps/private-practice-owner/src",
      "apps/knowledge-qa-demo/src",
      "shared/auth-web/src",
      "shared/clinic-web/src",
      "shared/journey-web/src",
      "shared/knowledge-web/src",
      "shared/sea-web/src",
      "shared/web-ui/src",
    ],
    excludePrefixes: [
      "shared/web-ui/src/__tests__/",
    ],
    owners: {
      "raw-main": ["shared/web-ui/src/layout/LayoutPrimitives.tsx"],
      "raw-viewport": ["shared/web-ui/src/layout.css"],
      "direct-applayout": ["shared/web-ui/src/shell/DashboardFrame.tsx"],
      "legacy-outer-container": [],
      "layout-css-import": [],
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
  const diagnostics = [];
  const stylesheetImports = new Map();

  for (const file of files) {
    const path = normalizePath(relative(rootDir, file));
    const source = readFileSync(file, "utf8");

    if (file.endsWith(".tsx") || file.endsWith(".ts")) {
      lintTypeScriptSource({ diagnostics, owners, path, source });
    } else if (file.endsWith(".css")) {
      lintCssSource({ diagnostics, owners, path, source, stylesheetImports });
    }
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
    scannedFiles: files.length,
    suppressed,
  };
}

function lintTypeScriptSource({ diagnostics, owners, path, source }) {
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

function lintCssSource({ diagnostics, owners, path, source, stylesheetImports }) {
  const root = postcss.parse(source, { from: path });

  root.walkDecls((declaration) => {
    if (viewportCssPattern.test(declaration.value) && !isOwner(owners, "raw-viewport", path)) {
      addCssDiagnostic(diagnostics, path, "raw-viewport", declaration);
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
