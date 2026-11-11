# Solitaire (Klondike)

Classic **Klondike** solitaire for **Godot 4.6** and **C#** — one scene (`scenes/table.tscn`) with a small rules/state core in **`Solitaire.Core`**. The game is **free** on the store; this repo is the **public** source for GitHub.

## Features

- Standard Klondike rules, configurable draw (1/3), scoring (None / Standard / **Vegas**), optional timed game, undo (up to 3 moves)
- **Mobile-first** input (touch + safe area)
- **Locales:** English, Russian, 简体中文 (see `res://locale/ui.json`)
- Resume in-progress game from `user://` · results and best scores in `user://`
- Exports: **Windows** and **Android** (see Godot **Export** presets in the project)

## Requirements

| | |
|--|--|
| **Godot** | **4.6** with **.NET** / C# support ([download](https://godotengine.org/download/)) |
| **.NET SDK** | **8.0** (for editor + desktop) · Android export uses **.NET 9** when targeting `android` in `Solitaire.csproj` (via Godot’s .NET build) |
| **Android** (export only) | OpenJDK, Android SDK — follow [Godot: Exporting for Android](https://docs.godotengine.org/en/latest/tutorials/export/exporting_for_android.html) |

## Run from source

1. Clone the repository.
2. Open the project folder in **Godot 4.6** (it must load `project.godot`).
3. **Run** the main scene (F5) — the entry scene is `res://scenes/table.tscn`.

## Build (CLI)

```bash
dotnet build Solitaire.sln
```

Tests (optional):

```bash
dotnet test Solitaire.sln
```

*Godot* still compiles/hosts the game when you **Run Project**; `dotnet build` is mainly for the C# game library and `Solitaire.Core` tests.

## Project layout (short)

| Path | |
|------|--|
| `scenes/table.tscn` | Main UI and table |
| `scripts/` | `Table.cs`, `I18n.cs`, persistence, etc. |
| `Solitaire.Core/` | Game rules, `KlondikeState`, scoring — **no** Godot references |
| `locale/ui.json` | UI strings (en / ru / zh) |

## Store

The app is **free** on the store. When the listing is public, the Play (or other) link can be added at the top of this README.

## License

The full terms for this project are in the repository root: **[`LICENSE`](LICENSE)** (English + Russian).

- **Game** (build / store app): free for players, provided **as is** without warranty; **commercial** use of the game, its assets, or Vardo **branding** is **not** allowed without **prior written** permission.
- **Source code** in this repository: you may use it for **non-commercial** purposes (study, private projects, etc.); **commercial** use of the code requires the same **written** permission. Redistribution of source or derivatives (even non-commercial) must keep this license file and must not suggest endorsement by **Vardo Games**.
- **Vardo Games**, **Vardo**, and related **logos / marks** are the **property of Vardo**; do not use them in your own products, services, or marketing without permission (except where applicable law allows, e.g. factually saying a fork is based on this repo).

**Third-party** software (Godot, .NET, and any materials under their own licenses) stays under those licenses; see the **Credits** section below and their upstream terms.

## Credits

- **[Godot Engine](https://godotengine.org/)** — [license](https://github.com/godotengine/godot/blob/master/LICENSE.txt)
- **.NET** — [MIT license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)
- **Court card art** (jacks, queens, kings) — [Vector Playing Cards](https://github.com/notpeter/Vector-Playing-Cards) by Byron Knoll, public domain (see `tools/court-art/README.md`)

## Author

**VardoGames** — [vardogames.com](https://vardogames.com/)

---

*Repository purpose: public source + transparency for a free store release, not a substitute for the store listing or privacy policy.*
