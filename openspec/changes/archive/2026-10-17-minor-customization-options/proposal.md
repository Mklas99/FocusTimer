# Proposal

## Why

The F-06 branch needs small user controls for widget placement, notification timing, and leaving Developer mode. These changes address OI-35, OI-41, and OI-37 in `docs/versions/current/OpenIssues.md`.

## What Changes

- Let users place widget controls beside or below the clock in both modes, with immediate appearance preview.
- Let users enable information and warning notifications and configure dismissal seconds by severity. Errors remain enabled and acknowledgement reminders stay open until dismissed.
- Toggle Developer mode after each seven clicks on the About version label, using the existing settings draft.
- Preserve defaults for older settings and use Apply/OK/Cancel for all new preferences.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `widget-ui`: Configurable clock/control placement in full and compact modes.
- `notifications`: Configurable visibility and dismissal timing.
- `settings`: Reversible Developer-mode activation and persistence of new controls.

## Impact

Core settings, App views/view models and notification service, Host activation, settings persistence tests, App regression tests, and current documentation. No new packages or native platform services.
