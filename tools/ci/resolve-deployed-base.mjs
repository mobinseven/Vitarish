import { execFileSync } from "node:child_process";
import { appendFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const shaPattern = /^[0-9a-f]{40}$/;

// Deployments are listed newest first. The newest one whose latest status is
// success is what production runs now; failed or rolled-back attempts after it
// do not move the base, so their changes are selected again.
export function selectDeployedBase(deployments) {
  const deployed = deployments.find(({ state }) => state === "success");
  if (!deployed) return "";
  if (!shaPattern.test(deployed.sha)) throw new Error("Production deployment record has an invalid commit SHA.");
  return deployed.sha;
}

async function readProductionDeployments({ apiUrl, repository, token, environment }) {
  const request = async (path) => {
    const response = await fetch(`${apiUrl}/repos/${repository}${path}`, {
      headers: { authorization: `Bearer ${token}`, accept: "application/vnd.github+json" },
      signal: AbortSignal.timeout(30_000),
    });
    if (!response.ok) throw new Error(`GitHub deployment lookup returned HTTP ${response.status}.`);
    return response.json();
  };
  const deployments = await request(`/deployments?environment=${encodeURIComponent(environment)}&per_page=100`);
  const records = [];
  for (const deployment of deployments) {
    const [latest] = await request(`/deployments/${deployment.id}/statuses?per_page=1`);
    records.push({ sha: deployment.sha, state: latest?.state });
    if (latest?.state === "success") break;
  }
  return records;
}

function isAncestorOfHead(sha) {
  try {
    execFileSync("git", ["merge-base", "--is-ancestor", sha, "HEAD"], { stdio: "ignore" });
    return true;
  } catch (error) {
    if (error.status === 1) return false;
    throw error;
  }
}

if (resolve(process.argv[1] ?? "") === fileURLToPath(import.meta.url)) {
  const { GITHUB_API_URL, GITHUB_REPOSITORY, GITHUB_TOKEN, GITHUB_OUTPUT, DEPLOYMENT_ENVIRONMENT } = process.env;
  if (!GITHUB_API_URL || !GITHUB_REPOSITORY || !GITHUB_TOKEN || !GITHUB_OUTPUT || !DEPLOYMENT_ENVIRONMENT) {
    throw new Error("GitHub API, repository, token, output, and deployment environment are required.");
  }
  let base = selectDeployedBase(await readProductionDeployments({
    apiUrl: GITHUB_API_URL, repository: GITHUB_REPOSITORY, token: GITHUB_TOKEN, environment: DEPLOYMENT_ENVIRONMENT,
  }));
  if (!base) console.log("No successful production deployment was found; selecting the full suite.");
  else if (!isAncestorOfHead(base)) {
    console.log(`Deployed commit ${base} is not an ancestor of this release; selecting the full suite.`);
    base = "";
  } else console.log(`Selecting changes since the deployed commit ${base}.`);
  appendFileSync(GITHUB_OUTPUT, `sha=${base}\n`);
}
