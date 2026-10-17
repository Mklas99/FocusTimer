# Tasks

## 1. OI-35 widget placement

- [x] 1.1 Add defaulted placement setting and clone support; verify JSON round-trip and old-file defaults.
- [x] 1.2 Bind full and compact clock/control placement to appearance preview; verify rendered geometry and target scaling at 0.5, 1, and 3 scales.
- [x] 1.3 Verify Apply/OK, failed save, Cancel, and title-bar discard preserve placement correctly; update widget docs and OI-35.

## 2. OI-41 notification preferences

- [x] 2.1 Add enabled flags and bounded severity durations with clone support; verify default and persistence tests.
- [x] 2.2 Add General controls and duration validation; verify invalid, failed-save, commit, and discard paths.
- [x] 2.3 Apply committed policy at startup and runtime activation; verify suppression, mandatory errors, severity delays, acknowledgement reminders, and existing-toast timing.
- [x] 2.4 Update notification/settings documentation and OI-41; verify text matches tested behavior.

## 3. OI-37 Developer toggle

- [x] 3.1 Toggle after each seven editable version clicks and reset partial counts when replacing drafts; verify repeated enable/disable, blocked clicks, and preserved Developer values.
- [x] 3.2 Verify Apply/OK persistence, Cancel/title-bar discard, and failed save; update Developer documentation and OI-37.

## 4. Integration verification

- [x] 4.1 Run affected Core, Persistence, App, and headless tests and a Windows Host build; record results in verification.md.
- [x] 4.2 Validate OpenSpec and reconcile current docs/specs with implementation; inspect the final diff and record remaining native-only verification limits.
