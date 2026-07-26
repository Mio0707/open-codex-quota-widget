---
name: open-codex-quota-widget
description: Launch the bundled Windows Codex quota desktop widget that displays the current user's local Codex remaining percentage and reset time. Use when a user asks to open, start, show, or launch the Codex quota widget, Codex额度悬浮窗, 灵动岛额度窗口, or desktop quota display.
---

# Open Codex Quota Widget

Launch the bundled widget immediately on Windows.

## Run

1. Resolve this skill's directory from the loaded `SKILL.md` path.
2. Run `scripts/open-widget.ps1` with Windows PowerShell:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File "<skill-directory>\scripts\open-widget.ps1"
   ```

3. Report whether the widget opened or was already running.

Do not rebuild the application, request an API key, or copy the executable elsewhere. The widget reads the current Windows user's own `%USERPROFILE%\.codex\sessions` records every five seconds; it does not use the skill creator's quota or credentials.

If the host is not Windows, explain that this bundled version is Windows-only and do not attempt to run the executable.
