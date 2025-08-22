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

namespace Flamenco.Distro.ReleaseInfo;

public static class UbuntuPockets
{
    public static readonly DpkgPocket Release = DpkgPocket.Release;
    public static readonly DpkgPocket Security = DpkgPocket.Parse("security");
    public static readonly DpkgPocket Updates = DpkgPocket.Parse("updates");
    public static readonly DpkgPocket Proposed = DpkgPocket.Parse("proposed");
    public static readonly DpkgPocket Backports = DpkgPocket.Parse("backports");
}