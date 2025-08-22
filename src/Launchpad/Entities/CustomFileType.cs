// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

namespace Canonical.Launchpad.Entities;

/// <summary>
/// The file type attached to a <see cref="PackageUpload"/>.
/// </summary>
/// <seealso cref="PackageUpload.CustomFileUrls"/>
public record CustomFileType(string Value)
{
    public static readonly CustomFileType RawInstaller = new("raw-installer");
    public static readonly CustomFileType RawTranslations = new("raw-translations");
    public static readonly CustomFileType RawDistUpgrader = new("raw-dist-upgrader");
    public static readonly CustomFileType RawDdtpTarball = new("raw-ddtp-tarball");
    public static readonly CustomFileType RawTranslationsStatic = new("raw-translations-static");
    public static readonly CustomFileType MetaData = new("meta-data");
    public static readonly CustomFileType Uefi = new("uefi");
    public static readonly CustomFileType Signing = new("signing");
}