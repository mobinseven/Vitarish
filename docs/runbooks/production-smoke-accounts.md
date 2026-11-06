# Production Smoke Accounts Runbook

Four real production accounts used to check sign-in journeys after a deploy.
They are set up once through the product, the same way a person would, and
then reused. There is no script: contacts are confirmed only by signing in with
a real code, and roles are granted only by a platform administrator.

## Accounts

| Username | Contact | State | After sign-in |
| --- | --- | --- | --- |
| `smoke-no-workspace` | `smoke-no-workspace@vitarish.com` | No clinic, no role | Platform home (`vitarish.com/home`) offering only "create workspace" |
| `smoke-one-clinic` | `smoke-one-clinic@vitarish.com` and the owner's phone | Owner of "Vitarish Smoke Clinic" | Platform home with one workspace |
| `smoke-two-clinics` | `smoke-two-clinics@vitarish.com` | Owner of "Vitarish Smoke Clinic B", staff in "Vitarish Smoke Clinic" | Platform home with two workspaces |
| `smoke-admin` | `smoke-admin@vitarish.com` | Platform administrator, two-step sign-in by email code | Platform admin, after the emailed code |

- The mailboxes are real and controlled by the owner. Only one phone number is
  available, so only `smoke-one-clinic` has a phone. Clinic creation needs a
  confirmed phone, so that account creates both clinics and clinic B is then
  handed to `smoke-two-clinics`.
- The smoke clinics are ordinary clinics with clear names. They are not hidden.
- Each password lives only in a GitHub Actions secret and the owner's password
  manager. Never paste it in chat, an issue, a commit, or a log.

| GitHub secret | Account |
| --- | --- |
| `VITARISH_SMOKE_PASSWORD_NO_WORKSPACE` | `smoke-no-workspace` |
| `VITARISH_SMOKE_PASSWORD_ONE_CLINIC` | `smoke-one-clinic` |
| `VITARISH_SMOKE_PASSWORD_TWO_CLINICS` | `smoke-two-clinics` |
| `VITARISH_SMOKE_PASSWORD_ADMIN` | `smoke-admin` |

Set each one by typing it at the prompt, so it stays out of shell history:

```powershell
gh secret set VITARISH_SMOKE_PASSWORD_ONE_CLINIC
```

## Before you start

- The active authentication profile, in platform admin security settings,
  allows sign-in by email code and a code after the password. The same setting
  covers SMS and email codes.
- Self-service clinic onboarding is on ([Clinic onboarding](clinic-onboarding.md)).
- You are signed in to platform admin as a platform administrator other than
  `smoke-admin`.
- Use a private browser window per smoke account. Each account has its own
  session limit, and the default policy allows one session per account.

## One-time setup

Setting a password and turning on two-step sign-in both need a sign-in within
the last ten minutes. If the page asks you to sign in again, sign out, sign in
and repeat the step.

1. **Create the users.** In platform admin, open Users, then New user, and
   create the four accounts with the usernames, emails and names above. No
   password is set here.
2. **Confirm the emails.** For each account, sign in at
   `https://auth.vitarish.com` with the email code sent to its mailbox. A code
   sign-in confirms that address.
3. **Set the passwords.** Still signed in, open account settings, then
   Password. The page offers "Set a password" with no current-password field.
   Use the value from the password manager, then store it in the matching
   secret.
   - Clinic accounts: on a clinic host, `/account/password`.
   - `smoke-no-workspace`: on the sign-in host,
     `https://auth.vitarish.com/account/password`. This path has not yet been
     tried in production; if it does not load, stop and report it rather than
     giving the account a clinic.
   - `smoke-admin`: do this in step 7.
4. **Confirm the phone.** As `smoke-one-clinic`, open account settings, then
   Phone, and confirm the owner's phone with the SMS code.
5. **Create the clinics.** As `smoke-one-clinic`, create "Vitarish Smoke
   Clinic" and then "Vitarish Smoke Clinic B" from platform home ("create
   workspace"). Each opens on its own clinic host.
6. **Move clinic B and add the staff membership.** In platform admin:
   1. On `smoke-one-clinic` and `smoke-two-clinics`, use End all sessions.
      Role changes are refused while the account has an active session.
   2. On `smoke-two-clinics`, Grant role: owner in "Vitarish Smoke Clinic B".
   3. On `smoke-two-clinics`, Grant role: a staff role in "Vitarish Smoke
      Clinic".
   4. On `smoke-one-clinic`, Revoke role: owner in "Vitarish Smoke Clinic B".
      Grant before revoking; the last owner of a clinic cannot be removed.

   The clinic's business-owner record in Parties still names
   `smoke-one-clinic`. Only the roles move.
7. **Make the admin.** In platform admin, on `smoke-admin`, use End all
   sessions, then Grant global role: platform administrator. Then sign in to
   platform admin as `smoke-admin` with the email code and open My account:
   1. Set a password.
   2. Turn on two-step sign-in. The code channel shows "email", because the
      account has no phone.
   3. Sign out and sign in again with the password. An email code must be
      asked for. If not, turn two-step sign-in on again before using the
      account.

## Smoke check after a deploy

Use a private window per account, and sign out at the end of each.

1. `smoke-no-workspace`: password sign-in at `https://auth.vitarish.com` lands
   on `vitarish.com/home` with only "create workspace". Do not create one.
2. `smoke-one-clinic`: lands on platform home with one workspace; continuing
   opens "Vitarish Smoke Clinic" on its own host, already signed in.
3. `smoke-two-clinics`: lands on platform home with both clinics; each opens
   on its own host.
4. `smoke-admin`: password sign-in asks for the email code; reading it from
   `smoke-admin@vitarish.com` and entering it opens platform admin. This check
   is manual: the code is a real second step and is never automated.

Forgot-password works for these accounts because the mailboxes are real, but
using it rotates the password, so update the secret afterwards.

## Rotation

1. Sign in as the account and change the password in account settings.
2. Update the matching GitHub secret with `gh secret set`.
3. Run the smoke check for that account.

## Drift

Fix drift through the same product screens. Never edit the database.

- `smoke-no-workspace` has a clinic or role: a platform administrator removes
  the membership after ending the account's sessions.
- An account lost its confirmed email (for example after an admin edit): sign
  in once with the email code to confirm it again.
- `smoke-admin` signs in without a code: turn two-step sign-in on again in My
  account. If that is refused, check that the email is confirmed and that the
  policy still allows a code after the password.
- A smoke clinic was renamed or removed: recreate it as in steps 5 and 6.
