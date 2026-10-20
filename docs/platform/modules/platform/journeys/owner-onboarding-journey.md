# Owner Onboarding Journey Map

## 1. Context

- Product / service: Vitarish self-service clinic onboarding
- Journey type: Current state, behind a feature flag
- Entry point: **Create your clinic** on the sign-in page, which opens the onboarding host
- Scenario: A person with no Vitarish account signs up with a phone number, creates a clinic at its own address, lands signed in on it, and sets it up.
- User goal: Have a working clinic without contacting support.
- Business goal: Let clinics join on their own while keeping clinics isolated from each other.
- Scope start: The person opens the onboarding host.
- Scope end: The person is signed in on the new clinic's home page.
- Primary persona: A practitioner or clinic manager starting with Vitarish
- Design record: [ADR-0016](../../../../architecture/decisions/0016-self-service-clinic-onboarding.md)
- Operations: [Clinic onboarding runbook](../../../../runbooks/clinic-onboarding.md)
- Proof: `src/Hosts/Web/tests/e2e/clinic-onboarding-walkthrough.spec.ts`

## 2. Feature Gate and Hosts

| Setting | Effect when missing |
| --- | --- |
| `Vitarish:Features:TenantOnboarding` | The creation endpoints answer 404. |
| `Vitarish:TenantBaseDomain` | The creation endpoints answer 404; no clinic address can be formed. |
| `Vitarish:OnboardingUrl` (Main) | No host is bound to the onboarding tenant, so nobody without a role can sign in anywhere. |
| `VITARISH_ONBOARDING_URL` (owner Web) | The sign-in page hides **Create your clinic**. |

Each clinic lives at `<subdomain>.<base domain>`. The onboarding host is one
more host under the base domain, bound to a platform-owned onboarding tenant
that holds no practice data. Session cookies are host-only.

## 3. Happy Path

1. On the sign-in page the person selects **Create your clinic** and lands on the onboarding host.
2. They sign in with **Sign in with Phone**: they enter a mobile number, receive a code by SMS, and enter it. A new number creates an account with no role. Only the onboarding host accepts a session without a role, and that session can't open any clinic's data.
3. The Journey profile gate asks for first and last name (**Complete your profile**).
4. The Journey clinic-creation gate sends them to `/setup` (**Create your clinic**).
5. Step **Name and address**: clinic name and address. The address is checked while they type and is reported as available, taken, reserved or malformed.
6. Step **Specialty and details**: a specialty from a fixed list, an optional description, and optional business details: address with a map pin, public phones, website, social links, and opening hours with split shifts. A notice says these details may be shown publicly.
7. **Create clinic**. The server checks the session's user and their verified phone, creates the clinic, its address, its owner and the business profile together, and audits the outcome.
8. Creation ends the person's onboarding session and makes them the clinic's owner. The page shows the new address and **Go to my clinic**.
9. **Go to my clinic** opens `/handoff` on the new clinic's host with a one-time token in the URL fragment. Redeeming it starts a normal session there.
10. The clinic's home page opens, with a prompt to upload a logo. Dismissing the prompt is stored on the server.

## 4. Failure and Recovery

| Situation | What the person sees | Recovery |
| --- | --- | --- |
| Address taken, reserved or malformed | The message under the address field; **Next** stays unavailable | Choose another address. |
| Address taken between the check and **Create clinic** | "This address was taken a moment ago." | Choose another address. |
| No verified phone | "A verified phone number is required to create a clinic." | Sign in with phone. |
| More than 3 clinics in a day, or too many requests from one network | "Too many clinics were created recently." | Try later. |
| The same **Create clinic** sent twice | The clinic created the first time | None needed. |
| The handoff link is expired, used, or opened by someone else | The handoff page reports it failed | Sign in on the new clinic normally. |
| The map can't load | "The map could not be loaded." | The written address is still saved. |
| Returning to the onboarding host after creating a clinic | The sign-in page | Sign in again; the session ended at creation. |

## 5. Not Yet Covered

- Editing the clinic's settings and adding photos after creation (#915).
- How the public business listing is modelled and shown (#917).
- Entity audit entries for the new clinic's records (#918).
