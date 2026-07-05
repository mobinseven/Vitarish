# Manual Smoke Checklist — HeroUI Pro Migration

Date: 2026-07-05
Status: Required before merge
Module: Design System / Web Host

## Pre-conditions

- Local dev server running (`npm run dev`)
- Authenticated as owner user
- Authenticated as normal user (separate browser/profile)
- Persian locale available for RTL check

## Smoke Scenarios

### 1. Desktop shell navigation

- [ ] Navigate to `/owner/clinic` — sidebar renders with all 6 links
- [ ] Click each sidebar link — correct page loads, active state highlights
- [ ] Navigate to `/account` — account sidebar renders
- [ ] Navigate to `/knowledge/qa` — knowledge page loads
- [ ] Navigate to `/home` — home dashboard loads
- [ ] No overlapping UI elements at desktop viewport (1280px+)

### 2. Mobile shell navigation

- [ ] Resize to mobile viewport (375px)
- [ ] Sidebar collapses or toggles via menu button
- [ ] Menu toggle works — sidebar opens/closes
- [ ] Content remains reachable after sidebar toggle
- [ ] No horizontal overflow on any page

### 3. Light/dark/system theme

- [ ] Toggle to light theme — all text readable, no invisible borders
- [ ] Toggle to dark theme — all text readable, no invisible borders
- [ ] Toggle to system theme — follows OS preference
- [ ] Pro components and OSS components both render correctly in each theme

### 4. RTL locale rendering

- [ ] Switch to Persian locale
- [ ] Account pages — text direction is right-to-left
- [ ] Clinic owner list — text direction is right-to-left
- [ ] Knowledge QA — prompt input and answer list are RTL-coherent
- [ ] Sidebar — menu items align correctly in RTL
- [ ] Icons and spacing remain coherent

### 5. Auth flow

- [ ] Login with password — session displays authenticated user
- [ ] View account page — profile data loads
- [ ] Logout — redirects to login, session cleared
- [ ] Access protected route while logged out — redirects to login

### 6. Clinic owner workflows

- [ ] View patient list — loading, empty, and populated states render
- [ ] Click a patient row — navigates to patient detail
- [ ] Sort patients by column — sort indicator updates
- [ ] View reservations list — records display correctly
- [ ] View shifts list — records display correctly

### 7. Knowledge QA

- [ ] View corpus rail — corpora list loads
- [ ] Select a corpus — selection highlights
- [ ] Submit a question — pending state shows, answer list populates
- [ ] View sources — source citations display
- [ ] Submit empty question — validation message appears

### 8. Keyboard navigation

- [ ] Tab through sidebar links — focus ring visible on each
- [ ] Tab through form fields — focus order is logical
- [ ] Activate buttons with Enter/Space — actions fire
- [ ] No keyboard traps (except intentional modal trap)

## Evidence

Record screenshots or tester notes for each scenario. Attach to PR description.

## Cleanup

- Reset theme to system/default after theme checks
- Reset locale to English after RTL checks
- Logout after auth checks
