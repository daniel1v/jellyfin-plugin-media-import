# Media Import for Jellyfin

[![Tests](https://github.com/daniel1v/jellyfin-plugin-media-import/actions/workflows/test.yaml/badge.svg)](https://github.com/daniel1v/jellyfin-plugin-media-import/actions/workflows/test.yaml)

An interactive Jellyfin plugin for reviewing, identifying, naming, and importing films and series into a Jellyfin library.

> **Beta:** Media Import moves existing files. Test the plugin with non-critical media first and keep normal backups of your library.

## Features

- Lists completed `.mkv`, `.mp4`, and `.m4v` files from a configured handoff directory.
- Searches movies and series through Jellyfin's enabled built-in TMDb provider.
- Resolves an explicit season and episode before importing a series file.
- Shows the generated filename, destination, and NFO sidecars before any file is changed.
- Moves selected files without overwriting existing media or conflicting NFO files.
- Shows sortable file size, duration, and resolution columns without blocking the queue.
- Queues a Jellyfin library scan after each successful import.

## Compatibility

The initial beta targets **Jellyfin Server 10.11.11** and its plugin ABI `10.11.11.0`. Jellyfin plugin APIs are version-sensitive; other server versions are not supported unless a matching Media Import build is published.

The beta has been integration-tested on Windows. The implementation is platform-neutral, but Linux and container deployments should be considered beta test targets until they have received an explicit smoke test.

## Installation

1. Download the ZIP for the latest beta from the GitHub [Releases](https://github.com/daniel1v/jellyfin-plugin-media-import/releases) page.
2. Stop Jellyfin.
3. Extract the release into its own directory below Jellyfin's plugin directory, for example `plugins/Media Import`.
4. Start Jellyfin and confirm that **Media Import** appears in the administrator dashboard.
5. Configure the handoff directory and the movie and series destination directories.

Jellyfin must be able to read the handoff directory and create directories, NFO files, and media files below both destination roots. The built-in `TheMovieDb` metadata provider must be enabled. No separate TMDb key is required.

## Known beta limitations

- Only files directly inside the handoff directory are listed; subdirectories are ignored.
- Only `.mkv`, `.mp4`, and `.m4v` files are supported.
- Series imports require a known season and episode number; remote season and episode browsing is not available through Jellyfin's public provider API.
- Imports are serialized inside one Jellyfin process. Media Import does not coordinate with unrelated tools writing to the same directories.
- A failed library-scan request does not undo an otherwise successful file move; the result is reported in the UI and Jellyfin logs.
- Downloads, ripping, transcoding, automatic deletion, quality management, and reorganization of existing libraries are outside the plugin's scope.

## Development baseline

- Plugin namespace: `Jellyfin.Plugin.MediaImport`
- .NET SDK: 9.0.317 (or a newer 9.0 feature band)
- Jellyfin target ABI: 10.11.11.0
- Test server: Jellyfin 10.11.11 with a separate data directory and test-only libraries

The `Jellyfin.Controller` and `Jellyfin.Model` package versions are centralised in `Directory.Build.props`. When changing the Jellyfin version, update both package references and `build.yaml` together, then test against that exact server version.

## Metadata lookup

Media Import uses Jellyfin's enabled built-in TMDb provider through `IProviderManager`; it does not need, store, expose, or log a separate TMDb key. The provider is selected as `TheMovieDb` with disabled providers excluded.

For series, version 1 searches and selects the series first. Season and episode numbers come from the filename when possible, otherwise the administrator enters them. The resulting episode title is resolved through Jellyfin before an import can be confirmed. Version 1 intentionally does not use Jellyfin-internal TMDb classes or implement remote season/episode browsing.

## Current API foundation

All Media Import endpoints require an elevated Jellyfin administrator session.

- `GET /MediaImport/Files` lists `.mkv`, `.mp4`, and `.m4v` files directly inside the configured inbox.
- `GET /MediaImport/Files/MediaInfo` progressively reads duration and video dimensions through Jellyfin's public media-probe abstraction.
- `GET /MediaImport/Search/Movies` searches the enabled `TheMovieDb` provider.
- `GET /MediaImport/Search/Series` searches the enabled `TheMovieDb` provider.
- `GET /MediaImport/Search/Episode` resolves an explicit season and episode number for a selected series.
- `POST /MediaImport/Preview` resolves metadata and returns the server-generated media and NFO destinations without changing files.
- `POST /MediaImport/Import` revalidates the request, creates the NFO sidecars, moves one file without overwriting, and queues a Jellyfin library scan.

The dashboard page provides a queue with checkboxes, a combined film/series TMDb search dialog, episode resolution, destination previews, and explicit multi-file import. The chosen result determines the media type; episode-like filenames merely preselect series search. The inbox response contains only filenames and file metadata, never the configured absolute inbox path. Generic Blu-ray names such as `title_t00.mkv` deliberately produce no title suggestion and open the combined search without a query.

Destination paths are generated by the server. Visible names intentionally contain no provider IDs: movies use `Title (Year)/Title (Year).ext`, while episodes use `Series (Year)/Season XX/Series SXXEXX Episode.ext`.

Identification is persisted in Jellyfin-standard NFO sidecars. A movie receives `movie.nfo`; a series receives `tvshow.nfo`; and each episode receives a same-name `.nfo`. Movie and series NFOs contain the resolved TMDb IDs. Episode NFOs contain the resolved season, episode number, and title; an episode TMDb ID is included when Jellyfin's public provider result supplies one. The subsequent library scan therefore does not have to infer identity from the visible name. Existing matching NFO files are reused unchanged. Unreadable or conflicting metadata is treated as a conflict, and no existing NFO or media file is overwritten.

Source and destination roots are guarded against traversal and symbolic-link escapes. The library scan is used only to discover the completed filesystem import; Media Import does not write directly into Jellyfin's database.

## Build and test

```powershell
dotnet test Jellyfin.Plugin.MediaImport.sln --configuration Debug
.\scripts\Publish-Plugin.ps1
```

The published DLL and its required dependencies are placed in `artifacts/local`.

## Isolated test deployment

Never point the deployment script at a production Jellyfin data directory.

1. Create and start a separate Jellyfin 10.11.11 test instance with dedicated configuration, cache, database, and test libraries.
2. Set the `JELLYFIN_TEST_DATA_DIR` environment variable to that test instance's data directory.
3. Create an empty `.media-import-test` file in that same directory. It is an explicit safety marker required by the deployment script.
4. Run the VS Code task **Media Import: deploy to test Jellyfin**, or:

```powershell
.\scripts\Deploy-TestPlugin.ps1 -JellyfinDataDir $env:JELLYFIN_TEST_DATA_DIR
```

5. Restart the test Jellyfin server, then use the dashboard to open **Media Import**.

The debugger configuration attaches to a running test server; it does not launch or build Jellyfin core.

## Codex

`AGENTS.md` holds the repository rules that Codex loads automatically. In the ChatGPT desktop app, configure project actions for the three VS Code tasks (build, test, deploy to test Jellyfin) in the local-environment settings. The deployment task will refuse to run until the test-data environment variable and marker file are in place.

## License

Media Import is licensed under GPL-3.0, matching the Jellyfin plugin template and the Jellyfin assemblies it links against.
