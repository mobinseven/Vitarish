#!/usr/bin/env node

// Runs the mock specs of each mock app and the real-host (E2E) specs to capture screenshots and video.
// Asks for the mode, flows, locales, themes, viewports, mock scenarios, video and browser; a flag answers its question
// up front, and without a terminal every unanswered question takes its default.
// Usage: npm run specs -- [--mode mock,e2e|all] [--flows <flow,...>|all] [--locales fa,en|all] [--themes light,dark|all]
//          [--viewports desktop,mobile|all] [--scenarios ready,empty,loading,error|all] [--video|--no-video]
//          [--browser headless|headed|ui]

import { spawn } from "node:child_process";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import path from "node:path";
import { createInterface } from "node:readline/promises";
import { fileURLToPath, pathToFileURL } from "node:url";
import { parseArgs } from "node:util";

import { supportedLocales } from "../shared/localization/src/locales.ts";
import {
  captureColorSchemeEnvironmentKey,
  captureColorSchemes,
  captureScreenshotsEnvironmentKey,
  captureVideoEnvironmentKey,
  captureViewportEnvironmentKey,
  captureViewports,
  mockAppForSpec,
  mockApps,
  mockScenarios,
  mockScreenshotRootFor,
} from "./spec-capture-options.mjs";
import { twoHostE2eLocaleEnvironmentKey, twoHostE2eProofScreenshotEnvironmentKey } from "./two-host-e2e-artifacts.mjs";

const webRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const modes = ["mock", "e2e"];
const browsers = ["headless", "headed", "ui"];
const all = "all";
const specSuffix = ".spec.ts";
// An E2E spec follows the chosen locale only when it reads the locale key; the others keep the locale they are written for.
const e2eLocaleMarker = Object.keys({ twoHostE2eLocaleEnvironmentKey })[0];
const mockCaptureRoots = Object.keys(mockApps).flatMap((app) => [
  path.join(webRoot, mockScreenshotRootFor(app)),
  path.join(webRoot, "artifacts", "playwright-results", `${app}-screenshots`),
]);
const e2eRunsRoot = path.join(webRoot, "artifacts", "two-host-e2e", "runs");
// Screenshots, videos and failure traces; the HTML report keeps its own copies of them, which are left out.
const captureExtensions = [".png", ".webm", ".zip"];
const reportDirectoryName = "report";

const { values: flags } = parseArgs({
  allowNegative: true,
  options: {
    mode: { type: "string" },
    flows: { type: "string" },
    locales: { type: "string" },
    themes: { type: "string" },
    viewports: { type: "string" },
    scenarios: { type: "string" },
    video: { type: "boolean" },
    browser: { type: "string" },
  },
});

const specs = modes.flatMap((mode) =>
  readdirSync(path.join(webRoot, "tests", mode))
    .filter((file) => file.endsWith(specSuffix))
    .sort()
    .map((file) => ({
      mode,
      file,
      flow: file.slice(0, -specSuffix.length),
      // Mock specs capture every locale and are narrowed by title; E2E specs read the locale from the environment.
      followsLocale: mode === "mock" || readFileSync(path.join(webRoot, "tests", mode, file), "utf8").includes(e2eLocaleMarker),
    })),
);

const prompt = process.stdin.isTTY ? createInterface({ input: process.stdin, output: process.stdout }) : null;
let selection;
try {
  selection = await ask();
} finally {
  prompt?.close();
}

const runs = planRuns(selection);
console.log(`\nSame selection without questions:\n  npm run specs -- ${describeFlags(selection)}`);
console.log(`\n${runs.length} run(s):`);
for (const run of runs) console.log(`  - ${run.label}`);

let interrupted = false;
// The child runners receive Ctrl+C themselves and stop their servers; wait for them, then start nothing new.
process.on("SIGINT", () => {
  interrupted = true;
});

const failures = [];
const startedAt = Date.now();
const e2eRunsBefore = listE2eRuns();
for (const run of runs) {
  if (interrupted) break;
  console.log(`\n== ${run.label}`);
  const exitCode = await execute(run);
  if (exitCode !== 0) failures.push(run.label);
}

printCaptures(listCaptures([...mockCaptureRoots, e2eRunsRoot], startedAt));
for (const run of listE2eRuns().filter((run) => !e2eRunsBefore.includes(run))) {
  console.log(`E2E host logs: ${pathToFileURL(path.join(e2eRunsRoot, run)).href}`);
}
if (interrupted) console.log("Stopped before every run finished.");
for (const failure of failures) console.error(`Failed: ${failure}`);
process.exit(failures.length > 0 || interrupted ? 1 : 0);

