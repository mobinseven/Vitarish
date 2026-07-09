export type PasskeyRegistrationStage =
  | "begin"
  | "parseOptions"
  | "browserCreate"
  | "complete";

export class PasskeyRegistrationStageError extends Error {
  constructor(
    readonly stage: PasskeyRegistrationStage,
    cause: unknown
  ) {
    super(`Passkey registration failed during ${stage}.`, { cause });
    this.name = "PasskeyRegistrationStageError";
  }
}
