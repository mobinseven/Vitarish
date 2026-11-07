"use client";

import {
  AccountSignInSettings,
  useAccountPasswordChange,
  useAccountSignInSecurityActions,
} from "@vitarish/account";
import { AccountBackLink, AccountSurface } from "@/components/account/AccountSurface";

export default function PasswordChangePage() {
  const { changePassword } = useAccountPasswordChange();
  const { setFirstPassword, setTwoFactorEnabled } =
    useAccountSignInSecurityActions();

  return (
    <AccountSurface width="wide">
      <AccountSignInSettings
        changePassword={changePassword}
        setFirstPassword={setFirstPassword}
        setTwoFactorEnabled={setTwoFactorEnabled}
      />
      <AccountBackLink />
    </AccountSurface>
  );
}