async function ask() {
  const chosenModes = await choose("mode", "Which specs?", modes, ["mock"]);
  const flowSpecs = specs.filter((spec) => chosenModes.includes(spec.mode));
  const flowChoices = flowSpecs.map((spec) => `${spec.flow} [${spec.mode}]`);
  const chosenFlows = await choose("flows", "Which flows?", flowChoices, flowChoices, (names) => {
    const unknown = names.filter((name) => !flowSpecs.some((spec) => spec.flow === name));
    if (unknown.length > 0) fail(`Unknown ${chosenModes.join("/")} flow: ${unknown.join(", ")}. Flows: ${[...new Set(flowSpecs.map((spec) => spec.flow))].join(", ")}`);
    return flowSpecs.filter((spec) => names.includes(spec.flow)).map((spec) => `${spec.flow} [${spec.mode}]`);
  });
  const chosenSpecs = flowSpecs.filter((spec) => chosenFlows.includes(`${spec.flow} [${spec.mode}]`));
  if (chosenSpecs.length === 0) fail("No flow was selected.");

  const fixedLocaleSpecs = chosenSpecs.filter((spec) => !spec.followsLocale);
  const locales = chosenSpecs.some((spec) => spec.followsLocale)
    ? await choose("locales", "Which locales?", supportedLocales, supportedLocales)
    : [];
  if (fixedLocaleSpecs.length > 0) {
    console.log(`Written for one locale, so run once in it: ${fixedLocaleSpecs.map((spec) => spec.flow).join(", ")}`);
  }
  const themes = await choose("themes", "Which themes?", captureColorSchemes, captureColorSchemes);
  const viewports = await choose("viewports", "Which viewports?", captureViewports, captureViewports);
  const scenarios = chosenSpecs.some((spec) => spec.mode === "mock")
    ? await choose("scenarios", "Which mock scenarios? (flows capture only in ready)", mockScenarios, ["ready"])
    : [];
  const video = flags.video ?? (await confirm("Record video?", true));
  const [browser] = await choose("browser", "How should the browser run?", browsers, ["headless"], undefined, false);
  return { specs: chosenSpecs, locales, themes, viewports, scenarios, video, browser };
}

function planRuns({ specs: chosenSpecs, locales, themes, viewports, scenarios, video, browser }) {
  const browserArguments = browser === "headless" ? [] : [`--${browser}`];
  const runs = [];

  // Each mock app runs against its own mock server.
  for (const app of Object.keys(mockApps)) {
    const mockSpecs = chosenSpecs.filter((spec) => spec.mode === "mock" && mockAppForSpec(spec.file) === app);
    if (mockSpecs.length === 0) continue;
    // Mock test titles carry their locale as a separate word.
    const localeFilter = locales.length < supportedLocales.length ? ["--grep", ` (${locales.join("|")})( |$)`] : [];
    const projects = viewports.flatMap((viewport) => themes.map((theme) => `${viewport}-${theme}`));
    runs.push({
      label: `mock ${app}: ${mockSpecs.map((spec) => spec.flow).join(", ")} | ${scenarios.join(", ")} | ${locales.join(", ")} | ${projects.join(", ")}`,
      args: [
        "scripts/private-practice-owner-screenshots.mjs",
        "--app",
        app,
        ...(scenarios.length < mockScenarios.length ? ["--scenarios", scenarios.join(",")] : []),
        "--",
        ...mockSpecs.map((spec) => spec.file),
        ...projects.flatMap((project) => ["--project", project]),
        ...localeFilter,
        ...browserArguments,
      ],
      environment: { [captureVideoEnvironmentKey]: video ? "1" : undefined },
    });
  }

  // Each E2E run starts fresh hosts, since the suite changes the accounts it signs in with.
  const e2eSpecs = chosenSpecs.filter((spec) => spec.mode === "e2e");
  const e2eLocales = locales.length > 0 ? locales : [undefined];
  for (const theme of themes) {
    for (const viewport of viewports) {
      e2eLocales.forEach((locale, index) => {
        // Specs written for one locale run with the first locale only.
        const runSpecs = index === 0 ? e2eSpecs : e2eSpecs.filter((spec) => spec.followsLocale);
        if (runSpecs.length === 0) return;
        runs.push({
          label: `e2e ${runSpecs.map((spec) => spec.flow).join(", ")} | ${locale ?? "own locale"} | ${viewport}-${theme}`,
          args: ["scripts/run-two-host-e2e.mjs", ...runSpecs.map((spec) => spec.file), ...browserArguments],
          environment: {
            [captureColorSchemeEnvironmentKey]: theme,
            [captureViewportEnvironmentKey]: viewport,
            [captureScreenshotsEnvironmentKey]: "1",
            [captureVideoEnvironmentKey]: video ? "1" : undefined,
            [twoHostE2eLocaleEnvironmentKey]: locale,
            // Each run writes its proof screenshot into its own run folder.
            [twoHostE2eProofScreenshotEnvironmentKey]: undefined,
          },
        });
      });
    }
  }
  return runs;
}

