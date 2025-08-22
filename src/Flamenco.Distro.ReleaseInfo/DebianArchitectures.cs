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
using Flamenco.Packaging.Dpkg;

namespace Flamenco.Distro.ReleaseInfo;

public static class DebianArchitectures
{
    static DebianArchitectures()
    {
        var officialPorts = ImmutableArray.CreateBuilder<DpkgArchitecture>(initialCapacity: 9);
        officialPorts.Add(DpkgArchitecture.Amd64);
        officialPorts.Add(DpkgArchitecture.Arm64);
        officialPorts.Add(DpkgArchitecture.Armel);
        officialPorts.Add(DpkgArchitecture.Armhf);
        officialPorts.Add(DpkgArchitecture.I386);
        officialPorts.Add(DpkgArchitecture.Mips64el);
        officialPorts.Add(DpkgArchitecture.Ppc64el);
        officialPorts.Add(DpkgArchitecture.S390X);
        officialPorts.Add(DpkgArchitecture.RiscV64);
        OfficialPorts = officialPorts.ToImmutable();
    }
    
    public static readonly ImmutableArray<DpkgArchitecture> OfficialPorts;
}