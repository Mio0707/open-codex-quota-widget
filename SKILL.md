---
name: open-codex-quota-widget
description: Launch the installed Windows Codex quota desktop widget that displays the current user's local Codex remaining percentage and reset time.
---

# Open Codex Quota Widget

Launch the installed widget immediately on Windows.

## Run

Run `scripts/open-widget.ps1` with Windows PowerShell.

The widget reads the current Windows user's own `%USERPROFILE%\.codex\sessions` records every five seconds. It does not use an API key or upload quota data. If it is not installed, direct the user to this repository's Releases page to download the Windows installer.