function describeFlags({ specs: chosenSpecs, locales, themes, viewports, scenarios, video, browser }) {
  const list = (values) => [...new Set(values)].join(",");
  return [
    `--mode ${list(chosenSpecs.map((spec) => spec.mode))}`,
    `--flows ${list(chosenSpecs.map((spec) => spec.flow))}`,
    ...(locales.length > 0 ? [`--locales ${list(locales)}`] : []),
    `--themes ${list(themes)}`,
    `--viewports ${list(viewports)}`,
    ...(scenarios.length > 0 ? [`--scenarios ${list(scenarios)}`] : []),
    video ? "--video" : "--no-video",
    `--browser ${browser}`,
  ].join(" ");
}

/** Asks for one or more of the choices by number or name; `flagParser` maps a flag value that is not a plain choice list. */
async function choose(flag, question, choices, defaults, flagParser, multiple = true) {
  const flagValue = flags[flag];
  if (flagValue !== undefined) {
    const names = flagValue.split(",").map((name) => name.trim()).filter(Boolean);
    const chosen = names.includes(all) && multiple ? [...choices] : flagParser ? flagParser(names) : names;
    const unknown = flagParser ? [] : chosen.filter((name) => !choices.includes(name));
    if (unknown.length > 0 || chosen.length === 0 || (!multiple && chosen.length !== 1)) {
      fail(`--${flag} accepts ${multiple ? `a comma-separated list of ${choices.join(", ")} or ${all}` : `one of ${choices.join(", ")}`}.`);
    }
    return chosen;
  }
  if (!prompt) {
    console.log(`No terminal to ask on; --${flag} defaults to ${defaults.join(",")}.`);
    return [...defaults];
  }

  console.log(`\n${question}`);
  choices.forEach((choice, index) => console.log(`  [${index + 1}] ${choice}`));
  const hint = multiple ? `numbers or names, comma-separated, or ${all}` : "a number or name";
  while (true) {
    const answer = (await prompt.question(`Choose ${hint} [${defaults.join(", ")}]: `)).trim();
    if (answer === "") return [...defaults];
    if (multiple && answer === all) return [...choices];
    const chosen = answer.split(",").map((part) => {
      const name = part.trim();
      const number = Number(name);
      return Number.isInteger(number) && number >= 1 && number <= choices.length ? choices[number - 1] : name;
    });
    if (chosen.every((name) => choices.includes(name)) && (multiple || chosen.length === 1)) return [...new Set(chosen)];
    console.log(`Enter ${hint}.`);
  }
}

async function confirm(question, defaultAnswer) {
  if (!prompt) {
    console.log(`No terminal to ask on; ${defaultAnswer ? "--video" : "--no-video"} is the default.`);
    return defaultAnswer;
  }
  while (true) {
    const answer = (await prompt.question(`\n${question} [${defaultAnswer ? "Y/n" : "y/N"}]: `)).trim().toLowerCase();
    if (answer === "") return defaultAnswer;
    if (answer === "y" || answer === "yes") return true;
    if (answer === "n" || answer === "no") return false;
  }
}

function execute(run) {
  const environment = { ...process.env };
  for (const [key, value] of Object.entries(run.environment)) {
    if (value === undefined) delete environment[key];
    else environment[key] = value;
  }
  return new Promise((resolve, reject) => {
    const child = spawn(process.execPath, run.args, { cwd: webRoot, env: environment, stdio: "inherit" });
    child.on("error", reject);
    child.on("exit", (code) => resolve(code ?? 1));
  });
}

/** Lists the capture files under the roots that were written since the runs started. */
function listCaptures(roots, since) {
  return roots
    .filter((root) => existsSync(root))
    .flatMap((root) => readdirSync(root, { recursive: true }).map((entry) => path.join(root, entry)))
    .filter((file) => captureExtensions.includes(path.extname(file).toLowerCase()))
    .filter((file) => !file.split(path.sep).includes(reportDirectoryName))
    .filter((file) => statSync(file).mtimeMs >= since)
    .sort();
}

function printCaptures(files) {
  if (files.length === 0) {
    console.log("\nNo screenshots or videos were written.");
    return;
  }
  console.log(`\n${files.length} capture(s):`);
  let directory;
  for (const file of files) {
    if (path.dirname(file) !== directory) {
      directory = path.dirname(file);
      console.log(`\n${path.relative(webRoot, directory)}`);
    }
    console.log(`  ${pathToFileURL(file).href}`);
  }
}

function listE2eRuns() {
  return existsSync(e2eRunsRoot) ? readdirSync(e2eRunsRoot) : [];
}

function fail(message) {
  console.error(message);
  process.exit(2);
}
