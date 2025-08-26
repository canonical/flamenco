// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Immutable;

namespace Flamenco.Packaging.Dpkg;

public readonly record struct ChangelogEntry(
    DpkgName PackageName,
    DpkgVersion Version,
    ImmutableArray<DpkgSuite> Distributions,
    ImmutableDictionary<string, string> Metadata,
    string Description,
    MaintainerInfo Maintainer,
    DateTimeOffset Date,
    Location Location = default)
{
    public string? Urgency => CollectionExtensions.GetValueOrDefault(Metadata, key: "urgency");
    
    public string? BinaryOnly => CollectionExtensions.GetValueOrDefault(Metadata, key: "binary-only");
}

public readonly record struct MaintainerInfo(string Name, string EmailAddress);