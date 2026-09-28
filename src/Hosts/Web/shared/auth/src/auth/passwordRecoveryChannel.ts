/** Values match the Authentication module's PasswordRecoveryChannelDto enum. */
export const passwordRecoveryChannel = {
  email: 0,
  sms: 1,
} as const;

export type PasswordRecoveryChannel = keyof typeof passwordRecoveryChannel;
