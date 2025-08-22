// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

namespace Flamenco.Packaging;

public enum TarballCompressionMethod
{
    // ReSharper disable once InconsistentNaming
    XZ = 2, 
    GZip = 1,
    None = 0
}

public static class TarballCompressionMethodExtensions
{
    public static string FileExtension(this TarballCompressionMethod tarballCompressionMethod) =>
        tarballCompressionMethod switch
        {
            TarballCompressionMethod.XZ => ".xz",
            TarballCompressionMethod.GZip => ".gz",
            TarballCompressionMethod.None => string.Empty,
            _ => throw new ArgumentException(paramName: nameof(tarballCompressionMethod),
                message: $"Invalid tarball compression method '{tarballCompressionMethod}'.")
        };
}

public interface ITarArchivingServiceProvider
{
    public Task<Result> CreateTarArchiveAsync(
        FileInfo archiveFile,
        DirectoryInfo archiveRoot,
        IEnumerable<string> includedPaths,
        TarballCompressionMethod compressionMethod = TarballCompressionMethod.None,
        CancellationToken cancellationToken = default);

    public Task<Result> ExtractTarArchiveAsync(
        FileInfo archiveFile,
        DirectoryInfo targetDirectory,
        uint stripComponents = 0,
        CancellationToken cancellationToken = default);
}