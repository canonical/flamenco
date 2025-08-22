
![Flamenco Icon](images/icon-512.webp)        
# Flamenco
[![Flamenco Snap Store Badge](https://snapcraft.io/flamenco/badge.svg)](https://snapcraft.io/flamenco)
[![License badge for GNU General Public License v3.0](https://img.shields.io/badge/License-GPL--3.0-informational)](https://github.com/canonical/flamenco/blob/main/LICENSE)

Flamenco helps deb package maintainers manage multiple versions of source packages from a single Debian source tree.

## Motivation

When packaging software for Debian-based systems (like Ubuntu), maintaining multiple versions of a source package for every series you target can be a challenge. Different versions often share many components, which means updating these shared components for every version if changes occur. This approach is sufficient for situations with infrequent upstream releases and allows for control over changes included for specific targets. However, in scenarios with many targets, frequent upstream releases, and frequently updated shared components, maintenance becomes time-consuming, prone to inconsistencies, and can discourage improving shared components.

For example, maintaining .NET packages for Ubuntu involves packaging 2-4 monthly .NET releases for 2-4 active Ubuntu series. The number of .NET versions and Ubuntu series to maintain may even increase in the future.

## Getting started

> [!WARNING]
> This project is still in active development. The command line interface and its output may change at any point.

### Prerequisites

- snapd ([How to install snapd?](https://snapcraft.io/docs/installing-snapd))

### Installation

```shell
snap install --edge flamenco
```

## Example

See how to use Flamenco with the practical example of [Ubuntu .NET Source Build](https://github.com/canonical/dotnet-source-build), the original use case for which it was developed.

## Dependencies

### Build Dependencies

- .NET 8.0 SDK (C# 12)
- System.CommandLine ([NuGet](https://www.nuget.org/packages/System.CommandLine)) ([Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/))
- Spectre.Console ([NuGet](https://www.nuget.org/packages/Spectre.Console)) ([Documentation](https://spectreconsole.net/))
- snapcraft (optional)

### Runtime Dependencies

- env (part of [Coreutils – GNU core utilities](https://www.gnu.org/software/coreutils/))
- tar
- xz-utils (recommended)
- libicu
- liblttng-ust1

## Build from source

> [!NOTE]
> The following shell script asume to be executed from the root of this repository.

### With Snapcraft

```shell
snapcraft
```

### Without Snapcraft

```shell
dotnet publish src \
    --output dist \
    --configuration Release \
    -p:DebugSymbols=false \
    -p:DebugType=none
```

## FAQ

### Can this be used for other use cases than .NET?

**Absolutely!** Although .NET is Flamenco's primary use case, we strive to keep it as generic as possible. We'd love to hear how Flamenco could help with your use case. Feel free to try it out, [raise any issues](https://github.com/canonical/flamenco/issues) you encounter, or [contact us](https://github.com/canonical/flamenco/discussions) to discuss your ideas.

### What is the origin of the name?

We brainstormed many names that were initially too long and unsuitable for daily use. We wanted a friendly-sounding name without contextual baggage in the Ubuntu packaging or toolchains world. "Flamenco" was chosen because:

1. Flamenco is a traditional Spanish dance. It refers to the Canonical Engineering Sprint in May 2024 in Madrid, Spain, where we presented the proof of concept.

1. Monthly packaging of .NET updates is like a choreography, similar to a dance.

### Why not use `git-ubuntu` with `git cherry-pick`?</b>

This is a valid workflow. For the .NET use case, packaging 2-4 monthly .NET releases for 2-4 active Ubuntu series can be repetitive and prone to mistakes. Additionally, getting an overview of which components are included in which package version can be challenging.

### Couldn't this have been a Makefile?

Yes, but for the .NET use case, we package 2-4 monthly .NET releases for 2-4 active Ubuntu series. Flamenco is tailored for this use case, making it easier to set up, adapt, and use. It also provides convenience features to streamline the release process and common maintenance tasks.

### If the source packages contain frequently updated shared component, why not individually package them up?

While individual packaging of frequently updated shared components can be a beneficial strategy, it's not without its challenges. Toolchains with dependencies on them self can pose significant hurdles. Additionally, the transition process, where individual packages must move together, can also be complex. Furthermore, the individual packaging of shared components will potentially clutter the archive package catalogue and can contribute to increased infrastructure cost associated with uploading, storing and distributing the package and additional code review time by maintainers.

## Authors

- **Dominik Viererbe** <<dominik.viererbe@canonical.com>> ([@dviererbe](https://github.com/dviererbe))
- **Mateus Rodrigues de Morais** <<mateus.morais@canonical.com>> ([@mateusrodrigues](https://github.com/mateusrodrigues))

## License

Flamenco
Copyright (C) 2024 Canonical Ltd.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 3 as published by
the Free Software Foundation.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
