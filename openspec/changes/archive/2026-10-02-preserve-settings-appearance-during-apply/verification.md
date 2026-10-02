# Verification

## Automated evidence

- App regression suite: 135 passed; one Windows native fixture skipped in the ordinary run.
- Isolated Windows native fixture: passed with `FOCUSTIMER_NATIVE_APPEARANCE_TESTS=1`. It uses the installed Avalonia desktop platform, UI synchronization context, and UI scheduler without new dependencies.
- Core regression suite: 202 passed.
- Host Debug build: succeeded with zero warnings and zero errors.
- Strict validation passes for this change and the affected Settings, theming, and widget specifications. Repository-wide strict validation flags the pre-existing auto-start specification's purpose text as shorter than 50 characters; it does not flag the affected specifications.
- Native widget checks realize FullModeView and CompactModeView and verify normal, hovered, pressed, and disabled icon foregrounds. A color edit while hovering updates the rendered foreground, and state precedence is verified.
- Native Settings checks run in Light, Monokai, and Solarized Dark. They verify the draft panel stays enabled during a delayed Apply, focus moves to the save status, dropdown/context menus close, text/paste/cut and slider keyboard input are blocked, duplicate OK does not start another transaction, and Apply restores focus.
- The native fixture supplies active preedit text through Avalonia's text-input-method client, verifies that it is present before Apply, and verifies that the TextPresenter's preedit text is cleared while the draft remains locked. Blocked routed text is not retained for replay.
- The same fixture covers fast Apply, ordinary activation failure, recovery-required failure, and successful OK closing a newly opened Settings window. Saving status clears for each result; recovery blocks another commit.
- Theme tests cover optional color inheritance, explicit values, clone/JSON and actual file import/export round trips, invalid Play/Pause colors, lifecycle compensation, Cancel after Apply, and reopening saved values. Deferred imports completing during or after a commit are rejected.

## Rendered evidence

The native fixture writes `appearance-evidence` under its test output directory, with saving, applied, ordinary-failure, and recovery screenshots for each of the three themes. The current Debug run uses `tests/FocusTimer.App.Tests/bin/ThemeCheck/appearance-evidence/`. Solarized Dark saving and Light applied captures were inspected: Settings retains its normal palette, the saving status has a reserved footer area, and the Play/Pause editor and future-use status-color explanation are present.

## Coverage limits

Composition is verified through Avalonia's client and presenter on the Windows desktop platform. Physical keyboard interaction with individual Windows IME language implementations was not exercised; the fixture verifies clearing active preedit text, releasing editor focus, and rejecting routed text during saving. No new dependency, platform-service change, or requirement waiver was needed.

OI-29 and unrelated OI-21/OI-24 visual work remain open. This change was verified for archive on 2026-10-02; the batch verification and acceptance evidence are recorded in `docs/versions/current/M03Verification.md`.
