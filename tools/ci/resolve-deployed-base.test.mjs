import assert from "node:assert/strict";
import test from "node:test";
import { selectDeployedBase } from "./resolve-deployed-base.mjs";

const deployed = "a".repeat(40);
const failed = "b".repeat(40);

test("the newest successful production deployment is the release base", () => {
  assert.equal(selectDeployedBase([{ sha: failed, state: "failure" }, { sha: deployed, state: "success" }]), deployed);
});

test("no successful production deployment selects no base", () => {
  assert.equal(selectDeployedBase([]), "");
  assert.equal(selectDeployedBase([{ sha: failed, state: "failure" }, { sha: deployed, state: "inactive" }]), "");
});

test("a malformed deployment commit is rejected", () => {
  assert.throws(() => selectDeployedBase([{ sha: "main", state: "success" }]), /invalid commit SHA/);
});
