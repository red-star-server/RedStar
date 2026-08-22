# AGENTS.md

This file contains repository-specific instructions for coding agents working on RedStar.

## Project

RedStar is based directly on the official:

`space-wizards/space-station-14`

repository.

The project should stay reasonably close to current Space Wizards architecture while adding RedStar-specific content where appropriate.

Do not treat Goob Station, RedStar-Goob, or another SS14 fork as architectural authority for this repository.

## General rules

When making changes:

1. Inspect the current implementation before modifying it.
2. Follow patterns already used by current Space Station 14 code.
3. Keep diffs focused and minimal.
4. Avoid unrelated cleanup and formatting.
5. Prefer existing APIs over introducing duplicate abstractions.
6. Keep generic code independent from RedStar-specific content where practical.
7. Do not copy implementations from another fork without checking their origin and license.

## Repository structure

Important locations:

- `Content.Shared` - shared client/server code.
- `Content.Server` - server-side code.
- `Content.Client` - client-side code and UI.
- `Resources/Prototypes` - YAML prototypes.
- `Resources/Locale` - localization.
- `Resources/Textures` - textures and RSI resources.
- `Resources/Audio` - audio resources.
- `RobustToolbox` - engine git submodule.

RedStar-specific resources should generally be placed under an appropriate `_RedStar` directory.

Generic changes that could reasonably be upstreamed should use the normal upstream directory structure.

## Editing upstream code

Do not add downstream marker comments such as:

```text
// RedStar
// RS14-start
// RS14-end
```

Git history is the source of truth for project-specific modifications.

Keep changes to upstream files as small as reasonably possible to reduce future merge conflicts.

## Upstreamable changes

When a change is generic enough to be useful to Space Station 14:

- avoid RedStar-specific prototype or system dependencies;
- follow current upstream naming and architecture;
- keep the implementation independently useful;
- do not include third-party code that cannot legally be contributed under MIT.

Do not broaden the task solely to make a change upstreamable.

The requested RedStar behavior remains the primary requirement.

## Third-party ports

Before porting code or assets from another repository:

- identify the original source;
- identify the applicable license;
- preserve required attribution and SPDX information;
- adapt the implementation to current RedStar and Wizards architecture;
- avoid blindly copying code built around fork-specific APIs.

Never assume that rewriting or heavily modifying third-party AGPL code automatically makes it eligible for MIT relicensing.

## Licensing

Original RedStar code and copyrightable RedStar modifications are AGPL-3.0-or-later unless explicitly stated otherwise.

Upstream Space Station 14 code retains its MIT license.

Third-party material retains its applicable license and attribution requirements.

See:

- `LICENSE.md`
- `CONTRIBUTOR_LICENSE_AGREEMENT.md`

## RobustToolbox

Do not modify the `RobustToolbox` submodule unless the task explicitly requires an engine change.

Do not commit accidental submodule pointer changes.

## Build and validation

Initial setup:

```shell
python RUN_THIS.py
```

Baseline build:

```shell
dotnet build
```

Run relevant targeted tests when they exist for the modified subsystem.

Do not modify or disable unrelated tests merely to make a change pass.

## Before finishing

Before considering a task complete:

- inspect the final diff;
- check the changed file list;
- remove accidental files or formatting changes;
- verify new resources are referenced correctly;
- verify third-party attribution where applicable;
- build or run relevant tests when practical.

Do not claim that a command or test was run unless it was actually executed.
