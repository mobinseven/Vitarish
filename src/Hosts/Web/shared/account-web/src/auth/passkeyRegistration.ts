import { parsePasskeyRegistrationOptions } from "../hooks/passkeyRegistrationOptions";
import { PasskeyRegistrationStageError, type PasskeyRegistrationStage } from "../hooks/passkeyRegistrationStageError";

type StartRegistrationFn = (options: PublicKeyCredentialCreationOptionsJSON) => Promise<PublicKeyCredentialCreationOptionsJSON>;

export async function registerPasskey(
  startRegistration: StartRegistrationFn,
  beginRegister: () => Promise<string>,
  completeRegister: (attestationResponseJson: string) => Promise<unknown>
): Promise<void> {
  const optionsJson = await runStage("begin", beginRegister);
  const options = await runStage("parseOptions", () => parsePasskeyRegistrationOptions(optionsJson));
  const credential = await runStage("browserCreate", () => startRegistration(options));
  await runStage("complete", () => completeRegister(JSON.stringify(credential)));
}

async function runStage<T>(stage: PasskeyRegistrationStage, action: () => T | Promise<T>): Promise<T> {
  try {
    return await action();
  } catch (error) {
    throw new PasskeyRegistrationStageError(stage, error);
  }
}
