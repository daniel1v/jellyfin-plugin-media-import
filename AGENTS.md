# Media Import working agreements

## Scope

- This repository is a standalone Jellyfin plugin. Do not clone, modify, or build the Jellyfin server or web-client source code.
- Keep production Jellyfin data and libraries out of development and automated tasks. Only the explicitly configured test instance may receive deployed plugin files.

## Import safety

- Treat every source path, filename, metadata response, and file operation as untrusted input.
- Before adding code that moves, renames, overwrites, or deletes media, implement a reviewable import plan and require an explicit administrator confirmation.
- Default to preview-only behavior. Never add unattended imports or background file mutations without a separately approved feature decision.

## Compatibility and secrets

- Keep `Jellyfin.Controller`, `Jellyfin.Model`, `build.yaml` target ABI, and the test-server version aligned. Change them deliberately and document the target release in the pull request.
- Use `IProviderManager` for TMDb lookup, explicitly selecting `TheMovieDb` and excluding disabled providers. Do not add a separate TMDb client or read, copy, store, expose, or log Jellyfin's effective TMDb key.
- Do not commit API keys, Jellyfin tokens, media paths, test-server configuration, or real media files. Use local environment variables or ignored local configuration.

## Verification

- Run `dotnet test Jellyfin.Plugin.MediaImport.sln --configuration Debug` after code changes.
- Use `scripts/Publish-Plugin.ps1` to validate a deployable plugin. Do not run `scripts/Deploy-TestPlugin.ps1` unless the configured data directory is confirmed to be the isolated test instance.
