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
using Flamenco.Distro.Services.Abstractions;

namespace Flamenco.Distro.ReleaseInfo;

public static class DebianComponents
{
    public static readonly DpkgComponent Main = DpkgComponent.Parse("main");
    public static readonly DpkgComponent Contrib = DpkgComponent.Parse("contrib");
    public static readonly DpkgComponent NonFree = DpkgComponent.Parse("non-free");

    public static readonly ImmutableArray<DpkgComponent> All;

    static DebianComponents()
    {
        All = [Main, Contrib, NonFree];
    }
}
