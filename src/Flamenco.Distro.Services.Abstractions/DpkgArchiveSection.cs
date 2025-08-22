// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using Flamenco.Packaging.Dpkg;

namespace Flamenco.Distro.Services.Abstractions;

/// <summary>
/// Immutable representation that refers to the section of an archive.
/// </summary>
/// <param name="Name">Human friendly name of the archive.</param>
/// <param name="Component">Component of the archive this section refers to.</param>
/// <param name="Suite">Suite of the archive this section refers to.</param>
public record DpkgArchiveSection(string Name, DpkgComponent Component, DpkgSuite Suite);
// Note: this is a record CLASS and not a struct to make it inheritable and therefore extendable.