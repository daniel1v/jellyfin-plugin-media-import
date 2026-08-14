# Changelog

## 0.1.0.0 — Initial beta — 2026-08-14

- Added an administrator-only import queue for `.mkv`, `.mp4`, and `.m4v` files.
- Added combined movie and series search through Jellyfin's enabled TMDb provider.
- Added explicit season and episode resolution for series imports.
- Added server-generated previews for clean Jellyfin-compatible names and destinations.
- Added movie, series, and episode NFO sidecars without provider IDs in visible names.
- Added non-overwriting file moves with path traversal and symbolic-link protection.
- Added explicit single and multi-file import with a Jellyfin library scan afterwards.
- Added progressive file size, duration, and resolution columns with stable sorting.
- Added controlled handling for unreadable or invalid video files.
